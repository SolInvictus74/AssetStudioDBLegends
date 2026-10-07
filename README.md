
# AssetStudio – Dragon Ball Legends Edition
A modified version of AssetStudio focused on restoring compatibility with modern **Dragon Ball Legends** assets built with **Unity 6 (6000.x)**.
> [!IMPORTANT]
> This project is primarily developed and tested for **Dragon Ball Legends**.
> It should not be considered a guarantee of universal Unity 6 compatibility across all Unity games.

---

# Features

- Unity 6 / 6000.x parsing support for modern Dragon Ball Legends assets
- Updated Shader serialization handling
- Updated Renderer serialization handling
- Fixed SkinnedMeshRenderer parsing
- Working Scene Hierarchy reconstruction
- Unity 6 Texture / Texture2D serialization support
- Correct handling of Unity 6 texture metadata and streamed `.resS` resources
- Fixed texture metadata corruption and texture decoding crashes
- Restored material and texture processing
- Working merged FBX model export
- Associated PNG texture export
- Fixed Unity 6 AnimationClip parsing
- Lazy Mesh processing to reduce unnecessary work during asset loading
- AssetMap support for selective asset loading
- Light / Dark GUI themes
- Live theme switching with persistent preferences
- Legacy parsing paths kept separate wherever possible

---

# Quick Start

If you simply want to extract a Dragon Ball Legends character, the recommended workflow is:

```text
Dragon Ball Legends files
          │
          ▼
        Misc.
          │
          ▼
Write a name in the first white rectangle
          │
          ▼
    Build Both
          │
          ▼
    Select Game files
          │
          ▼
    Let the program build
          │
          ▼
    Open AssetBrowser in Misc
          │
          ▼
    Load AssetMap
          │
          ▼
   Search Character
          │
          ▼
    Load Selected
          │
          ▼
  Go to Scene Hierarchy
          │
          ▼
     Select Model
          │
          ▼
      Export FBX
          │
          ▼
  FBX + PNG Textures
```

AssetStudio also supports loading files normally through **Load Folder**, but for large Dragon Ball Legends installations the **AssetMap workflow is recommended**.

---

# Installation

Download the latest release from the GitHub Releases page.

Two builds may be provided:

### .NET 8 — Recommended

Use the .NET 8 build unless you have a specific reason not to.

This is the primary build of AssetStudio – Dragon Ball Legends Edition.

### .NET 7 — Compatibility Build

The .NET 7 build provides the same core functionality and can be used as an alternative if the .NET 8 build causes compatibility or antivirus issues on your system.

Extract the downloaded archive and launch:

```text
AssetStudio.GUI.exe
```

No installation is required.

---

# Tutorial

## 1. Understanding Asset Loading

There are two ways to load Dragon Ball Legends assets.

### Method 1 — AssetMap

**Recommended for Dragon Ball Legends.**

AssetMap allows AssetStudio to index the game files first and later load only the assets you actually want to inspect.

This is particularly useful for Dragon Ball Legends because a complete game dataset may contain **tens of thousands of files**.

Instead of loading the entire dataset every time, the workflow becomes:

```text
Game Files
    │
    ▼
AssetMap / CABMap
    │
    ▼
Asset Browser
    │
    ▼
Search
    │
    ▼
Load Selected Assets
```

### Why AssetMap is recommended

Use AssetMap when:

- Searching for a specific character
- Extracting a specific model
- Looking for animations or textures
- Working with the complete Dragon Ball Legends asset collection
- You do not need every game asset loaded simultaneously

This is the recommended workflow for normal use.

---

### Method 2 — Normal Folder Loading

AssetStudio can also load assets directly.

Use:

```text
File → Load Folder
```

and select the folder containing the files you want to inspect.

AssetStudio will process the files in that folder normally.

This method is useful when:

- Working with a small number of CAB files
- Testing a small extracted dataset
- Debugging
- Inspecting an entire folder
- Building or validating parser changes

It is fully supported, but loading an entire Dragon Ball Legends installation is generally unnecessary if you only need one character or asset.

### Which method should I use?

| Situation | Recommended method |
|---|---|
| Extract one character | AssetMap |
| Find a specific model | AssetMap |
| Find animations | AssetMap |
| Search the complete game | AssetMap |
| Work with a few CAB files | Load Folder |
| Parser/debug testing | Load Folder |
| Load the entire dataset | Load Folder |

> [!TIP]
> If you are unsure which method to use, use **AssetMap**.

---

