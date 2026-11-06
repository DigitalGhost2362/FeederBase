# Feeder Editor Tools

Editor tooling for Unity: batch asset and prefab operations, renaming, mesh and texture optimization, a scene loader and script templates.

**Unity:** 2022.3+
**Requires:** [Odin Inspector](https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041)

Full documentation: https://bingoboiz.github.io/FeederEditorTools/

---

## Install

1. Open **Window > Package Manager**.
2. Click **+** and choose **Add package from git URL**.
3. Paste:

```
https://github.com/BingoBoiz/FeederEditorTools.git?path=/Packages/com.feeder.editortools
```

4. Click **Add**.

Heavy features ship as separate modules. Install only the ones you need.

## Modules

Open **Tools > Feeder > Editor Tools > Modules** to install or remove a module from inside the Editor, or add its git URL by hand.

| Module | What it adds | Path for the git URL |
|---|---|---|
| Mesh Dedup & Align | Deduplicate Mesh Tool, plus mesh alignment with a Kabsch/ICP solver and a scene overlay | `/Packages/com.feeder.editortools.meshdedup` |
| Repack Models (FBX) | Repack Models Tool. Needs `com.unity.formats.fbx` | `/Packages/com.feeder.editortools.repackfbx` |
| Asset Cache | Asset Cache window: browse, filter and find references for any asset | `/Packages/com.feeder.editortools.assetcache` |
| GoogleSheet Importer | GoogleSheet Importer window. Needs `com.unity.nuget.newtonsoft-json` | `/Packages/com.feeder.editortools.googlesheet` |

Append the path to the repository URL, as in the install step above.

A tool from a module that is not installed still shows in the tool tree, with a button that installs the module.

---

## Windows

| Window | Menu |
|---|---|
| Editor Tools | `Tools > Feeder > Editor Tools > Tool Hub` |
| Modules | `Tools > Feeder > Editor Tools > Modules` |
| Documentation | `Tools > Feeder > Editor Tools > Documentation` |
| Scene Loader | `Tools > Feeder > Editor Tools > Windows > Scene Loader` |
| Asset Cache | `Tools > Feeder > Editor Tools > Windows > Asset Cache` |
| Texture Modifier | `Tools > Feeder > Editor Tools > Windows > Texture Modifier` |
| Mesh Palette Analyzer | `Tools > Feeder > Editor Tools > Windows > Mesh Palette Analyzer` |
| Script Template | `Tools > Feeder > Editor Tools > Windows > Script Template` |
| GoogleSheet Importer | `Tools > Feeder > Editor Tools > Windows > GoogleSheet Importer` |
| Empty Trash | `Tools > Feeder > Editor Tools > Settings > Empty Trash` |

## Tools

All of these live in the tool tree of the **Editor Tools** window. Each tool has a **Docs** button that opens its page.

