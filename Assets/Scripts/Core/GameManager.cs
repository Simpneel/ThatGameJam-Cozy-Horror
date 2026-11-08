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
    public int pointsToArmWinStreak = 5;
    public int winStreakDays = 3;
    public int bedroomIndex = 0;

    [Header("Systems")]
    public Screens screens;
    public RoomCamera rooms;
    public Shop shop;
    public AnomalyDirector director;
    public FlyerPopup flyer;
    public MusicPlayer music;
    public ConfirmDialog bedDialog;
    public PauseMenu pause;

    [Header("Interactive props")]
    public Prop[] bedProps;       // clicking any of these asks "Go to bed?"
    public Prop[] computerProps;  // clicking any of these opens the shop

    enum Phase { Title, Playing, Busy, Ended }
    Phase phase = Phase.Title;
    bool shopVisitedToday;

    /// True when clicks should reach the room (no menus, no transitions).
    public bool WorldInputEnabled =>
        phase == Phase.Playing && !shop.IsOpen && !bedDialog.IsOpen && !flyer.IsOpen && !pause.IsPaused && !rooms.Moving;

    public bool CanPause => phase == Phase.Playing;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
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

        screens.ShowPage(screens.titleScreen, () => StartCoroutine(BeginRun()));
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
        shopVisitedToday = false;
        rooms.JumpTo(bedroomIndex);
        director.OnDayStart(day);
        music.PlayDay(day);
        yield return screens.FadeFromBlack(0.9f);
        phase = Phase.Playing;
        flyer.OnDayStart(day);
        StartCoroutine(screens.ShowDayBanner(day));
        if (day == 1) StartCoroutine(NudgeComputer());
    }

    /// Day 1 tutorial hint: the computer pings and wiggles until the player opens the shop.
    IEnumerator NudgeComputer()
    {
        yield return new WaitForSeconds(4f);
        while (!shopVisitedToday && PlayerState.Day == 1 && phase == Phase.Playing)
        {
            foreach (var p in computerProps) if (p != null && p.gameObject.activeInHierarchy) p.Wiggle(0.6f);
            yield return new WaitForSeconds(3.5f);
        }
    }

    void OpenShop()
    {
        shopVisitedToday = true;
        shop.Open();
    }

    void AskBed()
    {
        if (phase != Phase.Playing) return;
        bedDialog.Show(() => StartCoroutine(Sleep()));
    }

    IEnumerator Sleep()
    {
        phase = Phase.Busy;
        flyer.Cancel();
        director.StopMidDay();
        music.FadeOut(1.2f);
        PlayerState.EndDay(pointsToArmWinStreak);

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
