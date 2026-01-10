using System.Collections;
using UnityEngine;

public sealed class MusicDirector : MonoBehaviour
{
    private static MusicDirector instance;

    [Header("Defaults")]
    [SerializeField] private float defaultFadeSeconds = 1f;
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private float targetVolume = 1f;

    [Header("Sources")]
    [SerializeField] private AudioSource sourceA;
    [SerializeField] private AudioSource sourceB;

    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private Coroutine fadeRoutine;

    public static MusicDirector GetInstance()
    {
        if (instance != null) return instance;

        var obj = new GameObject("MusicDirector");
        instance = obj.AddComponent<MusicDirector>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (sourceA == null) sourceA = gameObject.AddComponent<AudioSource>();
        if (sourceB == null) sourceB = gameObject.AddComponent<AudioSource>();

        sourceA.playOnAwake = false;
        sourceB.playOnAwake = false;

        activeSource = sourceA;
        inactiveSource = sourceB;
    }

    public void FadeTo(AudioClip clip, float fadeSeconds, bool alignToCurrent, bool loop)
    {
        if (clip == null) return;

        if (activeSource.clip == clip && activeSource.isPlaying)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        float duration = fadeSeconds > 0f ? fadeSeconds : defaultFadeSeconds;

        inactiveSource.clip = clip;
        inactiveSource.loop = loop;
        inactiveSource.volume = 0f;

        if (alignToCurrent && activeSource.clip != null && activeSource.clip.length > 0f)
        {
            float t = activeSource.time;
            inactiveSource.time = Mathf.Repeat(t, clip.length);
        }
        else
        {
            inactiveSource.time = 0f;
        }

        inactiveSource.Play();

        if (duration <= 0f)
        {
            activeSource.Stop();
            inactiveSource.volume = targetVolume;
            SwapSources();
            return;
        }

        fadeRoutine = StartCoroutine(FadeRoutine(duration));
    }

    private IEnumerator FadeRoutine(float duration)
    {
        float time = 0f;
        float startActiveVolume = activeSource.volume;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            activeSource.volume = Mathf.Lerp(startActiveVolume, 0f, t);
            inactiveSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        activeSource.Stop();
        inactiveSource.volume = targetVolume;
        SwapSources();
        fadeRoutine = null;
    }

    private void SwapSources()
    {
        var temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;
    }
}
