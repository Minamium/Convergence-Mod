"""Deterministic Ogg Vorbis helpers for project audio tools (no external encoder).

- fixed_serial: libsndfile picks a random stream serial per file; pin it and
  recompute every page CRC so an unchanged recipe reproduces identical bytes.
- set_vorbis_tags: rebuild the Vorbis comment header with explicit tags such as
  tModLoader's LOOPSTART/LOOPEND sample positions, re-paginating the header
  packets and renumbering the audio pages.
"""
import struct


def _crc_table():
    table = []
    for index in range(256):
        value = index << 24
        for _ in range(8):
            value = ((value << 1) ^ 0x04C11DB7) if value & 0x80000000 else value << 1
        table.append(value & 0xFFFFFFFF)
    return table


_CRC = _crc_table()


def _crc(page):
    value = 0
    for byte in page:
        value = ((value << 8) & 0xFFFFFFFF) ^ _CRC[((value >> 24) & 0xFF) ^ byte]
    return value


def _pages(data):
    pages, position = [], 0
    while position < len(data):
        if data[position:position + 4] != b"OggS":
            raise ValueError("Unexpected Ogg page layout")
        header_type = data[position + 5]
        granule = struct.unpack_from("<q", data, position + 6)[0]
        serial = struct.unpack_from("<I", data, position + 14)[0]
        count = data[position + 26]
        lacing = list(data[position + 27:position + 27 + count])
        start = position + 27 + count
        payload = data[start:start + sum(lacing)]
        pages.append({"type": header_type, "granule": granule, "serial": serial, "lacing": lacing, "payload": payload})
        position = start + sum(lacing)
    return pages


def _page_bytes(page, sequence, serial):
    header = bytearray(b"OggS")
    header += bytes([0, page["type"]])
    header += struct.pack("<qIII", page["granule"], serial, sequence, 0)
    header += bytes([len(page["lacing"])]) + bytes(page["lacing"])
    raw = bytearray(header) + page["payload"]
    struct.pack_into("<I", raw, 22, _crc(raw))
    return bytes(raw)


def fixed_serial(data, serial):
    pages = _pages(data)
    return b"".join(_page_bytes(page, index, serial) for index, page in enumerate(pages))


def _header_packets(pages):
    """Split the first three packets (identification, comment, setup) and return
    them with the index of the first page that holds audio."""
    packets, current = [], bytearray()
    for index, page in enumerate(pages):
        offset = 0
        for size in page["lacing"]:
            current += page["payload"][offset:offset + size]
            offset += size
            if size < 255:
                packets.append(bytes(current))
                current = bytearray()
                if len(packets) == 3:
                    if offset != len(page["payload"]):
                        raise ValueError("Audio shares the setup header page; cannot re-paginate")
                    return packets, index + 1
    raise ValueError("Incomplete Vorbis headers")


def _comment_packet(vendor, tags):
    body = bytearray(b"\x03vorbis")
    vendor_bytes = vendor.encode("utf-8")
    body += struct.pack("<I", len(vendor_bytes)) + vendor_bytes
    entries = [f"{key}={value}".encode("utf-8") for key, value in tags]
    body += struct.pack("<I", len(entries))
    for entry in entries:
        body += struct.pack("<I", len(entry)) + entry
    body += b"\x01"
    return bytes(body)


def _paginate(packets):
    """Lace header packets into pages of at most 255 segments.

    A page that continues an unfinished packet carries flag 1; a page on which
    no packet ends carries granule -1, as the Ogg specification requires.
    """
    pages, lacing, payload, continued = [], [], bytearray(), False

    def close():
        ends = any(size < 255 for size in lacing)
        pages.append({"type": 1 if continued else 0, "granule": 0 if ends else -1,
                      "lacing": list(lacing), "payload": bytes(payload)})

    for packet in packets:
        sizes = [255] * (len(packet) // 255) + [len(packet) % 255]
        offset = 0
        for size in sizes:
            if len(lacing) == 255:
                close()
                continued = lacing[-1] == 255
                lacing, payload = [], bytearray()
            lacing.append(size)
            payload += packet[offset:offset + size]
            offset += size
    if lacing:
        close()
    return pages


def read_vorbis_tags(data):
    packets, _ = _header_packets(_pages(data))
    comment = packets[1]
    if comment[:7] != b"\x03vorbis":
        raise ValueError("Missing Vorbis comment header")
    position = 7
    vendor_length = struct.unpack_from("<I", comment, position)[0]
    position += 4 + vendor_length
    count = struct.unpack_from("<I", comment, position)[0]
    position += 4
    tags = []
    for _ in range(count):
        length = struct.unpack_from("<I", comment, position)[0]
        position += 4
        key, _, value = comment[position:position + length].decode("utf-8").partition("=")
        tags.append((key, value))
        position += length
    return tags


def set_vorbis_tags(data, tags, vendor="Convergence audio tools"):
    pages = _pages(data)
    serial = pages[0]["serial"]
    packets, audio_start = _header_packets(pages)
    if pages[0]["lacing"] != [len(packets[0])] or not pages[0]["type"] & 2:
        raise ValueError("Identification header must fill the first page")
    rebuilt = [pages[0]] + _paginate([_comment_packet(vendor, tags), packets[2]]) + pages[audio_start:]
    return b"".join(_page_bytes(page, index, serial) for index, page in enumerate(rebuilt))
