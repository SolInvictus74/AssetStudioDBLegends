### IF YOU SEE THIS MESSAGE I HAVE NOT YET UPLOADED THIS PROJECT (04-10-2026)

# AssetStudio – Dragon Ball Legends / Unity 6 Edition

A modified version of AssetStudio focused on restoring compatibility with modern Dragon Ball Legends assets built with Unity 6 (6000.x).
Recent Dragon Ball Legends updates introduced serialized asset layouts that are not correctly handled by older AssetStudio-based tools, resulting in incomplete asset maps, object parsing failures, broken Scene Hierarchy data, and crashes during model/texture export.

This version extends the parser to correctly handle the Unity 6 serialization changes found in current Dragon Ball Legends assets, while preserving compatibility with legacy Unity asset layouts.

# Key improvements
Unity 6 / 6000.x parsing support for Dragon Ball Legends
Updated Shader serialization handling
Updated Renderer serialization handling
Fixed SkinnedMeshRenderer parsing and Scene Hierarchy reconstruction
Added proper Unity 6 Texture / Texture2D serialization support
Correct handling of Unity 6 texture metadata, mipmap fields, platform blobs, inline texture data and streamed .resS resources
Fixed texture metadata corruption that could cause invalid dimensions/formats and crashes during texture decoding
Restored model, material and texture processing
Restored merged FBX export for modern Dragon Ball Legends character assets
Legacy parsing paths are kept separate wherever possible to avoid breaking older Unity assets

The Unity 6 changes were investigated using serialized TypeTree data, raw object layouts and byte-level validation, rather than relying on format guesses. Texture2D parsing, for example, was validated against the complete serialized object boundary to ensure that the parser consumes the expected number of bytes.

# Current status - Oct 2026

Tested successfully with Dragon Ball Legends assets built with Unity 6000.3.14f1:
0 object parsing failures, working Scene Hierarchy, working textures/materials, and successful merged FBX export.

# Scope

This project should currently be considered a Dragon Ball Legends-focused AssetStudio fork, not a guarantee of complete Unity 6 compatibility across all Unity games. Additional Unity 6 titles and serialization variants may require further testing.

# Credits

Based on AssetStudio and the existing work of its contributors, with additional parser research and compatibility work for modern Dragon Ball Legends assets.
_____________________________________________________________________________________________________________________________
How to use:

Check the tutorial [here](https://gist.github.com/Modder4869/0f5371f8879607eb95b8e63badca227e) (Thanks to Modder4869 for the tutorial)
_____________________________________________________________________________________________________________________________
CLI Version:
```
Description:

Usage:
  AssetStudioCLI <input_path> <output_path> [options]

Arguments:
  <input_path>   Input file/folder.
  <output_path>  Output folder.

Options:
  --silent                                                Hide log messages.
  --type <Texture2D|Sprite|etc..>                         Specify unity class type(s)
  --filter <filter>                                       Specify regex filter(s).
  --game <BH3|CB1|CB2|CB3|GI|SR|TOT|ZZZ> (REQUIRED)       Specify Game.
  --map_op <AssetMap|Both|CABMap|None>                    Specify which map to build. [default: None]
  --map_type <JSON|XML>                                   AssetMap output type. [default: XML]
  --map_name <map_name>                                   Specify AssetMap file name.
  --group_assets_type <ByContainer|BySource|ByType|None>  Specify how exported assets should be grouped. [default: 0]
  --no_asset_bundle                                       Exclude AssetBundle from AssetMap/Export.
  --no_index_object                                       Exclude IndexObject/MiHoYoBinData from AssetMap/Export.
  --xor_key <xor_key>                                     XOR key to decrypt MiHoYoBinData.
  --ai_file <ai_file>                                     Specify asset_index json file path (to recover GI containers).
  --version                                               Show version information
  -?, -h, --help                                          Show help and usage information
```
_____________________________________________________________________________________________________________________________
NOTES:
```
- in case of any "MeshRenderer/SkinnedMeshRenderer" errors, make sure to enable "Disable Renderer" option in "Export Options" before loading assets.
- in case of need to export models/animators without fetching all animations, make sure to enable "Ignore Controller Anim" option in "Options -> Export Options" before loading assets.
```
_____________________________________________________________________________________________________________________________
Special Thank to:
- Perfare: Original author.
- Khang06: [Project](https://github.com/khang06/genshinblkstuff) for extraction.
- Radioegor146: [Asset-indexes](https://github.com/radioegor146/gi-asset-indexes) for recovered/updated asset_index's.
- Ds5678: [AssetRipper](https://github.com/AssetRipper/AssetRipper)[[discord](https://discord.gg/XqXa53W2Yh)] for information about Asset Formats & Parsing.
- mafaca: [uTinyRipper](https://github.com/mafaca/UtinyRipper) for `YAML` and `AnimationClipConverter`. 
