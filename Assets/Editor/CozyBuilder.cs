using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Video;

/// Builds Assets/Scenes/Game.unity from the PSDs: rooms, clickable props, shop items, UI and all wiring.
/// Menu: Cozy Horror ▸ Rebuild Game Scene.  Headless: -executeMethod CozyBuilder.Build
/// NOTE: re-running this replaces the scene, so tweak placement here (ItemSpecs) rather than by hand.
public static class CozyBuilder
{
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string HousePsd = "Assets/Art/Source/Rooms/PSD_House.psd";
    const string HouseAnomalyPsd = "Assets/Art/Source/Rooms/PSD_House_Anomaly.psd";
    const string BathroomPsd = "Assets/Art/Source/Rooms/PSD_Bathroom.psd";
    const string BalconyPsd = "Assets/Art/Source/Rooms/PSD_Balcony.psd";
    const string Props1Psd = "Assets/Art/Source/Props/PSD_Props_01.psd";
    const string Props2Psd = "Assets/Art/Source/Props/PSD_Props_02.psd";
    const string ShopPsd = "Assets/Art/Source/UI/PSD_ComputerScreen.psd";
    const string DialogPsd = "Assets/Art/Source/UI/PSD_DialogueBoxes.psd";
    const string FlyerPsd = "Assets/Art/Source/Props/PSD_Flyer.psd";
    const string ArrowPng = "Assets/Art/Sprites/UI/SPR_UI_Arrow.png";

    static readonly Color Dark = new Color(0.2f, 0.2f, 0.2f, 0.96f);
    static Font font;

    // ------------------------------------------------------------------ data

    /// Where a room object lives, as a fraction of its room (x across, y = where its BOTTOM sits), and how tall it is.
    class ItemSpec
    {
        public string id, display, psd, layer, anomalyLayer, sound, icon;
        public int room, order = 40;
        public float nx, ny, height;
        public string attachLayer; // extra layer that moves with it (headphone stand)
    }

    const int Bedroom = 0, TVRoom = 1, Kitchen = 2, Hallway = 3, Bathroom = 4, Balcony = 5;

    // Shop catalogue order. Prices come from the card slot (1, 2, 4, 2), see Shop.slotPrices.
    static readonly ItemSpec[] ShopSpecs =
    {
        new ItemSpec { id = "flower_vase", display = "Flower Vase", psd = Props1Psd, layer = "Flower Vase", anomalyLayer = "Flower Vase Anomaly", room = Bedroom, nx = 0.071f, ny = 0.715f, height = 1.45f, sound = "Flowers" },
        new ItemSpec { id = "radio", display = "Radio", psd = HousePsd, layer = "@radio", sound = "Radio", icon = "Radio" },
        new ItemSpec { id = "headphones", display = "Headphones", psd = Props1Psd, layer = "Headphones", attachLayer = "Headphone Stand", room = TVRoom, nx = 0.905f, ny = 0.37f, height = 1.45f, sound = "Headphones" },
        new ItemSpec { id = "board_game", display = "Board Game", psd = Props1Psd, layer = "Board Game", anomalyLayer = "Anomaly", room = TVRoom, nx = 0.62f, ny = 0.035f, height = 0.75f, sound = "BoardGame" },
        new ItemSpec { id = "plant", display = "Plant", psd = Props2Psd, layer = "plant", anomalyLayer = "anamoly plant", room = Kitchen, nx = 0.957f, ny = 0.06f, height = 2.3f, sound = "Plant" },
        new ItemSpec { id = "lamp", display = "Lamp", psd = Props2Psd, layer = "Lamp", anomalyLayer = "Lamp Anomaly", room = Bedroom, nx = 0.165f, ny = 0.15f, height = 3.2f, sound = "Lamp" },
        new ItemSpec { id = "alexa", display = "Smart Speaker", psd = HousePsd, layer = "@speaker", sound = "Notification_03", icon = "Alexa" },
        new ItemSpec { id = "weights", display = "Kettlebell", psd = Props2Psd, layer = "weights", anomalyLayer = "Anamoly weights", room = Bedroom, nx = 0.57f, ny = 0.03f, height = 1.15f, sound = "RunningShoes" },
        new ItemSpec { id = "goat_button", display = "Goat Button", psd = Props2Psd, layer = "Goat", anomalyLayer = "Anamoly Goat", room = TVRoom, nx = 0.57f, ny = 0.37f, height = 0.8f, sound = "ScreamingGoatButton" },
    };

    // Hallway package-rack slots for delivery boxes (one appears per purchase).
    static readonly (string layer, float nx, float ny, float h)[] BoxSlots =
    {
        ("Box 2", 0.575f, 0.33f, 1.0f), ("Box 4", 0.83f, 0.33f, 1.0f), ("Box 3", 0.69f, 0.09f, 1.0f),
        ("Box 5", 0.815f, 0.09f, 0.95f), ("Box 6", 0.565f, 0.595f, 0.85f), ("Box 7", 0.78f, 0.595f, 0.85f), ("Box 1", 0.66f, 0.745f, 0.55f),
    };

    static readonly Dictionary<string, string> HouseSounds = new Dictionary<string, string>
    {
        { "Bed", "Cloth" }, { "Blanket", "Cloth" }, { "Pillows", "Cloth" }, { "Sofa", "Cloth" }, { "Bedroom Rug", "Cloth" },
        { "Bedroom Cabinet", "Cabinet" }, { "Kitchen Sink Cabinet", "Cabinet" }, { "Package Rack", "Cabinet" }, { "Door", "Cabinet" }, { "Door_1", "Cabinet" }, { "Bathroom", "Cabinet" },
        { "Desk", "Book" }, { "Fridge", "SoftDrink" }, { "Stove Island", "Coffee" }, { "Kitchen Sink", "TeaBag" },
        { "Monitor", "Notification_01" }, { "@laptop", "Notification_02" },
    };

    static readonly Dictionary<string, (string name, string sound)> BathroomLayers = new Dictionary<string, (string, string)>
    {
        { "Layer 5", ("Shower Curtain", "Cloth") }, { "Layer 6", ("Curtain Fold", "Cloth") }, { "Layer 8", ("Small Bottle", null) },
        { "Layer 3", ("Tub Rim", null) }, { "Layer 4", ("Tub", null) }, { "Layer 7", ("Shower Pipe", null) }, { "Layer 12", ("Towels", "Cloth") },
        { "Layer 10", ("Towel Rail", null) }, { "Layer 16", ("Soap", null) }, { "Layer 15", ("Bottles", null) }, { "Layer 17", ("Red Bottle", null) },
        { "Layer 9", ("Bath Mat", "Cloth") },
    };

