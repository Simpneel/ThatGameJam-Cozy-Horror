# While Stocks Last! — Shiiro Games

2D point-and-click cozy-horror spot-the-difference. You're stuck in a loop of days at home: spot what's wrong, and decide whether to keep buying things.

## Playtesting

1. Open the project in **Unity 6000.6.7f1**. The first open imports everything and takes a few minutes.
2. Open `Assets/Scenes/Game.unity` and press **Play**.
3. Or make a standalone build: **Cozy Horror ▸ Build ▸ Windows (Development)** goes to `Builds/Dev/` (debug keys on); **Build ▸ Windows** and **Build ▸ WebGL (itch.io)** make the release builds in `Builds/Windows/` and `Builds/WebGL/`.

**Controls:** click objects; ← → arrows (or A/D) change rooms; Esc pauses.

**Debug keys** (Editor and Development builds only):

| Key | Does |
|---|---|
| 1 | +5 hidden points |
| 2 | Spawn an anomaly |
| 3 | Show the flyer |
| 4 | Hidden-state readout (day, points, streak, anomalies) |

## Where things are

- [WIN_CONDITIONS.txt](WIN_CONDITIONS.txt) and [DEBUFFS.txt](DEBUFFS.txt): plain-text rules for endings and everything that makes life harder.
- [DESIGN.md](DESIGN.md): rules, endings and every tuning number (all editable on the `Game` object in the Inspector).
- [ANOMALIES.md](ANOMALIES.md): every object and what can go wrong with it (generated).
- [CONVENTIONS.md](CONVENTIONS.md): folder layout and file naming.

**The scene is generated.** `Game.unity` is built from the PSDs by **Cozy Horror ▸ Rebuild Game Scene** (`Assets/Editor/CozyBuilder.cs`).
After changing art, run that menu item. Manual edits to the scene are lost on the next rebuild, so change placement in `CozyBuilder.cs` instead.

**Screen art** lives in `Assets/Art/Source/Screens/`: save over a PSD there and run **Cozy Horror ▸ Rebuild Game Scene**.
