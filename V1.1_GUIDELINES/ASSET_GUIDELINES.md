# While Stocks Last! Asset Guidelines (v1.1)

**For the art and sound teams, Shiiro Games**

This covers how to draw and record for the remade house: 6 rooms and 50 products, each with a glitched version. The game builds every room automatically from your Photoshop layer names. If you follow these rules, a new asset works the moment you save it, with no extra programming.

A printable copy is in this folder as `ASSET_GUIDELINES.pdf`.

---

## Art 1: One PSD per room

### 1.1 Size and files
- Each room is its own file at **1920 × 1080 px** (16:9), the size of the game screen. The current Bathroom and Balcony files already follow this.
- Name them `PSD_Room_Bedroom`, `PSD_Room_TVRoom`, `PSD_Room_Kitchen`, `PSD_Room_Hallway`, `PSD_Room_Bathroom` and `PSD_Room_Balcony`, and save them in `Assets/Art/Source/Rooms/`.
- Keep the **top 90 px** calm: ceiling or wall, with nothing to click. The day and points bar sits there.
- Keep the **bottom-left corner** calm too, about 450 × 120 px. The room name shows there.
- Keep the left and right **middle edges** free of small objects. The room arrows sit there.

> **Why:** the old house squeezed four rooms into one 4096 × 1714 file. Each room came out wider than the screen, which caused the black bars and clipped objects at the edges.

### 1.2 Fixed places
- Draw every product **exactly where it will appear** once bought, on its own layer. The game hides it until the player buys it.
- Spread the 50 products across rooms, roughly 8 per room, so every room grows as the player shops.
- Products can overlap furniture, like a lamp on a cabinet. Avoid stacking two products on the same spot, because each one must be clickable.

---

## Art 2: Layer names do the work

The game reads each layer's name to decide what it is. Use these patterns exactly: capitals and spaces matter.

| Layer name | What it is | In game |
|---|---|---|
| `Background` | Walls, floor, ceiling | Never clickable |
| `Scenery_Staircase` | Fixtures that never change: railings, pipes, built-in shelves | Never clickable, never glitches |
| `Fridge` | Furniture the player starts with | Clickable; can glitch |
| `Fridge Anomaly` | The glitched version of `Fridge` | Swapped in when it glitches |
| `Item_Lamp` | A product (one of the 50) | Hidden until bought |
| `Item_Lamp Anomaly` | The glitched version of `Item_Lamp` | Swapped in when it glitches |

### 2.1 Naming rules
- Every name must be **unique within its file**. Photoshop silently allows duplicates, but Unity renames them `Lamp_1` and the pairing breaks.
- A glitch layer is the **exact normal name, plus a space, plus `Anomaly`**. For example, `Item_Lamp Anomaly` pairs with `Item_Lamp`, but `Lamp anomaly` doesn't.
- Use plain letters, numbers and spaces. Write product names in PascalCase after `Item_`, like `Item_StanleyCup` or `Item_BoardGame`.
- Don't leave layers with names like "Layer 12". Unnamed layers are why several current objects needed fixing by hand.

### 2.2 One object, one layer
Everything that should move and wiggle together goes on **one layer**: the bathtub and its rim, a bed with its blanket and pillows, a towel rail with its towels.

> **Why:** the old bathtub was two layers, so only half of it shook when clicked. Merge parts before saving. If you want an unmerged working copy, keep it in a separate file.

---

## Art 3: Drawing glitches

In the story, glitches are bugs in BuyMe's house that the player reports for a bounty. Some also carry hidden messages from previous testers. Coordinate those messages with the scriptwriter.

**Do**
- Draw the glitch **on the same spot** as the normal object, at the same size or close to it.
- Make it **obvious at game size**. Check it at 100% on a 1080p screen, from arm's length.
- Change something big: the silhouette, a strong colour, a face, text.
- Make text at least **28 px tall**, so it reads on a laptop.
- Give every product and most furniture a glitch. In playtests, drawn glitches were the most fun part of the game.

**Don't**
- Don't only change fine detail. The balcony pot's thin white lines were invisible at game size.
- Don't put tiny details inside small areas. The ghost faces in the door window were too small to notice.
- Don't move the glitch somewhere else in the room. The click area stays where the normal object is.
- Don't make the glitch much smaller than the original, or it gets hard to click.

---

## Art 4: Photoshop settings

Unity reads the raw pixels of each layer. Anything Photoshop draws "live" on top gets lost.

**Fine to use**
- RGB, 8 bits per channel, transparent backgrounds
- Normal pixel layers
- Groups, to keep your file tidy (only layer names matter)
- Hidden layers (they still import)

**Rasterize or avoid**
- Layer styles such as stroke, drop shadow and glow (they disappear)
- Smart objects, adjustment layers, fill layers
- Clipping masks and layer masks (apply them first)
- Blend modes other than Normal (bake them in)

