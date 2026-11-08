using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Shows one room at a time. Arrow buttons (or A/D, ←/→) move through the rooms in order, wrapping around.
/// Neighbouring rooms pan; distant ones (bathroom, balcony) cut through a quick fade.
/// The view is letterboxed to each room's shape, so you never see into the next room.
[RequireComponent(typeof(Camera))]
public class RoomCamera : MonoBehaviour
{
    [Serializable]
    public class Room
    {
        public string name;
        public Rect area; // world-space rectangle the camera frames exactly
    }

    public Room[] rooms;
    public int startRoom;
    [SerializeField] Button leftArrow, rightArrow;
    [SerializeField] CanvasGroup fade;
    [SerializeField] float panTime = 0.45f;
    [SerializeField] float panMaxDistance = 25f;
    [Header("Blackout (non-neighbouring rooms)")]
    [SerializeField] float blackIn = 0.3f;
    [SerializeField] float blackHold = 0.15f;
    [SerializeField] float blackOut = 0.4f;
    [SerializeField] AudioClip moveSound;

    public int Current { get; private set; }
    public bool Moving { get; private set; }
    public event Action<int> RoomChanged;

    Camera cam;
    int lastWidth, lastHeight; // screen size the current framing was computed for
    Rect shown;                // what the camera is framing right now (mid-move this is not rooms[Current])

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (leftArrow != null) leftArrow.onClick.AddListener(() => Step(-1));
        if (rightArrow != null) rightArrow.onClick.AddListener(() => Step(+1));
    }

    void Start() => JumpTo(startRoom);

    void Update()
    {
        // Re-fit only when the window really changed size. (A float aspect comparison here was never equal in builds,
        // so it snapped the camera back to the old room every frame during the blackout.)
        if (Screen.width != lastWidth || Screen.height != lastHeight) Frame(shown);
        bool input = GameManager.Instance == null || GameManager.Instance.WorldInputEnabled;
        if (leftArrow != null) leftArrow.gameObject.SetActive(input);
        if (rightArrow != null) rightArrow.gameObject.SetActive(input);
        if (!input || Moving) return;
        if (GameInput.LeftPressed) Step(-1);
        else if (GameInput.RightPressed) Step(+1);
    }

    public void Step(int dir)
    {
        if (Moving || rooms.Length == 0) return;
        int next = (Current + dir + rooms.Length) % rooms.Length;
        StartCoroutine(MoveTo(next));
    }

    public void JumpTo(int index)
    {
        Current = Mathf.Clamp(index, 0, rooms.Length - 1);
        Frame(rooms[Current].area);
        RoomChanged?.Invoke(Current);
    }

    IEnumerator MoveTo(int next)
    {
        Moving = true;
        Sfx.Play(moveSound, 0.35f);
        Rect from = rooms[Current].area, to = rooms[next].area;
        // Only pan between side-by-side rooms on the same floor; everything else cuts through black.
        bool sameRow = Mathf.Abs(from.center.y - to.center.y) < 1f;
        bool pan = sameRow && Vector2.Distance(from.center, to.center) <= panMaxDistance && Mathf.Approximately(from.width / from.height, to.width / to.height);
        if (pan)
        {
            float t = 0f;
            while (t < panTime)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                float k = Mathf.SmoothStep(0f, 1f, t / panTime);
                Frame(new Rect(Vector2.Lerp(from.position, to.position, k), Vector2.Lerp(from.size, to.size, k)));
                yield return null;
            }
        }
        else if (fade != null)
        {
            fade.blocksRaycasts = true;
            yield return UIAnim.Fade(fade, 1f, blackIn);
            // Switch only once fully black, then hold so the new room has rendered (and any texture-upload hitch
            // has happened) before anything is revealed.
            fade.alpha = 1f;
            Frame(to);
            Current = next;
            RoomChanged?.Invoke(Current); // room name appears together with the new room
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(blackHold);
            yield return UIAnim.Fade(fade, 0f, blackOut);
            fade.blocksRaycasts = false;
        }
        Frame(to);
        Moving = false;
        if (Current != next)
        {
            Current = next;
            RoomChanged?.Invoke(Current);
        }
    }

    /// Fits the rect exactly: orthographic size from its height, viewport letterboxed to its aspect.
    void Frame(Rect area)
    {
        shown = area;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        float screenAspect = Screen.width / (float)Screen.height;
        float roomAspect = area.width / area.height;
        Rect viewport = new Rect(0, 0, 1, 1);
        if (screenAspect > roomAspect) { float w = roomAspect / screenAspect; viewport = new Rect((1 - w) / 2, 0, w, 1); }
        else { float h = screenAspect / roomAspect; viewport = new Rect(0, (1 - h) / 2, 1, h); }
        cam.rect = viewport;
        cam.orthographicSize = area.height / 2f;
        transform.position = new Vector3(area.center.x, area.center.y, transform.position.z);
    }

    public bool IsVisible(Vector3 world)
    {
        Vector3 v = cam.WorldToViewportPoint(world);
        return v.x >= 0 && v.x <= 1 && v.y >= 0 && v.y <= 1;
    }
}
