# Changelog

## [1.0.0] - 2026-11-06

### Added

- Deduplicate Mesh Tool, under `Optimize/` in the Editor Tools window: compare mesh assets side by side, find matching meshes in the scene, then align and replace them.
- Mesh align scene overlay and gizmos, with a Kabsch/ICP solver. Bundles MathNet.Numerics.
- The tool registers itself, so it appears in the tool tree as soon as the package is installed.

### Changed

- Moved out of `com.feeder.editortools`, which no longer ships these tools.
