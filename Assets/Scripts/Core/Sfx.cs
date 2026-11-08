using UnityEngine;

/// Fire-and-forget sound effects from a small pool, so overlapping clicks never cut each other off.
public static class Sfx
{
    static AudioSource[] pool;
    static int next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { pool = null; next = 0; }

    static void Ensure()
    {
        if (pool != null && pool[0] != null) return;
        var go = new GameObject("[Sfx]");
        Object.DontDestroyOnLoad(go);
        pool = new AudioSource[10];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = go.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
        }
    }

    public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        Ensure();
        var s = pool[next];
        next = (next + 1) % pool.Length;
        s.Stop();
        s.clip = clip;
        s.volume = volume;
        s.pitch = pitch;
        s.Play();
    }
}
