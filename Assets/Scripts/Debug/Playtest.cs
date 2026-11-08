
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// Automated play-through used for headless verification. Does nothing unless the COZY_PLAYTEST env var is set.
/// Launched by CozyPlaytest.Run (Editor). Writes screenshots + results.txt to COZY_OUT, then exits the editor.
public class Playtest : MonoBehaviour
{
    string outDir;
    readonly List<string> log = new List<string>();
    int failures;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!Debug.isDebugBuild || string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("COZY_PLAYTEST"))) return;
        new GameObject("[Playtest]").AddComponent<Playtest>();
    }

    void Start()
    {
        outDir = System.Environment.GetEnvironmentVariable("COZY_OUT") ?? "Temp/Playtest";
        Application.runInBackground = true; // the test must keep running even if its window loses focus
        Directory.CreateDirectory(outDir);
        StartCoroutine(Watchdog());
        StartCoroutine(Run());
    }

    IEnumerator Watchdog()
    {
        yield return new WaitForSecondsRealtime(240f);
        Check(false, "watchdog: playtest took too long");
        Finish();
    }

    void Check(bool ok, string what)
    {
        log.Add((ok ? "PASS " : "FAIL ") + what);
        if (!ok) failures++;
    }

    IEnumerator WaitFor(System.Func<bool> cond, float timeout)
    {
        float t = 0f;
        while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
    }

    IEnumerator Run()
    {
        var gm = GameManager.Instance;
        var shop = gm.shop;
        yield return new WaitForSecondsRealtime(1.2f);
        Check(gm.mainMenu.IsOpen, "Start screen shows first");
        yield return Shot("00_main_menu");
        gm.mainMenu.GetComponentsInChildren<Button>(true).First(b => b.name == "Play").onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.8f);
        Check(!gm.mainMenu.IsOpen && gm.screens.titleScreen.gameObject.activeSelf, "Play leads to the Welcome card");
        yield return Shot("01_title");

        gm.screens.titleScreen.onClick.Invoke();
        yield return WaitFor(() => gm.WorldInputEnabled, 6f);
        Check(PlayerState.Day == 1, "Day 1 starts after clicking the title");
        Check(gm.director.ActiveCount == 0, "No anomalies on Day 1");
        Check(shop.items.All(i => !i.gameObject.activeSelf), "All shop items hidden at start");
        var curtain = Object.FindObjectsByType<Prop>(FindObjectsInactive.Include).First(p => p.name == "Shower Curtain");
        curtain.Click();
        Check(!curtain.gameObject.activeSelf && curtain.toggleTo.gameObject.activeSelf, "Clicking the shower curtain opens it");
        curtain.toggleTo.Click();
        Check(curtain.gameObject.activeSelf && !curtain.toggleTo.gameObject.activeSelf, "Clicking it again closes it");
        yield return new WaitForSecondsRealtime(0.8f);
        yield return Shot("02_day1_bedroom");

        // The guaranteed Day 1 flyer should arrive on its own within ~15 s of free play
        float waited = 0f;
        while (!gm.flyer.IsOpen && waited < 20f) { waited += Time.unscaledDeltaTime; yield return null; }
        Check(gm.flyer.IsOpen && !gm.flyer.IsAnomalous, $"Day 1 flyer appears by itself (after {waited:0.0}s, normal version)");
        gm.flyer.Hide();

        // Day cycle: fast-forward the day and measure both the light and the actual screen brightness
        yield return MeasureScreen();
        float lumBefore = lastLum, lightBefore = gm.atmosphere.globalLight.intensity;
        gm.atmosphere.dayLength = 4f;
        yield return new WaitForSecondsRealtime(8f);
        yield return MeasureScreen();
        log.Add($"  dusk: progress {gm.atmosphere.DayProgress:0.00} light {lightBefore:0.00}->{gm.atmosphere.globalLight.intensity:0.00} screen {lumBefore:0.000}->{lastLum:0.000} playing {gm.WorldInputEnabled}");
        Check(gm.atmosphere.DayProgress >= 0.99f && gm.atmosphere.globalLight.intensity < 0.6f, "Day clock runs and dims the global light");
        Check(lastLum < lumBefore * 0.8f, "Dusk is visibly darker on screen");
        yield return Shot("02b_dusk");
        gm.atmosphere.dayLength = 150f;

        // Shop
        gm.computerProps[0].Click();
        yield return new WaitForSecondsRealtime(0.4f);
        Check(shop.IsOpen, "Clicking the monitor opens the shop");
        Check(!gm.WorldInputEnabled, "World input blocked while shop is open");
        yield return Shot("03_shop");
        shop.cards[2].buyButton.onClick.Invoke(); // 4 pt item with 2 points
        Check(!shop.items[2].Owned, "Can't buy a 4-point item with 2 points");
        yield return new WaitForSecondsRealtime(0.15f);
        yield return Shot("04_cant_afford");
        shop.cards[0].buyButton.onClick.Invoke(); // flower vase, 1 pt
        Check(shop.items[0].Owned && PlayerState.Points == 1, "Bought the 1-point flower vase");
        shop.nextButton.onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.3f);
        Check(shop.cards[0].Item == shop.items[4], "Next arrow turns to page 2");
        yield return Shot("05_shop_page2");
        shop.closeButton.onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.8f);
        Check(!shop.IsOpen && shop.items[0].gameObject.activeSelf, "Closing the shop reveals the bought item");
        Check(shop.deliveryBoxes[0].activeSelf, "A delivery box appears in the hallway");
        yield return Shot("06_vase_revealed");

        // Flyer
        gm.flyer.Show();
        yield return new WaitForSecondsRealtime(0.7f);
        yield return Shot("07_flyer");
        gm.flyer.Hide();

        // Pause
        gm.pause.Pause();
        yield return null;
        Check(Time.timeScale == 0f && !gm.WorldInputEnabled, "Pause freezes time and blocks input");
        yield return Shot("08_pause");
        var pauseButtons = gm.pause.GetComponentsInChildren<Button>(true);
        pauseButtons.First(b => b.name == "What To Do").onClick.Invoke();
        yield return null;
        var whatPage = gm.pause.transform.Find("Pause What To Do").gameObject;
        Check(whatPage.activeSelf && gm.pause.IsPaused, "Pause menu 'What to do?' opens the instructions page");
        yield return Shot("08b_what_to_do");
        whatPage.GetComponent<Button>().onClick.Invoke();
        yield return null;
        Check(!whatPage.activeSelf && gm.pause.transform.Find("Pause Panel").gameObject.activeSelf, "Clicking the instructions page returns to the pause menu");
        gm.pause.Resume();

        // Rooms
        bool labelOk = true;
        if (System.Environment.GetEnvironmentVariable("COZY_PROBE") != null) { yield return ProbeTransition(gm); Finish(); yield break; }
        var visited = new List<int> { gm.rooms.Current };
        for (int i = 0; i < gm.rooms.rooms.Length; i++)
        {
            gm.rooms.Step(+1);
            yield return WaitFor(() => !gm.rooms.Moving, 3f);
            visited.Add(gm.rooms.Current);
            yield return new WaitForSecondsRealtime(0.1f);
            yield return Shot($"09_room_{gm.rooms.Current}");
            var roomLabel = FindAnyObjectByType<RoomLabel>();
            if (roomLabel.GetComponentInChildren<Text>().text != gm.rooms.rooms[gm.rooms.Current].name.ToUpperInvariant() || roomLabel.GetComponent<CanvasGroup>().alpha < 0.2f) labelOk = false;
        }
        Check(gm.rooms.Current == 0, $"Arrows cycle through all rooms back to the bedroom ({string.Join(">", visited)})");
        Check(labelOk, "Room label shows the right name after every move");

        // Bed → Day 2
        gm.bedProps.First(p => !p.canBeAnomalous).Click();
        yield return new WaitForSecondsRealtime(0.4f);
        Check(gm.bedDialog.IsOpen, "Clicking the bed asks 'Go to bed?'");
        yield return Shot("10_bed_dialog");
        ClickYes(gm);
        yield return WaitFor(() => PlayerState.Day == 2 && gm.WorldInputEnabled, 20f);
        Check(PlayerState.Day == 2, "Sleeping starts Day 2");
        Check(gm.atmosphere.globalLight.intensity > 0.95f, "Brightness resets the next morning");
        int active = gm.director.ActiveCount;
        Check(active >= 2, $"Day 2 spawns anomalies ({active})");
        yield return Shot("11_day2_bedroom");

        // Fix every anomaly
        int before = PlayerState.PointsEarned;
        foreach (var p in FindObjectsByType<Prop>(FindObjectsInactive.Exclude).Where(p => p.IsAnomalous).ToList()) p.Click();
        Check(PlayerState.PointsEarned == before + active && gm.director.ActiveCount == 0, "Clicking anomalies fixes them and earns hidden points");

        // Misclick is harmless
        int pts = PlayerState.Points;
        FindObjectsByType<Prop>(FindObjectsInactive.Exclude).First(p => !p.IsAnomalous && !gm.bedProps.Contains(p) && !gm.computerProps.Contains(p)).Click();
        Check(PlayerState.Points == pts, "Clicking a normal object costs nothing");

        // Next-day restock: the vase bought yesterday is gone from the shop
        gm.shop.Open();
        yield return new WaitForSecondsRealtime(0.3f);
        Check(gm.shop.cards[0].Item != null && gm.shop.cards[0].Item != gm.shop.items[0] && !gm.shop.cards[0].Item.Owned, "Shop restocks with unowned items the next day");
        yield return Shot("11b_shop_day2");
        gm.shop.Close();

        // Anomaly flyer
        gm.flyer.anomalyChance = 1f;
        gm.flyer.Show();
        yield return new WaitForSecondsRealtime(0.7f);
        Check(gm.flyer.IsAnomalous && gm.flyer.anomalyLayers[0].activeSelf && !gm.flyer.normalLayers[0].activeSelf, "Flyer can show its anomaly version");
        yield return Shot("11c_flyer_anomaly");
        int fp = PlayerState.PointsEarned;
        gm.flyer.GetComponentInChildren<Button>().onClick.Invoke();
        Check(PlayerState.PointsEarned == fp + 1 && !gm.shop.IsOpen && !gm.flyer.IsOpen, "Clicking the anomaly flyer earns a point and doesn't open the shop");
        gm.flyer.anomalyChance = 0.3f;

        // House voice: clicking the TV makes it say something
        var voice = gm.GetComponent<HouseVoice>();
        var tv = voice.speakers.First(s => s.label == "TV").prop;
        if (tv.IsAnomalous) tv.Click();
        tv.Click();
        yield return new WaitForSecondsRealtime(0.5f);
        Check(voice.IsShowing, "Clicking the TV shows a caption line");
        yield return Shot("11d_voice");

        // Creep consequences: pile up anomalies, expect a vanished object, then a forced bedtime
        var atm = gm.atmosphere;
        atm.forceSleepAfter = 999f; // first: vanish
        for (int k = 0; k < 30 && gm.director.ActiveCount < atm.forceSleepAt + 1; k++) gm.director.SpawnOne(false);
        int dayBeforeForce = PlayerState.Day;
        yield return WaitFor(() => gm.director.VanishedCount > 0, 12f);
        Check(gm.director.VanishedCount > 0, $"Heavy creep makes an object vanish ({gm.director.ActiveCount} unfixed)");
        yield return Shot("11e_vanish");
        atm.forceSleepAfter = 3f; // then: forced bedtime
        yield return WaitFor(() => PlayerState.Day > dayBeforeForce, 15f);
        Check(PlayerState.Day > dayBeforeForce, "Overwhelming creep forces you to bed");
        atm.forceSleepAfter = 20f;
        yield return WaitFor(() => gm.CanPause, 15f);

        // Win: arm the streak, then three nights without buying
        PlayerState.EarnPoint(5);
        for (int night = 0; night < 3; night++)
        {
            int day = PlayerState.Day;
            gm.bedProps.First(p => !p.canBeAnomalous).Click();
            yield return new WaitForSecondsRealtime(0.3f);
            ClickYes(gm);
            yield return WaitFor(() => (PlayerState.Day > day && gm.CanPause) || gm.screens.winScreen.gameObject.activeSelf, 20f);
            log.Add($"  night {night}: day {PlayerState.Day} streak {PlayerState.NoPurchaseStreak} bought {PlayerState.PurchasesToday} earned {PlayerState.PointsEarned} playing {gm.CanPause}");
            if (night == 0) Check(gm.director.ActiveCount >= 5, $"Day 3 has at least 5 anomalies ({gm.director.ActiveCount})");
        }
        yield return new WaitForSecondsRealtime(2.5f);
        Check(gm.screens.winScreen.gameObject.activeSelf, $"3 armed days without buying wins (streak {PlayerState.NoPurchaseStreak})");
        yield return Shot("12_win");

        // Lose rule (logic only): owning everything ends the game at bedtime
        foreach (var it in shop.items) PlayerState.TryBuy(it.id, 0);
        Check(shop.AllOwned, "Owning all items is detected for the lose ending");

        Finish();
    }

    void ClickYes(GameManager gm)
    {
        var yes = gm.bedDialog.GetComponentsInChildren<Button>(true).OrderBy(b => b.transform.position.x).First();
        yes.onClick.Invoke();
    }

    float lastLum;

    /// Diagnostic: jump to the hallway, step into the bathroom (a blackout move) and log every frame.
    IEnumerator ProbeTransition(GameManager gm)
    {
        gm.rooms.JumpTo(3);
        yield return new WaitForSecondsRealtime(0.5f);
        var cam = Camera.main;
        var fadeGroup = gm.screens.fade;
        gm.rooms.Step(+1);
        int i = 0;
        float t = 0f;
        while (t < 1.6f)
        {
            yield return new WaitForEndOfFrame();
            t += Time.unscaledDeltaTime;
            log.Add($"  f{Time.frameCount} t={t:0.000} alpha={fadeGroup.alpha:0.00} camX={cam.transform.position.x:0.0} rect={cam.rect.width:0.00}x{cam.rect.height:0.00} moving={gm.rooms.Moving}");
            if (!Application.isBatchMode && i++ % 3 == 0)
            {
                var shot = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(outDir, $"probe_{t:0.000}_a{fadeGroup.alpha:0.00}.jpg"), shot.EncodeToJPG(40));
                Destroy(shot);
            }
        }
    }

    /// Average brightness of what's on screen (windowed runs only; batchmode can't capture the screen).
    IEnumerator MeasureScreen()
    {
        yield return new WaitForEndOfFrame();
        lastLum = -1f;
        if (Application.isBatchMode) yield break;
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        float sum = 0f; int n = 0;
        for (int y = 0; y < tex.height; y += 8)
            for (int x = 0; x < tex.width; x += 8) { var c = tex.GetPixel(x, y); sum += c.r * 0.3f + c.g * 0.59f + c.b * 0.11f; n++; }
        lastLum = sum / n;
        Destroy(tex);
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        if (!Application.isBatchMode)
        {
            // Windowed run: grab the real back buffer, UI included.
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(outDir, "play_" + name + ".png"), shot.EncodeToPNG());
            Destroy(shot);
            yield break;
        }
        var cam = Camera.main;
        var canvas = FindAnyObjectByType<Canvas>();
        var rt = new RenderTexture(1920, 1080, 24);
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.black);
        var oldRect = cam.rect;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.Render();
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        File.WriteAllBytes(Path.Combine(outDir, "play_" + name + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null;
        cam.rect = oldRect;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        RenderTexture.active = null;
        Destroy(rt);
        Destroy(tex);
    }

    void Finish()
    {
        log.Add(failures == 0 ? "ALL PASSED" : $"{failures} FAILED");
        File.WriteAllLines(Path.Combine(outDir, "results.txt"), log);
        Debug.Log("[Cozy] Playtest done: " + (failures == 0 ? "ALL PASSED" : failures + " FAILED"));
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(failures == 0 ? 0 : 2);
#else
        Application.Quit(failures == 0 ? 0 : 2);
#endif
    }
}

