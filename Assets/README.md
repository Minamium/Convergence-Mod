# Assets

Only distributable, reviewed runtime assets belong in this directory. Concept batches, editable source files, DAW projects, raw recordings, and third-party dependency files are excluded from the repository and from `.tmod` packaging.

Every PNG, OGG, WAV, MP3, font, or other distributable asset must have an entry in [ATTRIBUTION.md](ATTRIBUTION.md) before it is committed. The entry must identify its creator, origin, license, creation or acquisition date, modifications, and redistribution restrictions.

Runtime music, sounds and textures are already present. [Asset Pipeline](../docs/ASSET_PIPELINE.md#external-originals-and-regeneration) owns the external-original manifest, preservation, backup and regeneration procedure. Do not put editable originals or raw batches here. Git LFS is not enabled; no new asset store is required for routine development.
