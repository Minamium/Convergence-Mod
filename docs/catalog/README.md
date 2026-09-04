# Documentation Catalog

`documents.yml` is generated from validated Markdown front matter and normalized
full-document hashes. It is intended for fast machine discovery and cache
invalidation. Do not treat it as a second source of truth or edit it by hand.

```bash
python3 tools/docs_catalog.py --write
python3 tools/docs_catalog.py --check
```

The schema vocabulary and exact, reason-bearing `excluded_paths` inventory are
committed in `schema.yml`. Every `docs/**/*.md` file must either have valid
front matter or appear in that inventory. This README is deliberately excluded;
there is no blanket exclusion for the catalog directory, so an unexpected new
Markdown file under `docs/catalog` fails validation. The same migration inventory
is emitted as `excluded_documents` in the generated catalog so consumers never
mistake an excluded legacy document for a missing file.

The validator rejects duplicate YAML keys, unknown front-matter fields,
non-authoritative statuses that claim topics, and H1 headings hidden inside
fenced examples. Full design and local database guidance live in
[`../DOCUMENTATION_SYSTEM.md`](../DOCUMENTATION_SYSTEM.md).
