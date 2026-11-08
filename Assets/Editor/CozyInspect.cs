using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Headless helpers used to inspect the project from the command line:
/// Unity.exe -batchmode -projectPath . -executeMethod CozyInspect.Dump -quit
public static class CozyInspect
{
    static string OutDir => System.Environment.GetEnvironmentVariable("COZY_OUT") ?? "Temp/CozyInspect";

    public static void Dump()
    {
        Directory.CreateDirectory(OutDir);
        var sb = new StringBuilder();

        sb.AppendLine("## PSD sprites");
        foreach (var guid in AssetDatabase.FindAssets("", new[] { "Assets/Art/Source" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".psd")) continue;
            sb.AppendLine("# " + path);
            foreach (var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                sb.AppendLine($"  {s.name}  rect={s.rect.width}x{s.rect.height} ppu={s.pixelsPerUnit}");
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        sb.AppendLine("## Scene " + scene.path);
        foreach (var root in scene.GetRootGameObjects()) DumpGO(root.transform, 0, sb);
        File.WriteAllText(Path.Combine(OutDir, "dump.txt"), sb.ToString());

        RenderAll(Path.Combine(OutDir, "scene_overview.png"));
        var main = Camera.main;
        if (main != null) RenderCamera(main, Path.Combine(OutDir, "scene_maincam.png"), 1920, 1080);
    }

    /// Renders each PSD on its own (fresh scene) to OutDir/psd_<name>.png.
    public static void ExportPsds()
    {
        Directory.CreateDirectory(OutDir);
        foreach (var guid in AssetDatabase.FindAssets("", new[] { "Assets/Art/Source" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".psd")) continue;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) continue; // layered PSDs already exported
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null) continue;
            new GameObject("flat").AddComponent<SpriteRenderer>().sprite = sprite;
            RenderAll(Path.Combine(OutDir, "psd_" + Path.GetFileNameWithoutExtension(path) + ".png"));
        }
    }