    static readonly Dictionary<string, (string name, string sound)> BalconyLayers = new Dictionary<string, (string, string)>
    {
        { "Layer 10", ("Delivery Truck", "Notification_04") }, { "Layer 7_1", ("Street Lamp L", null) }, { "Layer 7_2", ("Street Lamp R", null) },
        { "Layer 15", ("Balcony Plant", "Plant") }, { "Layer 10_1", ("Planter", "Plant") }, { "Layer 15_1", ("Scissors", null) },
        { "Layer 13", ("Hook Rail", null) }, { "Layer 9", ("Cup", "Coffee") },
    };

    // ------------------------------------------------------------------ entry

    [MenuItem("Cozy Horror/Rebuild Game Scene")]
    public static void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureArrowSprite();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Cameras + light
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.depth = 0;
        camGo.AddComponent<AudioListener>();

        var bars = new GameObject("Letterbox Camera").AddComponent<Camera>();
        bars.clearFlags = CameraClearFlags.SolidColor;
        bars.backgroundColor = Color.black;
        bars.cullingMask = 0;
        bars.depth = -10;

        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        var lso = new SerializedObject(light);
        lso.FindProperty("m_LightType").intValue = 4; // Global
        lso.FindProperty("m_Intensity").floatValue = 1f;
        lso.ApplyModifiedPropertiesWithoutUndo();

        // World
        var world = new GameObject("World").transform;
        var house = Psd(HousePsd, world, Vector3.zero);
        // The wall quadrants overlap a little; inset so a room never shows a sliver of its neighbour.
        var rooms = HouseRooms(house).Select(r => new Rect(r.x + 0.2f, r.y + 0.2f, r.width - 0.4f, r.height - 0.4f)).ToList();
        var bathroom = Psd(BathroomPsd, world, new Vector3(rooms.Max(r => r.xMax) + 30f, 9f, 0));
        var balcony = Psd(BalconyPsd, world, new Vector3(rooms.Max(r => r.xMax) + 60f, 9f, 0));
        rooms.Add(LayerBounds(bathroom, "Background"));
        rooms.Add(LayerBounds(balcony, "Background"));

        var props = new List<Prop>();
        var bedProps = new List<Prop>();
        var computerProps = new List<Prop>();
        SetupHouse(house, props, bedProps, computerProps);
        WireHouseAnomalies(house);
        SetupRoomPsd(bathroom, BathroomLayers, new[] { "Layer 3_1", "Layer 1", "Background" }, null, props);
        // Shower curtain: "Curtain Fold" is the open version. Clicking swaps between closed and open.
        var curtainClosed = bathroom.Find("Shower Curtain").GetComponent<Prop>();
        var curtainOpen = bathroom.Find("Curtain Fold").GetComponent<Prop>();
        curtainOpen.name = "Shower Curtain (Open)";
        curtainClosed.toggleTo = curtainOpen; curtainOpen.toggleTo = curtainClosed;
        curtainOpen.gameObject.SetActive(false);
        SetupRoomPsd(balcony, BalconyLayers, new[] { "Layer 7", "Layer 6", "Layer 6_1", "Layer 2", "Layer 1", "Background" }, new[] { "Layer 9_1" }, props);
        // Balcony cup: the lower "Layer 9_1" is its drawn anomaly version (white lines).
        balcony.Find("Cup").GetComponent<Prop>().anomalySprite = balcony.Find("Layer 9_1").GetComponent<SpriteRenderer>().sprite;

        var itemsRoot = new GameObject("Shop Items").transform;
        itemsRoot.SetParent(world);
        var shopItems = BuildShopItems(house, itemsRoot, rooms);
        var boxes = BuildDeliveryBoxes(itemsRoot, rooms[Hallway]);

        // Systems
        var game = new GameObject("Game");
        var gm = game.AddComponent<GameManager>();
        var shop = game.AddComponent<Shop>();
        var director = game.AddComponent<AnomalyDirector>();
        var musicSrc = game.AddComponent<AudioSource>();
        var music = game.AddComponent<MusicPlayer>();
        var debug = game.AddComponent<DebugTools>();
        var click = game.AddComponent<ClickHandler>();
        Wire(click, ("cam", cam));
        music.dayTracks = Enumerable.Range(1, 4).Select(i => Load<AudioClip>($"Assets/Audio/Music/MUS_Day_0{i}.wav")).ToArray();
        musicSrc.playOnAwake = false;

        var roomCam = camGo.AddComponent<RoomCamera>();
        roomCam.rooms = new[] { "Bedroom", "TV Room", "Kitchen", "Hallway", "Bathroom", "Balcony" }
            .Select((n, i) => new RoomCamera.Room { name = n, area = rooms[i] }).ToArray();
        roomCam.startRoom = Bedroom;