# 2. Building an AssetMap

Before using Asset Browser for the first time, an AssetMap must be generated from your Dragon Ball Legends files.

The recommended option is:

```text
Build Both
```

This generates the information required for both the **AssetMap** and **CABMap** workflows.

The AssetMap contains information about the serialized assets.

The CABMap helps AssetStudio associate those assets with their corresponding CAB files and dependencies.

### Basic procedure

1. Open AssetStudio.
2. Open Misc.
3. Put a name in the two white box
4. Select **Build Both**.
5. Select the folder containing your Dragon Ball Legends asset files.
6. Choose where the generated map files should be saved.
7. Wait for AssetStudio to finish processing the files.


Once generated, the maps can be reused for later sessions until the game assets change significantly.

> **Screenshot 1 — Main AssetStudio window**
>
> `<!-- <img width="1264" height="713" alt="image" src="https://github.com/user-attachments/assets/7307a3e8-35e3-48b3-846f-6ee313d33ad1" /> -->`

> **Screenshot 2 — Building AssetMap / CABMap**
>
> `<!-- <img width="1260" height="710" alt="image" src="https://github.com/user-attachments/assets/3db7a227-bb56-44cd-9a0b-9f1c0dd8a7d9" /> -->`

---

# 3. Using Asset Browser

After creating your AssetMap, open the **Asset Browser**.

Load the previously generated AssetMap.

The Asset Browser allows you to search the indexed Dragon Ball Legends assets without loading the complete game into AssetStudio.

> **Screenshot 3 — Asset Browser**
>
> `<!-- <img width="624" height="509" alt="image" src="https://github.com/user-attachments/assets/d756e405-ceb3-4d4b-85ca-96adbdcc1e2e" /> -->`

Use the search and filtering tools to locate the character or asset you need.

Depending on what you are looking for, useful asset types may include:

- Animator
- GameObject
- Mesh
- SkinnedMeshRenderer
- Texture2D
- Material
- AnimationClip

---

# 4. Load Selected vs Export Selected

Asset Browser provides different ways of working with the assets you find.

## Load Selected

Use **Load Selected** when you want to inspect the asset inside the main AssetStudio interface.

This is the recommended option when extracting complete character models.

Typical workflow:

```text
Search Character
      │
      ▼
Select Asset
      │
      ▼
Load Selected
      │
      ▼
Main AssetStudio GUI
      │
      ▼
Scene Hierarchy
```

> **Screenshot 4 — Load Selected**
>
> `<!-- <img width="625" height="513" alt="image" src="https://github.com/user-attachments/assets/087282f3-89f9-4b0a-b392-f79cad024e47" /> -->`

TIP: Select the first white rectangle in the upper left corner to select all the results.

## Export Selected

**Export Selected** can be useful when you simply want to export supported assets directly without exploring them through the Scene Hierarchy first.

For complete character/model extraction, however, **Load Selected → Scene Hierarchy → FBX Export** provides more control.

---

# 5. Youtube tutorial for animation and mesh
https://www.youtube.com/watch?v=uFEz9pVr4-I

# 6. Export Options

Export behavior can be customized through:

```text
Options → Export Options
```

Some options can significantly affect model and animation export behavior.

## Collect Animations

When enabled, AssetStudio may collect animations associated with the exported model.

This can be useful when you want model and animation data together.

However, collecting large numbers of animations may substantially increase FBX size and export time.

If you only need the character model, consider disabling animation collection.

## Ignore Controller Anim

If you want to export models or animators without automatically fetching all controller animations, enable:

```text
Ignore Controller Anim
```

before loading the assets.

---

# 10. Light and Dark Themes

AssetStudio – Dragon Ball Legends Edition includes both Light and Dark GUI themes.

Use:

```text
Theme → Light
```

or:

```text
Theme → Dark
```

Theme changes are applied live without restarting AssetStudio.

The selected theme is saved and restored the next time the application is launched.

Dark Mode includes custom styling for:

- Menus
- Tabs
- Panels
- TreeViews
- ListViews
- DataGridViews
- Input controls
- Secondary windows
- Progress indicators

> [!NOTE]
> Changing themes after a large number of assets have already been loaded may require several seconds while Windows redraws the interface. AssetStudio may temporarily appear unresponsive during this process.

---

# Troubleshooting

## AssetStudio takes a long time to load the entire game

Use **AssetMap** instead of loading the complete Dragon Ball Legends folder.

AssetMap is specifically useful for large datasets because it allows you to locate and load only the assets you need.

