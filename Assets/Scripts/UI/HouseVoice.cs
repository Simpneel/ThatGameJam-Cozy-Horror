using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Things in the house talk: ads early on, pushier mid-run, then lines hinting that buying is the problem.
/// Shown as a caption at the bottom of the screen. All text is editable here in the Inspector (first draft copy).
public class HouseVoice : MonoBehaviour
{
    [Serializable]
    public class Speaker
    {
        public Prop prop;
        public string label;
        [Tooltip("Say something when clicked (off for the laptop, which opens the shop).")]
        public bool talksOnClick = true;
    }

    public Speaker[] speakers;
    [Tooltip("Seconds between unprompted lines from whichever speakers exist (bought or always there).")]
    public Vector2 ambientGap = new Vector2(25f, 45f);
    public float captionSeconds = 4f;

    [Header("Lines (Days 1-2 / 3-4 / 5+)")]
    [TextArea] public string[] earlyLines =
    {
        "New day, new deals! Your cart misses you.",
        "Treat yourself. You've earned it.",
        "Customers like you also bought: everything.",
        "Fast delivery, straight to your door. Every day.",
        "Don't miss out, while stocks last!",
        "A happier home is just one click away.",
    };
    [TextArea] public string[] midLines =
    {
        "Still thinking about it? Your cart is waiting.",
        "Limited time only. Limited time. Limited.",
        "Your home looks a little empty, doesn't it?",
        "We noticed you looking. We're always looking.",
        "Buy now. Think later. Buy now.",
        "Something in the house feels off? A new purchase will fix that.",
    };
    [TextArea] public string[] lateLines =
    {
        "you already have so much",
        "do you really need it?",
        "the boxes keep piling up",
        "it's quieter when you stop ordering",
        "there's no room left",
        "please stop buying things",
    };
    [Tooltip("Used sometimes once you've earned enough points and skipped buying for a day: a hint at the hidden win.")]
    [TextArea] public string[] restraintLines =
    {
        "...the house feels lighter today.",
        "No orders today? We'll wait.",
        "A few quiet days and the spell might break.",
    };

    [Header("Creep warnings")]
    public string tiredWarning = "You feel so tired. The house is too much.";
    public string forcedSleepLine = "You can't keep your eyes open.";

    [Header("UI")]
    [SerializeField] CanvasGroup group;
    [SerializeField] Text text;
    [SerializeField] AudioClip blip;
    [SerializeField] int pointsToArmStreak = 5;

    float nextAmbient;
    string lastLine;
    Coroutine running;

    void Awake()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    void Start()
    {
        foreach (var s in speakers)
        {
            if (s.prop == null || !s.talksOnClick) continue;
            var speaker = s;
            s.prop.Clicked += _ => Say(speaker);
        }
        nextAmbient = Time.time + 20f;
    }

    void Update()
    {
        bool free = GameManager.Instance != null && GameManager.Instance.WorldInputEnabled;
        if (!free || Time.time < nextAmbient) return;
        nextAmbient = Time.time + UnityEngine.Random.Range(ambientGap.x, ambientGap.y);
        var live = new List<Speaker>();
        foreach (var s in speakers) if (s.prop != null && s.prop.gameObject.activeInHierarchy) live.Add(s);
        if (live.Count > 0) Say(live[UnityEngine.Random.Range(0, live.Count)]);
    }

    public void Say(Speaker s) => Show($"{s.label}:  “{PickLine()}”");

    string PickLine()
    {
        string[] pool;
        int day = PlayerState.Day;
        bool restraint = PlayerState.PointsEarned >= (GameManager.Instance != null ? GameManager.Instance.pointsToArmWinStreak : pointsToArmStreak) && PlayerState.NoPurchaseStreak >= 1 && PlayerState.PurchasesToday == 0;
        if (restraint && UnityEngine.Random.value < 0.5f) pool = restraintLines;
        else pool = day <= 2 ? earlyLines : day <= 4 ? midLines : lateLines;
        if (pool.Length == 0) return "...";
        string line = pool[UnityEngine.Random.Range(0, pool.Length)];
        if (line == lastLine && pool.Length > 1) line = pool[(Array.IndexOf(pool, line) + 1) % pool.Length];
        lastLine = line;
        return line;
    }

    /// Shows any caption text for a few seconds.
    public void Show(string message)
    {
        text.text = message;
        Sfx.Play(blip, 0.3f, 0.7f);
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run());
    }

    public bool IsShowing => group.alpha > 0.01f;

    IEnumerator Run()
    {
        yield return UIAnim.Fade(group, 1f, 0.2f);
        float t = 0f;
        while (t < captionSeconds) { t += Time.unscaledDeltaTime; yield return null; }
        yield return UIAnim.Fade(group, 0f, 0.5f);
        running = null;
    }
}
