using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// The game's state machine (Unreal: the GameMode).
/// Title → Day N (explore, fix anomalies, shop) → bed → win/lose check → transition → Day N+1 ...
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Rules (see DESIGN.md)")]
    public int startingPoints = 2;
    [Tooltip("Points earned from anomalies before the no-purchase streak starts counting.")]
    public int pointsToArmWinStreak = 8;
    public int winStreakDays = 3;
    [Tooltip("A no-purchase day only counts toward the win streak if at least this many anomalies were fixed that day.")]
    public int minFixesPerStreakDay = 2;
    public int bedroomIndex = 0;
    [Tooltip("Seconds between computer pings when you have points and haven't opened the shop today (Day 2+).")]
    public Vector2 shopReminderGap = new Vector2(25f, 40f);

    [Header("Systems")]
    public Screens screens;
    public RoomCamera rooms;
    public Shop shop;
    public AnomalyDirector director;
    public FlyerPopup flyer;
    public MusicPlayer music;
    public ConfirmDialog bedDialog;
    public PauseMenu pause;
    public Atmosphere atmosphere;
    public MainMenu mainMenu;

    [Header("Interactive props")]
    public Prop[] bedProps;       // clicking any of these asks "Go to bed?"
    public Prop[] computerProps;  // clicking any of these opens the shop

    [Header("Feedback")]
    [Tooltip("Played whenever any anomaly is fixed.")]
    public AudioClip anomalyFixedSound;

    enum Phase { Title, Playing, Busy, Ended }
    Phase phase = Phase.Title;

    /// True when clicks should reach the room (no menus, no transitions).
    public bool WorldInputEnabled =>
        phase == Phase.Playing && !shop.IsOpen && !bedDialog.IsOpen && !pause.IsPaused && !rooms.Moving; // the flyer is a pop-up ad, not a menu: the room stays playable

    public bool CanPause => phase == Phase.Playing;

    /// Time of day advances during play and menus (shopping costs daylight), but not while paused or between rooms.
    public bool ClockRunning => phase == Phase.Playing && !pause.IsPaused && !rooms.Moving;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Prop.FixedSound = anomalyFixedSound;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        foreach (var p in bedProps) if (p != null) p.Clicked += _ => AskBed();
        foreach (var p in computerProps) if (p != null) p.Clicked += _ => OpenShop();

        PlayerState.Reset(startingPoints);
        shop.ResetRoom();
        director.ResetAll();
        rooms.JumpTo(bedroomIndex);

        // Start screen → Play → "Welcome home" card → Day 1.  Credits returns to the start screen.
        mainMenu.PlayPressed += () =>
        {
            if (phase != Phase.Title) return;
            mainMenu.Hide();
            screens.ShowPage(screens.titleScreen, () => StartCoroutine(BeginRun()), fadeIn: false); // swaps straight in for the menu
        };
        mainMenu.CreditsPressed += () => screens.ShowPage(screens.creditsScreen, () => screens.HidePage(screens.creditsScreen));
        mainMenu.Show();
        StartCoroutine(screens.FadeFromBlack(0.8f));
    }

    IEnumerator BeginRun()
    {
        phase = Phase.Busy;
        yield return screens.FadeToBlack(0.6f);
        screens.HidePage(screens.titleScreen);
        yield return StartDay();
    }

    IEnumerator StartDay()
    {
        PlayerState.StartNextDay();
        int day = PlayerState.Day;
        rooms.JumpTo(bedroomIndex);
        shop.Restock();
        director.OnDayStart(day);
        music.PlayDay(day);
        atmosphere.StartDay();
        yield return screens.FadeFromBlack(0.9f);
        phase = Phase.Playing;
        flyer.OnDayStart(day);
        StartCoroutine(screens.ShowDayBanner(day));
        StartCoroutine(day == 1 ? NudgeComputer() : ShopReminders(day));
    }

    /// Day 1 tutorial hint: the computer pings and wiggles until the player opens the shop.
    IEnumerator NudgeComputer()
    {
        yield return new WaitForSeconds(4f);
        while (!shop.OpenedToday && PlayerState.Day == 1 && phase == Phase.Playing)
        {
            foreach (var p in computerProps) if (p != null && p.gameObject.activeInHierarchy) p.Wiggle(0.6f);
            yield return new WaitForSeconds(3.5f);
        }
    }

    /// From Day 2: if you have points to spend and haven't looked at the shop today, the computer pings now and then.
    IEnumerator ShopReminders(int day)
    {
        while (PlayerState.Day == day && !shop.OpenedToday)
        {
            yield return new WaitForSeconds(Random.Range(shopReminderGap.x, shopReminderGap.y));
            if (phase != Phase.Playing || PlayerState.Day != day || shop.OpenedToday) yield break;
            if (!WorldInputEnabled || PlayerState.Points < 1 || !shop.HasStock) continue;
            foreach (var p in computerProps)
                if (p != null && p.gameObject.activeInHierarchy) { p.Wiggle(0.6f); Sfx.Play(p.clickSound, 0.5f); }
        }
    }

    void OpenShop() => shop.Open();

    void AskBed()
    {
        if (phase != Phase.Playing) return;
        bedDialog.Show(() => StartCoroutine(Sleep()));
    }

    /// Creep consequence: too many unfixed anomalies for too long and the day ends without asking.
    public void ForceSleep()
    {
        if (phase != Phase.Playing) return;
        shop.Close();
        bedDialog.Hide();
        flyer.Cancel();
        StartCoroutine(Sleep());
    }

    IEnumerator Sleep()
    {
        phase = Phase.Busy;
        flyer.Cancel();
        director.StopMidDay();
        atmosphere.Stop();
        music.FadeOut(1.2f);
        PlayerState.EndDay(pointsToArmWinStreak, minFixesPerStreakDay);

        // Hidden endings, checked at bedtime. Owning everything wins over the streak.
        if (shop.AllOwned) { yield return End(false); yield break; }
        if (PlayerState.NoPurchaseStreak >= winStreakDays) { yield return End(true); yield break; }

        yield return screens.PlayTransition();
        yield return StartDay();
    }

    IEnumerator End(bool won)
    {
        phase = Phase.Ended;
        yield return screens.FadeToBlack(1.2f);
        screens.ShowPage(won ? screens.winScreen : screens.loseScreen, ShowCreditsThenRestart);
        yield return screens.FadeFromBlack(0.6f);
    }

    void ShowCreditsThenRestart()
    {
        screens.HidePage(screens.winScreen);
        screens.HidePage(screens.loseScreen);
        screens.ShowPage(screens.creditsScreen, () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
    }
}
