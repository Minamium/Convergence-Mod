# Assets

Only distributable, reviewed runtime assets belong in this directory. Concept batches, editable source files, DAW projects, raw recordings, and third-party dependency files are excluded from the repository and from `.tmod` packaging.

Every PNG, OGG, WAV, MP3, font, or other distributable asset must have an entry in [ATTRIBUTION.md](ATTRIBUTION.md) before it is committed. The entry must identify its creator, origin, license, creation or acquisition date, modifications, and redistribution restrictions.

Planned runtime layout:

```text
Assets/
  Music/
  Sounds/
  Textures/
    Encounters/
    Items/
    Tiles/
    UI/
    VFX/
```

Large source assets will move to a separately governed storage workflow if they become necessary. Git LFS is not enabled yet.

