# Project Conventions

## Folder layout (`Assets/`)

```
Art/
  Source/          Layered PSDs (hand-drawn masters, imported directly by Unity's PSD Importer)
    Rooms/         PSD_House, PSD_Balcony, PSD_Bathroom
    Props/         PSD_Props_01, PSD_Props_02
    UI/            PSD_ComputerScreen, PSD_DialogueBoxes, PSD_Flyer
    Screens/       PSD_StartScreen, PSD_WinScreen, PSD_EndScreen, PSD_Credits
  Sprites/         Flat PNG exports of single layers
    House/         Pieces of PSD_House (walls, furniture), named <Room>_<Thing>
    Props/         Pieces of PSD_Props_01 (items + their anomaly variants)
  Video/           VID_Transition
Audio/
  Music/           MUS_Day_01 .. MUS_Day_04
  SFX/Items/       One sound per buyable item (SFX_StanleyCup, SFX_Labubu, ...)
  SFX/UI/          SFX_Notification_01 .. 04
Scenes/            Game (the main scene; first in Build Settings)
Scripts/
  Core/            Game-wide state and input (PlayerState, GameInput)
  Interaction/     Clicking on the world (ClickHandler)
  Items/           Buyable items and room population
  Anomalies/       Anomaly data and behaviour
  Debug/           Test-only tools; not shipped
Settings/          URP / renderer / input settings (Unity template, leave as is)
```

## File naming

Asset type prefix, then PascalCase words, then an optional `_Variant` or `_NN` index:

| Prefix  | Type                                   | Example                      |
|---------|----------------------------------------|------------------------------|
| `PSD_`  | Layered Photoshop source               | `PSD_House.psd`              |
| `SPR_`  | Sprite / PNG                           | `SPR_FlowerVase_Anomaly.png` |
| `MUS_`  | Music                                  | `MUS_Day_02.wav`             |
| `SFX_`  | Sound effect                           | `SFX_Notification_03.wav`    |
| `VID_`  | Video                                  | `VID_Transition.mp4`         |
| `PF_`   | Prefab (Unity's closest thing to a Blueprint actor) | `PF_FlowerVase` |
| `DA_`   | ScriptableObject data asset            | `DA_Item_FlowerVase`, `DA_Anomaly_FlowerVase_Grin` |
| `MAT_`  | Material                               | `MAT_Outline`                |
| `ANIM_` / `AC_` | Animation clip / Animator Controller | `ANIM_Radio_Wobble`     |

- No spaces, no leading layer numbers (`03_...`), and no ALL CAPS.
- Anomaly variants use the base name + `_Anomaly` (or `_Anomaly_<What>` if there are several).
- Scenes and C# scripts have **no prefix**. A script's file name must equal its class name (Unity requirement).

## Rules for moving or renaming assets

- Always move the `.meta` file **with** its asset. It holds the asset's GUID, and scenes reference assets by GUID. The easiest way is to do moves inside the Unity Project window.
- Artists: when re-exporting a PSD, save over the file at its new path/name above, so all references stay intact.