> **Quick test:** apply *Layer → Rasterize → Layer Style*, then hide every other layer. If the layer still looks the same, Unity will see it the same way.

---

## Art 5: Shop icons
- Put all icons in one file, `PSD_ShopIcons`, in `Assets/Art/Source/UI/`. Give each product one layer named to match it: `Icon_Lamp` for `Item_Lamp`.
- Draw each icon to fit a **512 × 512 px** square, centred, on transparency, with the product facing the viewer.
- Keep the style consistent across all 50. The shop shows them side by side, four at a time.

---

## Sound

| Type | Format | Length | Loudness |
|---|---|---|---|
| Click and product sounds | WAV, 48 kHz, 16- or 24-bit, **mono** | 0.2–1.2 s | Around −18 LUFS, peaks below −1 dBFS |
| UI and event sounds | WAV, 48 kHz, mono | 0.2–2 s (stings up to 5 s) | Around −18 LUFS |
| Music | WAV, 48 kHz, **stereo** | 1.5–3 min, seamless loop | Around −20 LUFS, under the effects |

### S.1 The rules that matter most
- **No silence at the start.** A click sound must begin within 10 ms, or clicking feels laggy.
- **Match names to the art.** `SFX_Lamp.wav` goes with `Item_Lamp`.
  - A glitched version of a sound is optional: name it `SFX_Lamp_Anomaly.wav`.
  - Without one, the game plays the normal sound pitched down.
- **Music loops cleanly.** Leave no fade-out tail, so the last beat runs straight into the first. Name new tracks `MUS_Day_05` and onwards.
- Keep levels consistent across the set, so no single product is jarringly loud.
- Save files in `Assets/Audio/SFX/Items/`, `Assets/Audio/SFX/UI/` and `Assets/Audio/Music/`.

### S.2 Game sounds we need
At the moment one notification chime is reused for fixing a glitch, "can't afford" and the glitched flyer, so success and failure sound the same. Distinct sounds will make the game much easier to read.

| File name | When it plays | Feel |
|---|---|---|
| `SFX_BountyPaid` | A glitch is fixed and pays a bounty | Bright, rewarding, short |
| `SFX_BountyExpired` | A glitch is fixed too late to pay | Dull thunk, flat |
| `SFX_CantAfford` | Buying without enough points | Soft "nope" buzz |
| `SFX_Purchase` | A product is bought | Checkout ding, a little too cheerful |
| `SFX_DeliveryDrop` | A delivery box lands in the hallway | Cardboard thud |
| `SFX_EmailArrive` | A new email arrives in the inbox | Corporate notification |
| `SFX_FlyerPop` | The BuyMe flyer appears | Paper slide, ad jingle |
| `SFX_GlitchCall` | A glitch twitches to get attention | Digital stutter, uneasy |
| `SFX_LightFlicker` | Lights flicker when glitches pile up | Electrical buzz |
| `SFX_ForcedSleep` | The player is too tired and passes out | Heavy, fading |
| `SFX_DayStart` | Each morning begins | A gentle wake-up that gets less gentle over the days |
| `SFX_Ending_*` | Each story ending (list from the scriptwriter) | Stings, 3–5 s |

Story events, such as a doorbell or a power cut, will come from the scriptwriter's event list. We'll share it when it's ready.

---

## The product catalog sheet

One shared spreadsheet tracks all 50 products, so art, sound, writing and programming stay in sync. The game will read it directly, so keep the column names exactly as they are.

| Column | Example | Filled in by |
|---|---|---|
| `Id` | `Lamp` (matches `Item_Lamp`, `Icon_Lamp`, `SFX_Lamp`) | Art lead |
| `DisplayName` | Mood Lamp 3000 | Scriptwriter |
| `Room` | Bedroom | Art |
| `PriceTier` | 1–4 (exact prices set later) | Design |
| `Art` / `Anomaly` / `Icon` | todo · drafted · done | Art |
| `Sound` | todo · drafted · done | Sound |
| `ShopText` | "Light that loves you back." | Scriptwriter |
| `Notes` | Anomaly hides a tester's message | Anyone |

---

## Before you hand over

- [ ] Saved over the same file name, in the same folder (new files are named as above)
- [ ] Every clickable thing is one layer, with a unique, readable name
- [ ] Every glitch layer is the normal name plus " Anomaly", drawn on the same spot
- [ ] No layer styles, smart objects, masks or special blend modes left
- [ ] Glitches checked at 100% zoom on a 1080p screen
- [ ] Sounds start instantly and are named to match; music loops cleanly
- [ ] Catalog sheet updated, and any **renamed or deleted layers** reported to the programmers

> **Renaming matters:** adding layers is always safe. Renaming or deleting one can unhook it from the game, so a quick message saves a confusing bug.

---

*While Stocks Last! by Shiiro Games. Send questions about these rules to the programming team. Version 1.1, November 2026.*
