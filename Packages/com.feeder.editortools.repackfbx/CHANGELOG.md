# Changelog

## [1.0.0] - 2026-11-06

### Added

- Repack Models Tool, under `Optimize/` in the Editor Tools window: export each target mesh as an FBX next to the source asset, repoint scene references to it, then delete the old mesh asset.
- The tool registers itself, so it appears in the tool tree as soon as the package is installed.

### Changed

- Moved out of `com.feeder.editortools`, which no longer depends on `com.unity.formats.fbx`.

### Fixed

- Pressing Repack a second time no longer throws away the undo record of the first run. A second run that changes nothing used to clear it, which disabled Revert Last Repack and left the original mesh stranded in the trash folder.