---

## I cannot find my character

Make sure:

1. The AssetMap was built from the correct game files.
2. The correct AssetMap is loaded in Asset Browser.
3. Your game files and AssetMap belong to the same or compatible game data version.
4. Your search/filter is not excluding the required asset type.

---

## FBX exports but textures are missing

Verify that the relevant Texture2D and Material dependencies were available when the model was loaded.

AssetMap is recommended because it helps AssetStudio identify the files and dependencies associated with the selected assets.

---

## Exporting a model produces an extremely large FBX

Check your animation export settings.

If you only need the model, disable unnecessary animation collection or enable **Ignore Controller Anim** where appropriate.

---

## Antivirus warning

Some antivirus engines may heuristically flag the .NET 8 build.

The equivalent .NET 7 build can be used as an alternative if this occurs.

The project is open source, and binaries can also be built directly from the provided source code.

---

# Compatibility

## Primary Target

**Dragon Ball Legends**

## Verified configuration

```text
Unity:              6000.3.14f1
Dataset:            30,000+ files
Parsing failures:   0
Scene Hierarchy:    Working
AssetMap:           Working
FBX export:         Working
PNG textures:       Working
AnimationClip:      Working
```

Other Dragon Ball Legends versions may also work, but serialization changes introduced by future game or Unity updates may require additional parser updates.

Other Unity 6 / Unity 6000.x games may work as well, but they have not necessarily been tested.

> [!WARNING]
> Do not assume that successful Dragon Ball Legends support means complete support for every Unity 6 game.
>
> Unity serialization can vary depending on Unity version, build configuration and asset type.

---

# CLI Version

The project also includes the AssetStudio CLI.

```text
Usage:
  AssetStudioCLI <input_path> <output_path> [options]

Arguments:
  <input_path>   Input file/folder.
  <output_path>  Output folder.

Options:
  --silent                                                Hide log messages.
  --type <Texture2D|Sprite|etc..>                         Specify Unity class type(s).
  --filter <filter>                                       Specify regex filter(s).
  --game <BH3|CB1|CB2|CB3|GI|SR|TOT|ZZZ>                Specify Game.
  --map_op <AssetMap|Both|CABMap|None>                    Specify which map to build. [default: None]
  --map_type <JSON|XML>                                   AssetMap output type. [default: XML]
  --map_name <map_name>                                   Specify AssetMap file name.
  --group_assets_type <ByContainer|BySource|ByType|None>  Specify how exported assets should be grouped.
  --no_asset_bundle                                       Exclude AssetBundle from AssetMap/Export.
  --no_index_object                                       Exclude IndexObject/MiHoYoBinData from AssetMap/Export.
  --xor_key <xor_key>                                     XOR key to decrypt MiHoYoBinData.
  --ai_file <ai_file>                                     Specify asset_index JSON file path.
  --version                                                Show version information.
  -?, -h, --help                                          Show help and usage information.
```

> [!NOTE]
> Some CLI options originate from the upstream RazTools/AssetStudio codebase and target other supported game workflows. The GUI AssetMap workflow described above is the recommended workflow for Dragon Ball Legends.

---

# Credits

This project is based on AssetStudio and the work of its contributors, with additional parser research, Unity 6 compatibility work and Dragon Ball Legends-specific testing.

Special thanks to:

- **Perfare** — Original AssetStudio author.
- **Razmoth** — [RazTools / Studio](https://github.com/RazTools/Studio), the fork this project builds upon.
- **Khang06** — [genshinblkstuff](https://github.com/khang06/genshinblkstuff) extraction research.
- **Radioegor146** — [GI Asset Indexes](https://github.com/radioegor146/gi-asset-indexes).
- **Ds5678 / AssetRipper** — [AssetRipper](https://github.com/AssetRipper/AssetRipper) and research into Unity asset formats and parsing.
- **mafaca** — [uTinyRipper](https://github.com/mafaca/UtinyRipper), including work related to YAML and AnimationClip conversion.
- **Modder4869** — for the original [AssetStudioHelp tutorial](https://gist.github.com/Modder4869/0f5371f8879607eb95b8e63badca227e), which provided useful reference material for the AssetMap workflow.

---

# Disclaimer

This project is an unofficial community tool and is not affiliated with or endorsed by Bandai Namco, Dimps, Unity Technologies, or the original AssetStudio project.

Use extracted game assets in accordance with the applicable licenses, terms of service and copyright laws.
