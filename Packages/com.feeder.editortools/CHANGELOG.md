# Changelog

## [2.0.0] - 2026-11-06

### Added

- `Tools > Feeder > Editor Tools > Modules` window: install or remove an optional module from inside the Editor, or copy its git URL.
- A tool that belongs to a module you have not installed still appears in the tool tree, with a button that installs the module.
- `Tools > Feeder > Editor Tools > Documentation`, and a **Docs** button on every tool that opens that tool's page.

### Changed

- Heavy features moved into separate packages. Install them alongside this one:
  `com.feeder.editortools.meshdedup` (Deduplicate Mesh Tool, mesh align),
  `com.feeder.editortools.repackfbx` (Repack Models Tool),
  `com.feeder.editortools.googlesheet` (GoogleSheet Importer),
  `com.feeder.editortools.assetcache` (Asset Cache window).
- Menu tree regrouped into `Asset/`, `Prefab/` and `Optimize/`. `Optimized/` is now `Optimize/`; Asset Collector and Asset Group Tool moved into `Asset/`.
- Windows moved under `Tools > Feeder > Editor Tools > Windows/`, settings under `Tools > Feeder > Editor Tools > Settings/`. `Feeder > Empty Feeder Trash` is now `Tools > Feeder > Editor Tools > Settings > Empty Trash`, and the root `Feeder` menu is gone.
- Feeder Editor Tools menus now live under their own `Tools > Feeder > Editor Tools` layer. The main window is `Tools > Feeder > Editor Tools > Tool Hub`.
- Window titles, tool names, progress bars and undo entries no longer repeat the word "Feeder".
- Dependencies are now `com.unity.textmeshpro` and `com.unity.2d.sprite`. `com.unity.formats.fbx` moved to the Repack Models module and `com.unity.nuget.newtonsoft-json` to the GoogleSheet Importer module.

### Fixed

- Running a tool a second time no longer throws away the undo record of the first run. A second run that changes nothing used to clear it, which left Rename, Asset Organizer, Deduplicate Texture and Deduplicate Material with a disabled Revert button and the moved files stranded in the trash folder.
- Replace With and Find in the Component Replacer tool now list only scripts. The dropdown offered every component type, and picking a built-in one such as BoxCollider always failed.
- Deduplicate Texture and Deduplicate Material now warn when a target is a prefab asset. They only work on objects in a scene, and used to skip prefab assets without a word, which read as "no duplicates found".
- Asset Collector reported the wrong number of prefabs. The typed Target Prefabs list is padded with empty slots so it lines up with Target Assets, and the log counted those slots as prefabs.

### Removed

- The Menu Style window. The tool tree always uses the built-in style.
- The `Tools/Feeder/Legacy/*` menu and every tool under it.

## [1.2.0] - 2026-09-21

### Added

- Scene Loader window (`Tools > Feeder > Scene Loader`, `Alt+S`): scenes grouped by folder, Favorites, Recent, In Build, fuzzy search, open or add additively, and a LOADED strip to set the active scene or remove a scene. The shortcut can be rebound from the window.
- `Scenes` button next to Play/Pause/Step in the main toolbar, with a dropdown for Favorites, Recent and Add Additive. Works on Unity 2022.3 and newer.
- Project Settings page `Feeder > Scene Loader`: scan roots, top-level folder order, and per-folder label, color and visibility. Saved to `ProjectSettings/FeederSceneLoader.asset`; favorites and recent scenes stay per machine in `UserSettings/FeederSceneLoader.asset`.

### Removed

- `Tools > Feeder > Scenes Loader Window`. Use the Scene Loader window instead.