        // UI
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule))
            .GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var canvas = BuildCanvas();
        var ui = canvas.transform;

        var leftArrow = ArrowButton(ui, "Arrow Left", -1);
        var rightArrow = ArrowButton(ui, "Arrow Right", +1);
        BuildRoomLabel(ui, roomCam);
        var voice = BuildHouseVoice(ui, game);
        Wire(click, ("hoverCursor", EnsureCursorTexture()), ("missPuff", BuildMissPuff(ui)));

        var flyer = BuildFlyer(ui, shop);
        BuildShopWindow(ui, shop);
        var bedDialog = BuildBedDialog(ui);
        var screens = BuildScreens(ui);
        var pause = BuildPause(ui);
        BuildHelpHint(ui);
        ui.Find("Day Banner").SetSiblingIndex(ui.Find("Flyer").GetSiblingIndex()); // banner sits behind flyer/shop/dialogs

        Wire(roomCam, ("leftArrow", leftArrow), ("rightArrow", rightArrow), ("fade", screens.fade), ("moveSound", null));
        Wire(director, ("rooms", roomCam));
        Wire(debug, ("director", director), ("flyer", flyer));

        shop.items = shopItems;
        shop.deliveryBoxes = boxes;
        shop.openSound = UiSfx(1);
        shop.buySound = UiSfx(3);
        shop.denySound = UiSfx(4);

        gm.screens = screens; gm.rooms = roomCam; gm.shop = shop; gm.director = director; gm.flyer = flyer;
        gm.music = music; gm.bedDialog = bedDialog; gm.pause = pause;
        gm.bedProps = bedProps.ToArray();
        gm.computerProps = computerProps.ToArray();
        gm.bedroomIndex = Bedroom;
        gm.anomalyFixedSound = UiSfx(4);
        director.callFallbackSound = UiSfx(2);

        // Atmosphere: creep vignette (behind all UI), dimming global light, glowing bed.
        var bed = bedProps.First(p => !p.canBeAnomalous);
        var bedBounds = bed.GetComponent<SpriteRenderer>().bounds;
        var glow = new GameObject("Bed Glow").AddComponent<SpriteRenderer>();
        glow.transform.SetParent(world);
        glow.sprite = EnsureRadialSprite(GlowPng, false);
        glow.sharedMaterial = Load<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat"); // ignores the dusk
        glow.color = new Color(1f, 0.8f, 0.45f, 0f);
        glow.sortingOrder = 7; // in front of the room walls (order 3-6), behind the furniture (8+)
        // Size it around the bed but keep it inside the bedroom so it never bleeds into the kitchen or TV room.
        var bedroomRect = rooms[Bedroom];
        Vector2 gc = bedBounds.center;
        float halfW = Mathf.Min(bedBounds.extents.x * 1.6f, bedroomRect.width / 2f);
        float halfH = Mathf.Min(bedBounds.extents.y * 2.6f, bedroomRect.height / 2f);
        // Keep full size; slide the centre inwards so the halo fits inside the room.
        gc.x = Mathf.Clamp(gc.x, bedroomRect.xMin + halfW, bedroomRect.xMax - halfW);
        gc.y = Mathf.Clamp(gc.y, bedroomRect.yMin + halfH, bedroomRect.yMax - halfH);
        glow.transform.position = gc;
        glow.transform.localScale = new Vector3(halfW * 2f / glow.sprite.bounds.size.x, halfH * 2f / glow.sprite.bounds.size.y, 1f);
        var vignetteRt = Full(ui, "Creep Vignette");
        vignetteRt.SetSiblingIndex(0);
        var vignette = Img(vignetteRt, EnsureRadialSprite(VignettePng, true), new Color(0f, 0f, 0f, 0f), false);
        vignetteRt.gameObject.AddComponent<Canvas>(); // own canvas: its per-frame fading doesn't re-batch the rest of the UI
        var atm = game.AddComponent<Atmosphere>();
        atm.director = director; atm.music = music; atm.vignette = vignette;
        atm.globalLight = light; atm.bedGlow = glow; atm.bed = bed;
        gm.atmosphere = atm;
        gm.mainMenu = builtMenu;
        atm.voice = voice;
        atm.flickerSound = UiSfx(2);

        // Who talks: anything that exists from the start, plus the radio and smart speaker once bought.
        var allProps = Object.FindObjectsByType<Prop>(FindObjectsInactive.Include);
        HouseVoice.Speaker Speaker(string propName, string label, bool talksOnClick) =>
            new HouseVoice.Speaker { prop = allProps.FirstOrDefault(p => p.name == propName), label = label, talksOnClick = talksOnClick };
        voice.speakers = new[]
        {
            Speaker("TV", "TV", true),
            Speaker("Laptop", "LAPTOP", false), // clicking it opens the shop instead
            Speaker("Radio", "RADIO", true),
            Speaker("Smart Speaker", "SMART SPEAKER", true),
        };
        if (voice.speakers.Any(s => s.prop == null)) Debug.LogError("[Cozy] A HouseVoice speaker prop was not found");

        AssignAnomalyKinds(roomCam.rooms);

        // Unpack the room PSDs into plain objects. As prefab instances, every rename / hide / added component was an
        // "override" pointing at a PSD layer; if an artist edits the PSD's layers those overrides break and Unity
        // logs prefab errors. Sprites still reference the PSD, so redrawn art keeps updating; re-run this builder
        // after adding, removing or renaming layers.
        foreach (var psdRoot in new[] { house, bathroom, balcony })
            PrefabUtility.UnpackPrefabInstance(psdRoot.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log($"[Cozy] Built {ScenePath}: {props.Count} props, {shopItems.Length} shop items, {boxes.Length} boxes, {rooms.Count} rooms");
    }

    // ------------------------------------------------------------------ world

    /// Drawn anomaly versions from PSD_House_Anomaly (same canvas as PSD_House): house prop name -> anomaly layer.
    static readonly (string prop, string layer)[] HouseAnomalyPairs =
    {
        ("Bedroom Cabinet", "Bedroom Cabinet Anomaly"),
        ("Door_1", "Door Anomaly"),
        ("Kitchen Vents", "Kitchen Vents Anomaly"),
        ("Kitchen Sink", "Kitchen Sink Anomaly"),
        ("Kitchen Window", "Kitchen Window"),
        ("TV", "TV Anomaly"),
        ("TV Room Window", "TV Room Window Anomaly"),
        ("Bathroom", "Bathroom Anomaly"),
    };

    static void WireHouseAnomalies(Transform house)
    {
        var src = (GameObject)Object.Instantiate(Load<GameObject>(HouseAnomalyPsd));
        src.transform.position = house.position;
        var props = house.GetComponentsInChildren<Prop>(true);
        foreach (var (propName, layer) in HouseAnomalyPairs)
        {
            var prop = props.FirstOrDefault(p => p.name == propName);
            var a = src.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(s => s.name == layer);
            if (prop == null || a == null) { Debug.LogError($"[Cozy] Anomaly pair missing: {propName} / {layer}"); continue; }
            prop.anomalySprite = a.sprite;
            prop.anomalySpriteScale = 1f;
            prop.anomalyOffset = prop.transform.parent.InverseTransformVector(a.transform.position - prop.transform.position);
        }
        Object.DestroyImmediate(src);
    }

    static Transform Psd(string path, Transform parent, Vector3 pos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path));
        go.transform.SetParent(parent);
        go.transform.position = pos;
        return go.transform;
    }

    static Rect LayerBounds(Transform root, string layer)
    {
        var b = root.Find(layer).GetComponent<SpriteRenderer>().bounds;
        return new Rect(b.min, b.size);
    }

    /// The four wall quadrants of the house, as [Bedroom, TV Room, Kitchen, Hallway].
    static IEnumerable<Rect> HouseRooms(Transform house)
    {
        var quads = house.GetComponentsInChildren<SpriteRenderer>()
            .Where(s => s.name.StartsWith("From selection") && s.bounds.size.x > 10f)
            .Select(s => new Rect(s.bounds.min, s.bounds.size)).ToList();
        float cx = quads.Average(r => r.center.x), cy = quads.Average(r => r.center.y);
        yield return quads.First(r => r.center.x < cx && r.center.y > cy); // bedroom: top-left
        yield return quads.First(r => r.center.x > cx && r.center.y > cy); // TV room: top-right
        yield return quads.First(r => r.center.x < cx && r.center.y < cy); // kitchen: bottom-left
        yield return quads.First(r => r.center.x > cx && r.center.y < cy); // hallway: bottom-right
    }

    /// Identify the unnamed "Layer 35"/"Layer 29" house layers by their pixel size.
    static string HouseAlias(SpriteRenderer sr)
    {
        if (!sr.name.StartsWith("Layer 35") && !sr.name.StartsWith("Layer 29")) return null;
        var r = sr.sprite.rect;
        if (sr.name.StartsWith("Layer 29")) return "@laptop";
        if (Mathf.Abs(r.width - 106.5f) < 2) return "@radio";
        if (Mathf.Abs(r.width - 46.5f) < 2) return "@speaker";
        return "@package";
    }

    static void SetupHouse(Transform house, List<Prop> props, List<Prop> bedProps, List<Prop> computerProps)
    {
        var anomalyVents = house.Find("Kitchen Vents");     // dark version drawn on top
        var normalVents = house.Find("Kitchen Vents_1");

        foreach (var sr in house.GetComponentsInChildren<SpriteRenderer>(true))
        {
            string n = sr.name;
            if (n == "Background" || n == "Layer 7" || n.StartsWith("From selection")) continue;
            if (sr.transform == anomalyVents) { sr.gameObject.SetActive(false); continue; }

            string alias = HouseAlias(sr);
            if (alias == "@radio" || alias == "@speaker") continue; // become shop items
            if (alias == "@laptop") sr.name = "Laptop";
            if (alias == "@package") sr.name = "Package";

            HouseSounds.TryGetValue(alias ?? n, out var sound);
            var prop = MakeProp(sr.gameObject, sound);
            props.Add(prop);

            if (n == "Bed") { prop.canBeAnomalous = false; bedProps.Add(prop); }
            if (n == "Blanket" || n == "Pillows") bedProps.Add(prop);
            if (n == "Monitor") { prop.canBeAnomalous = false; computerProps.Add(prop); }
            if (alias == "@laptop") computerProps.Add(prop);
            if (sr.transform == normalVents) sr.name = "Kitchen Vents";
            if (n == "Bedroom Rug")
            {
                prop.anomalySprite = PsdSprite(Props1Psd, "Anomaly Rug");
                prop.anomalySpriteScale = sr.sprite.bounds.size.x / prop.anomalySprite.bounds.size.x;
            }
        }
    }

    static void SetupRoomPsd(Transform root, Dictionary<string, (string name, string sound)> names, string[] background, string[] hidden, List<Prop> props)
    {
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            string n = sr.name;
            if (background.Contains(n)) continue;
            if (hidden != null && hidden.Contains(n)) { sr.gameObject.SetActive(false); continue; }
            names.TryGetValue(n, out var info);
            if (info.name != null) sr.name = info.name;
            props.Add(MakeProp(sr.gameObject, info.sound));
        }
    }

    static Prop MakeProp(GameObject go, string sound)
    {
        if (go.GetComponent<Collider2D>() == null) go.AddComponent<PolygonCollider2D>();
        var prop = go.GetComponent<Prop>() ?? go.AddComponent<Prop>();
        prop.clickSound = sound == null ? null : (sound.StartsWith("Notification") ? UiSfx(int.Parse(sound.Substring(13))) : ItemSfx(sound));
        return prop;
    }

    static ShopItem[] BuildShopItems(Transform house, Transform parent, List<Rect> rooms)
    {
        var p1 = Unpacked(Props1Psd, parent);
        var p2 = Unpacked(Props2Psd, parent);
        var result = new List<ShopItem>();

        foreach (var spec in ShopSpecs)
        {
            GameObject go;
            if (spec.layer.StartsWith("@"))
            {
                go = house.GetComponentsInChildren<SpriteRenderer>(true).First(s => HouseAlias(s) == spec.layer).gameObject;
                go.name = spec.display;
            }
            else
            {
                var src = spec.psd == Props1Psd ? p1 : p2;
                go = src.Find(spec.layer).gameObject;
                go.name = spec.display;
                go.transform.SetParent(parent, true);
                if (spec.attachLayer != null)
                {
                    var extra = src.Find(spec.attachLayer);
                    extra.SetParent(go.transform, true);
                    extra.GetComponent<SpriteRenderer>().sortingOrder = spec.order - 1;
                    if (extra.GetComponent<Collider2D>() == null) extra.gameObject.AddComponent<PolygonCollider2D>();
                }
                Place(go.transform, rooms[spec.room], spec.nx, spec.ny, spec.height);
                go.GetComponent<SpriteRenderer>().sortingOrder = spec.order;
            }

            var prop = MakeProp(go, spec.sound);
            if (spec.anomalyLayer != null)
                prop.anomalySprite = (spec.psd == Props1Psd ? p1 : p2).Find(spec.anomalyLayer).GetComponent<SpriteRenderer>().sprite;

            var item = go.AddComponent<ShopItem>();
            item.id = spec.id;
            item.displayName = spec.display;
            item.icon = spec.icon != null ? PsdSprite(Props1Psd, spec.icon) : null;
            result.Add(item);
        }
        Object.DestroyImmediate(p2.gameObject); // the unused remainder (anomaly layers live on as sprite references)
        return result.ToArray();
    }

    static GameObject[] BuildDeliveryBoxes(Transform parent, Rect hallway)
    {
        var p1 = parent.Find("PSD_Props_01");
        var boxes = new List<GameObject>();
        foreach (var slot in BoxSlots)
        {
            var box = p1.Find(slot.layer);
            box.SetParent(parent, true);
            box.name = "Delivery " + slot.layer;
            Place(box, hallway, slot.nx, slot.ny, slot.h);
            box.GetComponent<SpriteRenderer>().sortingOrder = 45;
            MakeProp(box.gameObject, "Cabinet");
            boxes.Add(box.gameObject);
        }
        Object.DestroyImmediate(p1.gameObject);
        return boxes.ToArray();
    }

    static Transform Unpacked(string psd, Transform parent)
    {
        var t = Psd(psd, parent, Vector3.zero);
        PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        t.name = Path.GetFileNameWithoutExtension(psd);
        return t;
    }

    /// Scales the object (and anything attached) to `height`, then stands its bottom-centre on the anchor point.
    static void Place(Transform t, Rect room, float nx, float ny, float height)
    {
        var b = Bounds(t);
        t.localScale *= height / b.size.y;
        b = Bounds(t);
        var anchor = new Vector2(room.xMin + nx * room.width, room.yMin + ny * room.height);
        t.position += (Vector3)(anchor - new Vector2(b.center.x, b.min.y));
    }

    static Bounds Bounds(Transform t)
    {
        var rs = t.GetComponentsInChildren<SpriteRenderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    // ------------------------------------------------------------------ UI

    static Canvas BuildCanvas()
    {
        var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Full(Transform parent, string name)
    {
        var rt = Rect(parent, name, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return rt;
    }

    static Image Img(RectTransform rt, Sprite sprite, Color? color = null, bool raycast = true)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color ?? Color.white;
        img.raycastTarget = raycast;
        return img;
    }

    static Text Label(Transform parent, string name, string text, int size, Vector2 pos, Vector2 box, Color? color = null, FontStyle style = FontStyle.Bold)
    {
        var rt = Rect(parent, name, pos, box);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color ?? Color.white;
        t.raycastTarget = false;
        return t;
    }

    /// Plain white text on a dark grey box — the game's one UI style.
    static Button TextButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, int fontSize = 40)
    {
        var rt = Rect(parent, name, pos, size);
        var img = Img(rt, null, Dark);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        b.colors = colors;
        Label(rt, "Label", text, fontSize, Vector2.zero, size);
        return b;
    }

    static Button InvisibleButton(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var rt = Rect(parent, name, pos, size);
        var img = Img(rt, null, new Color(1, 1, 1, 0));
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.None;
        return b;
    }

    /// Recreates a PSD's layers as UI Images at their exact layout (1 world unit = 100 canvas px).
    static List<(Sprite sprite, RectTransform rt)> PsdToUI(string psd, Transform parent, params string[] skip)
    {
        var tmp = (GameObject)Object.Instantiate(Load<GameObject>(psd));
        var bg = (tmp.transform.Find("Background") ?? tmp.transform.Find("Layer 1")).GetComponent<SpriteRenderer>().bounds.center;
        var map = new List<(Sprite sprite, RectTransform rt)>();
        foreach (var sr in tmp.GetComponentsInChildren<SpriteRenderer>().OrderBy(s => s.sortingOrder))
        {
            if (skip.Contains(sr.name)) continue;
            var b = sr.bounds;
            var rt = Rect(parent, sr.name, (Vector2)(b.center - bg) * 100f, (Vector2)b.size * 100f);
            Img(rt, sr.sprite, null, false);
            map.Add((sr.sprite, rt));
        }
        Object.DestroyImmediate(tmp);
        return map;
    }

    static Button ArrowButton(Transform ui, string name, int dir)
    {
        var rt = Rect(ui, name, Vector2.zero, new Vector2(110, 110));
        rt.anchorMin = rt.anchorMax = new Vector2(dir < 0 ? 0f : 1f, 0.5f);
        rt.anchoredPosition = new Vector2(dir < 0 ? 80f : -80f, 0f);
        rt.localScale = new Vector3(dir, 1, 1);
        var img = Img(rt, Load<Sprite>(ArrowPng));
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.normalColor = new Color(1, 1, 1, 0.75f);
        colors.highlightedColor = Color.white;
        b.colors = colors;
        return b;
    }

    static FlyerPopup BuildFlyer(Transform ui, Shop shop)
    {
        var layer = Full(ui, "Flyer");
        var card = Rect(layer, "Flyer Card", new Vector2(560, 20), new Vector2(466, 640));
        Img(card, null, new Color(1, 1, 1, 0)); // click target
        var art = Rect(card, "Art", Vector2.zero, new Vector2(932, 1280));
        art.localScale = Vector3.one * 0.5f;
        var flyerLayers = PsdToUI(FlyerPsd, art);
        GameObject[] Layers(params string[] names) => flyerLayers.Where(l => names.Contains(l.rt.name)).Select(l => l.rt.gameObject).ToArray();
        var flyerButton = card.gameObject.AddComponent<Button>();
        flyerButton.transition = Selectable.Transition.None;
        var close = TextButton(card, "Close", "X", new Vector2(215, 302), new Vector2(64, 64), 36);
        var popup = layer.gameObject.AddComponent<FlyerPopup>();
        Wire(popup, ("card", card), ("flyerButton", flyerButton), ("closeButton", close), ("shop", shop), ("popSound", UiSfx(2)));
        popup.normalLayers = Layers("Inserted Image", "Inserted Image_1");
        popup.anomalyLayers = Layers("Anomaly", "Anomaly_1");
        popup.fixedSound = UiSfx(4);
        if (popup.normalLayers.Length != 2 || popup.anomalyLayers.Length != 2) Debug.LogError("[Cozy] Flyer layers not found");
        return popup;
    }

    static void BuildShopWindow(Transform ui, Shop shop)
    {
        var window = Full(ui, "Shop Window");
        Img(window, null, new Color(0, 0, 0, 0.55f)); // blocks clicks to the room
        var page = Rect(window, "Page", Vector2.zero, new Vector2(1920, 1080));
        var layers = PsdToUI(ShopPsd, page, "Background");

        var cardBgs = layers.Where(kv => Mathf.Abs(kv.sprite.rect.width - 151f) < 3f).OrderBy(kv => kv.rt.anchoredPosition.x).Select(kv => kv.rt).ToList();
        var buyBtns = layers.Where(kv => Mathf.Abs(kv.sprite.rect.width - 123.25f) < 3f).OrderBy(kv => kv.rt.anchoredPosition.x).Select(kv => kv.rt).ToList();

        var cards = new List<ShopCard>();
        for (int i = 0; i < cardBgs.Count; i++)
        {
            var bg = cardBgs[i];
            var cardRt = Rect(page, "Card " + (i + 1), bg.anchoredPosition, bg.sizeDelta);
            cardRt.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0); // invisible root
            var card = cardRt.gameObject.AddComponent<ShopCard>();
            card.group = cardRt.gameObject.AddComponent<CanvasGroup>();
            bg.SetParent(cardRt, true);
            card.icon = Img(Rect(cardRt, "Icon", new Vector2(-6, 0), bg.sizeDelta * 0.7f), null, null, false);
            card.icon.preserveAspect = true;
            card.nameLabel = Label(cardRt, "Name", "Item", 24, new Vector2(0, bg.sizeDelta.y / 2 - 26), new Vector2(bg.sizeDelta.x - 20, 40), new Color(0.25f, 0.25f, 0.3f));
            var owned = Rect(cardRt, "Owned", Vector2.zero, new Vector2(200, 56));
            Img(owned, null, Dark, false);
            Label(owned, "Label", "OWNED", 32, Vector2.zero, owned.sizeDelta);
            card.ownedBadge = owned.gameObject;
            var sticker = Rect(cardRt, "Price Sticker", new Vector2(bg.sizeDelta.x / 2 - 20, -bg.sizeDelta.y / 2 + 60), new Vector2(150, 54));
            Img(sticker, null, new Color(0.78f, 0.1f, 0.12f), false);
            sticker.localRotation = Quaternion.Euler(0, 0, 12f);
            card.priceStickerLabel = Label(sticker, "Label", "NOW 4!", 30, Vector2.zero, sticker.sizeDelta);
            card.priceSticker = sticker.gameObject;
            sticker.gameObject.SetActive(false);

            var buy = buyBtns[i];
            buy.SetParent(cardRt, true);
            var buyImg = buy.GetComponent<Image>();
            buyImg.raycastTarget = true;
            var button = buy.gameObject.AddComponent<Button>();
            button.targetGraphic = buyImg;
            card.buyButton = button;
            cards.Add(card);
        }

        // Hotspots over art that's baked into the window image: ✕, ← and →.
        shop.closeButton = InvisibleButton(page, "Close (X)", new Vector2(689, 390), new Vector2(80, 76));
        shop.prevButton = InvisibleButton(page, "Prev (<-)", new Vector2(-759, 340), new Vector2(80, 56));
        shop.nextButton = InvisibleButton(page, "Next (->)", new Vector2(-671, 340), new Vector2(80, 56));
        shop.cartLabel = Label(page, "Cart Count", "", 34, new Vector2(625, 233), new Vector2(90, 60), new Color(0.15f, 0.15f, 0.15f));
        shop.cards = cards.ToArray();
        shop.window = window.gameObject;

        var toastRt = Rect(ui, "Toast", new Vector2(0, -480), new Vector2(520, 90));
        Img(toastRt, null, Dark, false);
        var toastLabel = Label(toastRt, "Label", "CAN'T AFFORD", 44, Vector2.zero, toastRt.sizeDelta);
        toastLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        toastLabel.rectTransform.anchorMin = Vector2.zero; toastLabel.rectTransform.anchorMax = Vector2.one; toastLabel.rectTransform.sizeDelta = Vector2.zero;
        var toast = toastRt.gameObject.AddComponent<Toast>();
        Wire(toast, ("label", toastLabel), ("group", toastRt.gameObject.AddComponent<CanvasGroup>()));
        shop.toast = toast;
    }

    static ConfirmDialog BuildBedDialog(Transform ui)
    {
        var layer = Full(ui, "Bed Dialog");
        var panel = Full(layer, "Panel");
        Img(panel, null, new Color(0, 0, 0, 0.45f));
        var map = PsdToUI(DialogPsd, panel, "Background");
        var boxes = map.Where(kv => Mathf.Abs(kv.sprite.rect.width - 404f) < 3f).OrderBy(kv => kv.rt.anchoredPosition.x).Select(kv => kv.rt).ToList();
        Button MakeBtn(RectTransform rt)
        {
            var img = rt.GetComponent<Image>();
            img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            return b;
        }
        var dialog = layer.gameObject.AddComponent<ConfirmDialog>();
        Wire(dialog, ("panel", panel.gameObject), ("yesButton", MakeBtn(boxes[0])), ("noButton", MakeBtn(boxes[1])));
        return dialog;
    }

    static Screens BuildScreens(Transform ui)
    {
        var s = ui.gameObject.AddComponent<Screens>();

        var banner = Rect(ui, "Day Banner", new Vector2(0, -90), new Vector2(360, 110));
        banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 1f);
        Img(banner, null, Dark, false);
        s.dayBannerLabel = Label(banner, "Label", "DAY 1", 64, Vector2.zero, banner.sizeDelta);
        s.dayBanner = banner.gameObject.AddComponent<CanvasGroup>();
        s.dayBanner.blocksRaycasts = false;

        var videoRt = Full(ui, "Transition Video");
        s.videoImage = videoRt.gameObject.AddComponent<RawImage>();
        s.video = ui.gameObject.AddComponent<VideoPlayer>(); // must stay on an always-active object to prepare
        s.video.playOnAwake = false;
        s.video.isLooping = false;
        s.video.renderMode = VideoRenderMode.RenderTexture;
        s.video.audioOutputMode = VideoAudioOutputMode.Direct;

        builtMenu = BuildMainMenu(ui); // under the pages, so Credits and the Welcome card draw over it
        s.titleScreen = Page(ui, "Title Screen", "Assets/Art/Source/Screens/PSD_StartScreen.psd", "click anywhere to begin"); // yellow "Welcome home" card before Day 1; instructions live behind pause > What to do?
        s.winScreen = Page(ui, "Win Screen", "Assets/Art/Source/Screens/PSD_WinScreen.psd", "click to continue");
        s.loseScreen = Page(ui, "Lose Screen", "Assets/Art/Source/Screens/PSD_EndScreen.psd", "click to continue");
        s.creditsScreen = Page(ui, "Credits Screen", "Assets/Art/Source/Screens/PSD_Credits.psd", "click to play again");

        var fade = Full(ui, "Fade");
        Img(fade, null, Color.black);
        s.fade = fade.gameObject.AddComponent<CanvasGroup>();
        return s;
    }

    const string MainMenuPsd = "Assets/Art/Source/Screens/PSD_MainMenu.psd";
    const string InstructionsPsd = "Assets/Art/Source/Screens/PSD_Instructions.psd";
    static MainMenu builtMenu;

    /// Start screen. Uses PSD_MainMenu.psd as the background once it exists; until then a plain placeholder with the title.
    /// Buttons sit in the bottom-right quarter of the screen.
    static MainMenu BuildMainMenu(Transform ui)
    {
        var layer = Full(ui, "Main Menu");
        var panel = Full(layer, "Panel");
        bool hasArt = File.Exists(MainMenuPsd);
        Img(panel, hasArt ? PsdSprite(MainMenuPsd, null) : null, hasArt ? Color.white : new Color(0.1f, 0.09f, 0.11f));
        if (!hasArt)
        {
            Label(panel, "Placeholder Title", "WHILE STOCKS LAST!", 110, new Vector2(-330, 240), new Vector2(1200, 160));
            Label(panel, "Placeholder Team", "Shiiro Games", 36, new Vector2(-330, 130), new Vector2(800, 60), new Color(1f, 1f, 1f, 0.6f), FontStyle.Normal);
        }
        Button MenuButton(string name, float y)
        {
            var b = TextButton(panel, name, name.ToUpperInvariant(), Vector2.zero, new Vector2(420, 96), 44);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-450f, y); // inside the title art's empty right-hand area
            return b;
        }
        var play = MenuButton("Play", 480f);
        var credits = MenuButton("Credits", 365f);
        var quit = MenuButton("Quit", 250f);
        var menu = layer.gameObject.AddComponent<MainMenu>();
        Wire(menu, ("panel", panel.gameObject), ("playButton", play), ("creditsButton", credits), ("quitButton", quit));
        return menu;
    }

    /// Caption box at the bottom centre for things in the house that talk (and the creep warnings).
    static HouseVoice BuildHouseVoice(Transform ui, GameObject game)
    {
        var rt = Rect(ui, "House Voice Caption", Vector2.zero, new Vector2(900, 80));
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(80f, 40f); // a little right of centre so it never overlaps the room label
        Img(rt, null, Dark, false);
        var text = Label(rt, "Label", "...", 30, Vector2.zero, rt.sizeDelta - new Vector2(40, 0), null, FontStyle.Italic);
        var voice = game.AddComponent<HouseVoice>();
        Wire(voice, ("group", rt.gameObject.AddComponent<CanvasGroup>()), ("text", text), ("blip", UiSfx(2)));
        return voice;
    }

    /// The "..." that floats up when you click on nothing.
    static ClickPuff BuildMissPuff(Transform ui)
    {
        var layer = Full(ui, "Miss Puff");
        var dots = Rect(layer, "Dots", Vector2.zero, new Vector2(120, 60));
        var t = dots.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = "…";
        t.fontSize = 56;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        var outline = dots.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
        var puff = layer.gameObject.AddComponent<ClickPuff>();
        Wire(puff, ("canvasRect", (RectTransform)ui), ("puff", dots), ("group", layer.gameObject.AddComponent<CanvasGroup>()));
        return puff;
    }

    const string CursorPng = "Assets/Art/Sprites/UI/SPR_UI_CursorHover.png";

    /// Hover cursor: a white ring with a dark edge and a centre dot. Generated once; replace the PNG with drawn art any time.
    static Texture2D EnsureCursorTexture()
    {
        if (!File.Exists(CursorPng))
        {
            const int S = 32;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var c = new Vector2(S / 2f, S / 2f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    float ring = Mathf.Clamp01(1.6f - Mathf.Abs(d - 10f));      // white ring, radius 10
                    float edge = Mathf.Clamp01(2.6f - Mathf.Abs(d - 10f)) - ring; // dark outline around it
                    float dot = Mathf.Clamp01(2.5f - d);                          // centre dot
                    float white = Mathf.Max(ring, dot);
                    var col = Color.Lerp(new Color(0.12f, 0.12f, 0.12f, Mathf.Max(edge, Mathf.Clamp01(3.5f - d) - dot) * 0.85f), Color.white, white);
                    col.a = Mathf.Max(col.a, white);
                    tex.SetPixel(x, y, col);
                }
            File.WriteAllBytes(CursorPng, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(CursorPng);
        }
        var imp = (TextureImporter)AssetImporter.GetAtPath(CursorPng);
        if (imp.textureType != TextureImporterType.Cursor)
        {
            imp.textureType = TextureImporterType.Cursor;
            imp.isReadable = true;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return Load<Texture2D>(CursorPng);
    }

    /// Bottom-left room name that fades in for 3 s on every room change.
    static void BuildRoomLabel(Transform ui, RoomCamera rooms)
    {
        var rt = Rect(ui, "Room Label", Vector2.zero, new Vector2(420, 80));
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(40f, 40f);
        Img(rt, null, Dark, false);
        var text = Label(rt, "Label", "BEDROOM", 40, Vector2.zero, rt.sizeDelta);
        var label = rt.gameObject.AddComponent<RoomLabel>();
        Wire(label, ("rooms", rooms), ("label", text), ("group", rt.gameObject.AddComponent<CanvasGroup>()));
    }

    static Button Page(Transform ui, string name, string psd, string hint)
    {
        var rt = Full(ui, name);
        var img = Img(rt, PsdSprite(psd, null));
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.None;
        rt.gameObject.AddComponent<CanvasGroup>();
        var label = Label(rt, "Hint", hint, 30, new Vector2(0, 60), new Vector2(900, 50), new Color(1, 1, 1, 0.7f), FontStyle.Normal);
        var lrt = (RectTransform)label.transform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f);
        return b;
    }

    static PauseMenu BuildPause(Transform ui)
    {
        var layer = Full(ui, "Pause");
        var panel = Full(layer, "Pause Panel");
        Img(panel, null, new Color(0, 0, 0, 0.7f));
        Label(panel, "Title", "PAUSED", 72, new Vector2(0, 270), new Vector2(600, 100));
        var resume = TextButton(panel, "Resume", "RESUME", new Vector2(0, 120), new Vector2(420, 96));
        var whatToDo = TextButton(panel, "What To Do", "WHAT TO DO?", new Vector2(0, 0), new Vector2(420, 96));
        var credits = TextButton(panel, "Credits", "CREDITS", new Vector2(0, -120), new Vector2(420, 96));
        var quit = TextButton(panel, "Quit", "QUIT", new Vector2(0, -240), new Vector2(420, 96));

        // Full-screen pages opened from the pause menu; clicking anywhere goes back.
        (GameObject page, Button close) SubPage(string name, string psd)
        {
            var rt = Full(layer, name);
            var img = Img(rt, PsdSprite(psd, null));
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            var hint = Label(rt, "Hint", "click to go back", 30, new Vector2(0, 60), new Vector2(900, 50), new Color(1, 1, 1, 0.7f), FontStyle.Normal);
            ((RectTransform)hint.transform).anchorMin = ((RectTransform)hint.transform).anchorMax = new Vector2(0.5f, 0f);
            return (rt.gameObject, b);
        }
        var creditsPage = SubPage("Pause Credits", "Assets/Art/Source/Screens/PSD_Credits.psd");
        var instructionsPage = SubPage("Pause What To Do", InstructionsPsd);

        var pause = layer.gameObject.AddComponent<PauseMenu>();
        Wire(pause, ("panel", panel.gameObject), ("creditsPanel", creditsPage.page), ("instructionsPanel", instructionsPage.page),
            ("resumeButton", resume), ("instructionsButton", whatToDo), ("creditsButton", credits), ("quitButton", quit),
            ("creditsCloseButton", creditsPage.close), ("instructionsCloseButton", instructionsPage.close));
        return pause;
    }

    static void BuildHelpHint(Transform ui)
    {
        var t = Label(ui, "Pause Hint", "ESC  pause", 22, new Vector2(-90, 30), new Vector2(200, 40), new Color(1, 1, 1, 0.45f), FontStyle.Normal);
        var rt = (RectTransform)t.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.SetSiblingIndex(0);
    }

    // ------------------------------------------------------------------ helpers

    static T Load<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) Debug.LogError("[Cozy] Missing asset: " + path);
        return a;
    }

    /// A sprite from a PSD by layer name (null = the first/only sprite).
    static Sprite PsdSprite(string psd, string layer)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(psd).OfType<Sprite>();
        var s = layer == null ? sprites.FirstOrDefault() : sprites.FirstOrDefault(x => x.name == layer);
        if (s == null) Debug.LogError($"[Cozy] Sprite '{layer}' not found in {psd}");
        return s;
    }

    // ------------------------------------------------------------------ anomaly visibility

    // Thresholds for "would a player actually notice this change?" Sizes are world units (a room is ~20 wide).
    const float MinTilt = 0.8f, MinShift = 0.6f, MinResize = 1.0f, MaxResize = 8f, MinTint = 0.6f;
    const float MinTintChange = 0.15f;      // how much the red tint changes the sprite (black or red objects barely change)

    /// Gives every Prop the procedural anomalies that will be clearly visible on it, and writes ANOMALIES.md.
    static void AssignAnomalyKinds(RoomCamera.Room[] rooms)
    {
        var rows = new List<string>();
        var debug = new List<string>();
        foreach (var p in Object.FindObjectsByType<Prop>(FindObjectsInactive.Include))
        {
            var sr = p.GetComponent<SpriteRenderer>();
            float size = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
            float tintChange = Measure(sr.sprite);
            var kinds = new List<AnomalyKind>();
            if (size >= MinTilt) kinds.Add(AnomalyKind.Tilt);
            if (size >= MinShift) kinds.Add(AnomalyKind.Shift);
            if (size >= MinResize && size <= MaxResize) { kinds.Add(AnomalyKind.Grow); kinds.Add(AnomalyKind.Shrink); }
            if (size >= MinTint && tintChange >= MinTintChange) kinds.Add(AnomalyKind.Tint);
            p.allowedKinds = kinds.ToArray();

            var room = rooms.FirstOrDefault(r => r.area.Contains(sr.bounds.center))?.name ?? "?";
            bool bought = p.GetComponent<ShopItem>() != null;
            bool box = p.name.StartsWith("Delivery Box");
            string drawn = p.anomalySprite != null ? "**yes**" : "";
            string procedural = !p.canBeAnomalous ? "never (interactive)" : kinds.Count == 0 ? "none (too small)" : string.Join(", ", kinds);
            string when = bought ? "after buying" : box ? "after a purchase" : "from the start";
            rows.Add($"| {room} | {p.name} | {when} | {drawn} | {procedural} |");
            debug.Add($"{room}/{p.name}: size {size:0.00} tintChange {tintChange:0.00} -> {string.Join(",", kinds)}");
        }
        rows.Sort();
        var md = new List<string>
        {
            "# Anomaly list (generated by CozyBuilder — do not edit by hand)",
            "",
            "**Drawn** = has hand-drawn anomaly art (shown 90% of the time it's picked).",
            "**Procedural** = the changes that are clearly visible on that object; one is picked at random.",
            "Strength grows each day: Day 2 is mild (tilt 7–10°, resize ±15%, small shift), Day 6+ is strong (tilt 14–20°, resize ±30%).",
            "Tint (red) only unlocks from Day 4, even where listed.",
            "",
            "| Room | Object | Can appear | Drawn | Procedural options |",
            "|---|---|---|---|---|",
        };
        md.AddRange(rows);
        File.WriteAllLines("ANOMALIES.md", md);
        var outDir = System.Environment.GetEnvironmentVariable("COZY_OUT");
        if (outDir != null) File.WriteAllLines(Path.Combine(outDir, "anomaly_metrics.txt"), debug);
        readable.Clear();
    }

    static readonly Dictionary<Texture, Texture2D> readable = new Dictionary<Texture, Texture2D>();

    /// PSD textures aren't CPU-readable, so copy them through a RenderTexture once.
    static Texture2D Readable(Texture tex)
    {
        if (readable.TryGetValue(tex, out var r)) return r;
        var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(tex, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        r = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        r.ReadPixels(new UnityEngine.Rect(0, 0, tex.width, tex.height), 0, 0);
        r.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        readable[tex] = r;
        return r;
    }

    /// How much the red anomaly tint (x1, x0.45, x0.45) changes the visible pixels' brightness.
    static float Measure(Sprite s)
    {
        var tex = Readable(s.texture);
        var rect = s.textureRect;
        int x0 = (int)rect.x, y0 = (int)rect.y, w = (int)rect.width, h = (int)rect.height;
        int step = Mathf.Max(1, Mathf.Max(w, h) / 64);
        float lumSum = 0f, alphaSum = 0f;
        for (int y = 0; y < h; y += step)
            for (int x = 0; x < w; x += step)
            {
                Color c = tex.GetPixel(x0 + x, y0 + y);
                lumSum += (0.59f * 0.55f * c.g + 0.11f * 0.55f * c.b) * c.a; // brightness lost to the tint
                alphaSum += c.a;
            }
        return alphaSum > 0f ? lumSum / alphaSum : 0f;
    }

    static AudioClip ItemSfx(string name) => Load<AudioClip>($"Assets/Audio/SFX/Items/SFX_{name}.wav");
    static AudioClip UiSfx(int n) => Load<AudioClip>($"Assets/Audio/SFX/UI/SFX_Notification_0{n}.wav");

    /// Sets [SerializeField] private fields by name.
    static void Wire(Object target, params (string field, Object value)[] pairs)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in pairs)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Cozy] {target.GetType().Name}.{field} not found"); continue; }
            p.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    const string VignettePng = "Assets/Art/Sprites/UI/SPR_UI_Vignette.png";
    const string GlowPng = "Assets/Art/Sprites/UI/SPR_UI_Glow.png";

    /// Radial alpha gradient, generated once. edges=true: clear centre, solid edges (vignette). false: solid centre, clear edges (glow).
    static Sprite EnsureRadialSprite(string path, bool edges)
    {
        if (!File.Exists(path))
        {
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / S * 2f - 1f, dy = (y + 0.5f) / S * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                    float a = edges ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, d)) : 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d * 1.4142f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
        return Load<Sprite>(path);
    }

    /// Generates the round dark arrow button sprite once.
    static void EnsureArrowSprite()
    {
        if (File.Exists(ArrowPng)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(ArrowPng));
        const int S = 128;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        Vector2 a = new Vector2(52, 30), b = new Vector2(82, 64), c = new Vector2(52, 98);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float circle = Mathf.Clamp01(61f - Vector2.Distance(p, new Vector2(64, 64)));
                float chevron = Mathf.Clamp01(9f - Mathf.Min(SegDist(p, a, b), SegDist(p, b, c)));
                var col = Color.Lerp(new Color(0.18f, 0.18f, 0.18f, 0.9f * circle), Color.white, chevron);
                col.a = Mathf.Max(col.a, chevron);
                tex.SetPixel(x, y, col);
            }
        File.WriteAllBytes(ArrowPng, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(ArrowPng);
        var imp = (TextureImporter)AssetImporter.GetAtPath(ArrowPng);
        imp.textureType = TextureImporterType.Sprite;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
    }

    static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }
}
