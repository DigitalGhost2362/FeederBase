# Changelog

## [2.0.0] - 2026-11-06

### Changed

- The merge pipeline is now written from scratch inside this module and no longer needs Mesh Baker. The module has no third-party dependency and is published like the other modules.
- Packing algorithm names: `TightPacker`, `FastShelfPacker`, `HorizontalStrips`, `VerticalStrips`, `UnityPackTextures`.
- Pivot location names: `BoundsCenter`, `WorldOrigin`, `CustomLocation`. `CustomLocation` now shows a Custom Pivot field.
- The bake results asset is now `FMeshBakeResults`, and the combined mesh object inside the prefab is named `{name}-mesh`.
- A bake with meshes that have Read/Write off now stops before writing anything and lists those meshes.

### Fixed

- Materials without a normal map no longer turn black when another source material has one: their part of the normal-map atlas is now a flat normal.

## [1.0.0] - 2026-11-06

### Added

- Mesh Modifier window: merge many meshes into one, split one mesh into parts.