| Group | Tool | What it does |
|---|---|---|
| Asset | Asset Collector | Scan a folder and load every asset of the chosen type into Target Assets for the other tools to use. |
| Asset | Asset Group Tool | Group Target Assets by name pattern or keyword, then write one group back into Target Assets. |
| Asset | Compare String Tool | Fuzzy-match Target Assets against the values of an enum by name, ignoring case, separators, token order and typos. |
| Asset | Sort Order Tool | Reorder Target Assets to match an enum's declaration order, matched by name. |
| Asset | Rename Tool | Rename assets or scene objects by pattern or by find-and-replace, with a preview and undo. |
| Asset | Asset Organizer Tool | Move Target Assets into a destination folder, keeping their order in the list. |
| Asset | Data Filler Tool | Fill a multi-asset field on a ScriptableObject or Component from Target Assets. |
| Asset | Character Mesh Updater Tool | Move SkinnedMeshRenderers and MeshRenderers from an old armature to a new one, keeping the source hierarchy. |
| Asset | Data Cloner Tool | Copy a collection field (Dictionary, List, HashSet) from one ScriptableObject to another. |
| Prefab | Component Modify Tool | Add, find, edit values on, or remove a component type across many prefabs and scene objects. |
| Prefab | Prefab Modify Tool | Edit Active, Layer, Tag and Transform of one node, or delete that node, across many prefabs. |
| Prefab | Prefab Variant Creator Tool | Create prefab variants in bulk from one base prefab and a list of models. |
| Prefab | Name Offset Tool | Shift the node that holds a name or label by a Y offset across many targets. |
| Prefab | Model Scale To Collider Tool | Scale a model to fit the collider of a base prefab. |
| Prefab | Mesh Box Collider Fitter Tool | Create a `_Col` child holding a BoxCollider fitted to the mesh by oriented bounding box. |
| Prefab | Component Replacer Tool | Replace one component type with another and copy over the fields that match by name and type. |
| Prefab | Missing Component Handler Tool | Handle GameObjects with a missing script: replace it, strip it, or just list them. |
| Optimize | Unpack Mesh Tool | Extract meshes, materials, textures and avatars into separate assets in their own folders. |
| Optimize | Unpack Animation Tool | Extract AnimationClips and the AnimatorController or Override into their own folder, then reassign the new controller. |
| Optimize | Deduplicate Texture Tool | Find identical textures (same resolution, same pixels) and merge them into one. |
| Optimize | Deduplicate Material Tool | Find materials that share a base map texture and merge them into one. |
| Optimize | VFX Flipbook Trim Tool | Trim a VFX flipbook atlas when Texture Sheet Animation uses only part of the tiles, and fix up `numTiles` and `frameOverTime` so the effect plays the same. |
| Optimize | Deduplicate Mesh Tool | Compare mesh assets side by side, find matching meshes in the scene, then align and replace them. Module: Mesh Dedup & Align. |
| Optimize | Repack Models Tool | Export each target mesh as an FBX next to the source asset, repoint scene references to it, then delete the old mesh asset. Module: Repack Models (FBX). |

---

## Scene Loader

Browse, open and add the project's scenes from one window.

**Open it**

- Click **Scenes** next to Play/Pause/Step in the main toolbar. The arrow next to it lists Favorites, Recent, Add Additive and Open Scene Loader.
- Press `Alt+S`. Rebind it from the shortcut chip at the bottom of the window, or in **Edit > Shortcuts** (`Feeder/Scene Loader/Toggle Window`).

**Use it**

- Double-click a scene to open it. Alt+Click or the `+` button adds it additively. The star adds it to Favorites.
- Drag a scene into the Hierarchy or onto the LOADED strip to add it. Click a LOADED chip to make it the active scene; its close button removes it.
- Opening and adding scenes is disabled in Play Mode.

**Configure it**

- **Project Settings > Feeder > Scene Loader**: Scan Roots (folders searched for scenes, default `Assets`), Top-Level Order, and Folder Rules (label, color, hidden per folder).
- Right-click a folder in the window for Edit Folder..., Color, Hide/Unhide and Show in Project. Drag top-level folders in the sidebar to reorder them. Hidden folders come back with the gear menu > Show Hidden Folders.

| File | Content | Commit |
|---|---|---|
| `ProjectSettings/FeederSceneLoader.asset` | Scan roots, folder order, folder rules | Yes, shared with the team |
| `UserSettings/FeederSceneLoader.asset` | Favorites, recent scenes, expanded folders | No, per machine |

## GoogleSheet Importer

Install the GoogleSheet Importer module first, then:

1. Enable the Google Sheets API in Google Cloud, create a service account, and download its JSON key.
2. Share the spreadsheet with the JSON key's `client_email`.
3. Open **Tools > Feeder > Editor Tools > Windows > GoogleSheet Importer**, set the credential path under **Data Config**, and add a sheet entry.
4. Paste the Spreadsheet ID, load the sheet, select a tab, then generate the script and the data asset.

Row 1 holds the generated type name. Row 2 holds the typed fields, such as `n_ID`, `f_Damage`, `s_Name` or `pref_Enemy:EnemyController`. Prefix a field header with `/` to skip that column.

Credential JSON files are project-local secrets. No credential, Spreadsheet ID, config asset or cached sheet data ships in this package.

---

## License

See [LICENSE](LICENSE).
