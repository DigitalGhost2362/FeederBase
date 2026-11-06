# Editor Tools - Mesh Modifier

Mesh merge and split window for the Feeder Editor Tools package.
Open it from `Tools > Feeder > Editor Tools > Windows > Mesh Modifier`.

## Merge

Combines every `MeshRenderer` under the chosen scene objects or prefab assets into one mesh.
Source objects are never modified.

- Textures of every source material are packed into one atlas per texture property
  (`_BaseMap`/`_MainTex`, normal map, metallic, specular, occlusion, emission, parallax,
  detail mask, plus any custom property you list). Normal maps are unpacked and written as a
  proper normal-map atlas.
- UVs are remapped into the atlas. With "Consider Mesh UVs" on, only the UV-covered region of
  each texture is baked, so tiled materials and UVs outside 0..1 keep their look.
- "Blend Non-Texture Properties" multiplies material tints (`_BaseColor`/`_Color`, emission
  color) into the atlas pixels.
- Packing algorithms: `TightPacker` (default), `FastShelfPacker`, `HorizontalStrips` and
  `VerticalStrips` (one axis stays tileable), `UnityPackTextures`.
- Pivot at the bounds center, the world origin or a custom point. Optional lightmap UV2.

Output, written to the chosen folder:

| File | Content |
|---|---|
| `{name}.prefab` | Root object with a child holding the combined `MeshFilter` and `MeshRenderer` |
| `{name}-mesh.asset` | Combined mesh |
| `{name}-mat.mat` | Combined material, a copy of the first source material with the atlases assigned |
| `{name}-mat-{property}-atlas-{n}.png` | One atlas per baked texture property |
| `{name}-bakeResults.asset` | `FMeshBakeResults`: which atlas rectangle each source material went to |

The combined material uses the shader of the first source material. Source meshes must have
Read/Write enabled in their import settings.

## Split

Splits one scene mesh into separate part meshes by UV island, by hand-picked vertices or with a
cutting volume, with a Scene View helper overlay.

## Install

Add it from the Modules window of Editor Tools, or paste this git URL into the Package Manager:

```
https://github.com/BingoBoiz/FeederEditorTools.git?path=/Packages/com.feeder.editortools.meshmodifier
```
