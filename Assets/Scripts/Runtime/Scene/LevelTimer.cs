using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using ThiccTapeman.Player.Reset;

public class LevelTimer : MonoBehaviour
{
    static LevelTimer instance;

    public static LevelTimer GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<LevelTimer>();
            if (instance == null)
            {
                GameObject obj = new GameObject("LevelTimer");
                instance = obj.AddComponent<LevelTimer>();
            }
        }
        return instance;
    }

    [SerializeField] float startDelaySeconds = 1.5f;
    [SerializeField] bool dontDestroyOnLoad = false;

    float elapsedTime;
    float checkpointTime;
    bool isRunning;
    bool isPausedOverride;
    Coroutine startRoutine;
    ResetManager resetManager;

    public float ElapsedTime => elapsedTime;
    public bool IsRunning => isRunning;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        BeginTimer();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        resetManager = ResetManager.GetInstance();
        if (resetManager != null)
        {
            resetManager.OnReset += HandleReset;
        }
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (resetManager != null)
        {
            resetManager.OnReset -= HandleReset;
        }
    }

    void Update()
    {
        if (!isRunning) return;
        if (ShouldPause()) return;

        elapsedTime += Time.deltaTime;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!dontDestroyOnLoad)
        {
            BeginTimer();
        }
    }

    public void BeginTimer()
    {
        elapsedTime = 0f;
        checkpointTime = 0f;
        isRunning = false;

        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
        }
        startRoutine = StartCoroutine(StartAfterDelay());
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void SetPaused(bool paused)
    {
        isPausedOverride = paused;
    }

    public void SetCheckpointReference(float timeSeconds)
    {
        checkpointTime = Mathf.Max(0f, timeSeconds);
    }

    public void SetCheckpointReferenceToCurrent()
    {
        checkpointTime = elapsedTime;
    }

    IEnumerator StartAfterDelay()
    {
        if (startDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(startDelaySeconds);
        }
        isRunning = true;
    }

    void HandleReset()
    {
        elapsedTime = checkpointTime;
    }

    bool ShouldPause()
    {
        if (isPausedOverride) return true;
        if (DialogueManager.IsDialogueActive) return true;
        if (Time.timeScale <= 0f) return true;

        return false;
    }
}