    /// Contact sheet: each layer of a PSD rendered alone into its own grid cell (row-major, in child order).
    public static void ExportLayerSheets()
    {
        foreach (var path in (System.Environment.GetEnvironmentVariable("COZY_PSDS") ?? "Assets/Art/Source/Rooms/PSD_Bathroom.psd;Assets/Art/Source/Rooms/PSD_Balcony.psd").Split(';'))
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var layers = root.GetComponentsInChildren<SpriteRenderer>();
            var b = layers[0].bounds;
            foreach (var l in layers) b.Encapsulate(l.bounds);
            int cols = 4, rows = Mathf.CeilToInt(layers.Length / (float)cols);
            int cw = 480, ch = 270;
            var rt = new RenderTexture(cols * cw, rows * ch, 24);
            var cam = new GameObject("cam").AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = b.extents.y * 1.02f;
            cam.transform.position = new Vector3(b.center.x, b.center.y, -50);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.magenta;
            cam.targetTexture = rt;
            var log = new StringBuilder();
            for (int i = 0; i < layers.Length; i++)
            {
                foreach (var l in layers) l.enabled = false;
                layers[i].enabled = true;
                int c = i % cols, r = rows - 1 - i / cols;
                cam.rect = new Rect(c / (float)cols, r / (float)rows, 1f / cols, 1f / rows);
                cam.Render();
                log.AppendLine($"{i}: {layers[i].name} order={layers[i].sortingOrder}");
            }
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            var name = Path.GetFileNameWithoutExtension(path);
            File.WriteAllBytes(Path.Combine(OutDir, "layers_" + name + ".png"), tex.EncodeToPNG());
            File.WriteAllText(Path.Combine(OutDir, "layers_" + name + ".txt"), log.ToString());
            RenderTexture.active = null;
        }
    }

    /// Renders each room of Game.unity (all shop items + boxes shown) and the main UI overlays to PNGs.
    public static void Snapshots()
    {
        Directory.CreateDirectory(OutDir);
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        foreach (var item in Object.FindObjectsByType<ShopItem>(FindObjectsInactive.Include)) item.gameObject.SetActive(true);
        var shop = Object.FindAnyObjectByType<Shop>();
        foreach (var b in shop.deliveryBoxes) b.SetActive(true);

        var rc = Object.FindAnyObjectByType<RoomCamera>();
        var cam = rc.GetComponent<Camera>();
        var canvas = Object.FindAnyObjectByType<Canvas>();
        foreach (Transform child in canvas.transform) child.gameObject.SetActive(false);

        for (int i = 0; i < rc.rooms.Length; i++)
        {
            var a = rc.rooms[i].area;
            cam.orthographicSize = a.height / 2f;
            cam.transform.position = new Vector3(a.center.x, a.center.y, -10);
            int w = 1600, h = Mathf.RoundToInt(w * a.height / a.width);
            cam.rect = new Rect(0, 0, 1, 1);
            RenderCamera(cam, Path.Combine(OutDir, $"room_{i}_{rc.rooms[i].name}.png"), w, h);
        }

        // UI overlays rendered over the bedroom via a camera-space canvas.
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        var a0 = rc.rooms[0].area;
        cam.orthographicSize = a0.height / 2f;
        cam.transform.position = new Vector3(a0.center.x, a0.center.y, -10);
        foreach (var name in new[] { "Shop Window", "Bed Dialog", "Flyer", "Pause", "Title Screen" })
        {
            foreach (Transform child in canvas.transform) child.gameObject.SetActive(false);
            var t = canvas.transform.Find(name);
            t.gameObject.SetActive(true);
            foreach (Transform c in t) c.gameObject.SetActive(true);
            if (name == "Pause") t.Find("Pause Credits").gameObject.SetActive(false);
            canvas.transform.Find("Arrow Left").gameObject.SetActive(true);
            canvas.transform.Find("Arrow Right").gameObject.SetActive(true);
            if (name == "Shop Window")
            {
                var cards = shop.cards;
                for (int i = 0; i < cards.Length; i++) cards[i].Bind(shop.items[i]);
                canvas.transform.Find("Toast").gameObject.SetActive(true);
            }
            var rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;
            canvas.GetComponent<UnityEngine.UI.CanvasScaler>().enabled = false;
            canvas.scaleFactor = 1f;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            Canvas.ForceUpdateCanvases();
            RenderCamera(cam, Path.Combine(OutDir, $"ui_{name.Replace(' ', '_')}.png"), 1920, 1080);
            cam.targetTexture = null;
        }
    }

    static void DumpGO(Transform t, int depth, StringBuilder sb)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        string extra = "";
        if (sr != null)
            extra = $" sprite={(sr.sprite ? sr.sprite.name : "null")} order={sr.sortingOrder} layer={sr.sortingLayerName} bounds=({sr.bounds.min.x:0.00},{sr.bounds.min.y:0.00})-({sr.bounds.max.x:0.00},{sr.bounds.max.y:0.00})";
        var comps = string.Join(",", t.GetComponents<Component>().Where(c => c != null && !(c is Transform) && !(c is SpriteRenderer)).Select(c => c.GetType().Name));
        sb.AppendLine($"{new string(' ', depth * 2)}{t.name} [{(t.gameObject.activeSelf ? "on" : "OFF")}] pos=({t.position.x:0.00},{t.position.y:0.00},{t.position.z:0.00}) scale={t.lossyScale.x:0.00}{extra} {comps}");
        foreach (Transform c in t) DumpGO(c, depth + 1, sb);
    }

    /// Renders every visible sprite in the open scene with a temporary camera that frames them all.
    public static void RenderAll(string file)
    {
        var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
        if (renderers.Length == 0) return;
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        var go = new GameObject("TempCam");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(b.extents.y, b.extents.x * 9f / 16f) * 1.02f;
        cam.transform.position = new Vector3(b.center.x, b.center.y, -50);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.magenta;
        RenderCamera(cam, file, 2400, 1350);
        Object.DestroyImmediate(go);
    }

    public static void RenderCamera(Camera cam, string file, int w, int h)
    {
        var rt = new RenderTexture(w, h, 24);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.Render(); // second pass: the first headless render can bind the wrong sprite textures
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        File.WriteAllBytes(file, tex.EncodeToPNG());
        cam.targetTexture = prev;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
