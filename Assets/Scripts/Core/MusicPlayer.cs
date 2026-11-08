using System.Collections;
using UnityEngine;

/// Days 1-4 play their own track; after that a random track each day (never the same twice in a row).
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public AudioClip[] dayTracks;
    [Range(0f, 1f)] public float volume = 0.5f;
    [Tooltip("Pitch drop set by Atmosphere when anomalies pile up (0 = in tune).")]
    public float detune;

    AudioSource source;
    int lastIndex = -1;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
    }

    void Update() => source.pitch = Mathf.MoveTowards(source.pitch, 1f - detune, Time.unscaledDeltaTime * 0.1f);

    public void PlayDay(int day)
    {
        if (dayTracks == null || dayTracks.Length == 0) return;
        int index;
        if (day <= dayTracks.Length) index = day - 1;
        else
        {
            index = Random.Range(0, dayTracks.Length);
            if (dayTracks.Length > 1 && index == lastIndex) index = (index + 1) % dayTracks.Length;
        }
        lastIndex = index;
        StopAllCoroutines();
        source.clip = dayTracks[index];
        source.Play();
        StartCoroutine(FadeTo(volume, 1.5f));
    }

    public void FadeOut(float duration = 1f)
    {
        StopAllCoroutines();
        StartCoroutine(FadeTo(0f, duration));
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float from = source.volume, t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(from, target, t / duration);
            yield return null;
        }
        source.volume = target;
        if (target <= 0f) source.Stop();
    }
}
