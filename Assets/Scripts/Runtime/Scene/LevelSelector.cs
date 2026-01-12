using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct LevelCompletionParams
{
    public float completionTimeSeconds;
    public bool completedWithinTime;

    public static LevelCompletionParams FromTime(float completionTimeSeconds, float targetTimeSeconds)
    {
        return new LevelCompletionParams
        {
            completionTimeSeconds = completionTimeSeconds,
            completedWithinTime = completionTimeSeconds <= targetTimeSeconds
        };
    }
}

public class LevelSelector : MonoBehaviour
{
    static LevelSelector instance;

    public static LevelSelector GetInstance()
    {
        if (instance == null)
        {
            instance = FindAnyObjectByType<LevelSelector>();
            if (instance == null)
            {
                GameObject obj = new GameObject("LevelSelector");
                instance = obj.AddComponent<LevelSelector>();
            }
        }
        return instance;
    }

    [SerializeField] List<LevelSO> levels = new List<LevelSO>();
    [SerializeField] bool dontDestroyOnLoad = true;
    [SerializeField] bool useSceneTransition = true;

    int currentLevelIndex = -1;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            instance.OverrideLevels(levels);
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        UpdateCurrentLevelIndexFromScene();
        EnsureInitialUnlock();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateCurrentLevelIndexFromScene();
    }

    public LevelSO GetCurrentLevel()
    {
        if (currentLevelIndex < 0 || currentLevelIndex >= levels.Count)
        {
            UpdateCurrentLevelIndexFromScene();
        }

        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
        {
            return levels[currentLevelIndex];
        }

        return null;
    }

    public IReadOnlyList<LevelSO> GetLevels()
    {
        return levels;
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= levels.Count)
        {
            Debug.LogWarning($"LevelSelector: invalid level index {index}.");
            return;
        }

        currentLevelIndex = index;
        LoadSceneForLevel(levels[index]);
    }

    public void LoadLevel(LevelSO level)
    {
        if (level == null)
        {
            Debug.LogWarning("LevelSelector: level is null.");
            return;
        }

        int index = levels.IndexOf(level);
        if (index < 0)
        {
            Debug.LogWarning($"LevelSelector: level not in list: {level.name}.");
            return;
        }

        LoadLevel(index);
    }

    public void LoadNextLevel()
    {
        if (levels.Count == 0)
        {
            Debug.LogWarning("LevelSelector: no levels configured.");
            return;
        }

        UpdateCurrentLevelIndexFromScene();
        
        int nextIndex = currentLevelIndex < 0 ? 0 : currentLevelIndex + 1;
        
        Debug.Log($"LevelSelector: Current level index: {currentLevelIndex}, Next index: {nextIndex}, Total levels: {levels.Count}");
        
        if (nextIndex >= levels.Count)
        {
            Debug.Log("LevelSelector: last level completed.");
            return;
        }

        if (levels[nextIndex] == null)
        {
            Debug.LogWarning($"LevelSelector: next level at index {nextIndex} is null.");
            return;
        }

        Debug.Log($"LevelSelector: Loading next level: {levels[nextIndex].name} (scene: {levels[nextIndex].sceneName})");
        LoadLevel(nextIndex);
    }

    public void CompleteLevel(LevelCompletionParams completionParams)
    {
        LevelSO currentLevel = GetCurrentLevel();
        if (currentLevel == null)
        {
            Debug.LogWarning("LevelSelector: no current level found to complete.");
            return;
        }

        SetLevelUnlocked(currentLevel, true);
        SaveCompletion(currentLevel, completionParams);
        UnlockNextLevel(currentLevel);
        LoadNextLevel();
    }

    public bool IsLevelCompleted(LevelSO level)
    {
        string levelId = GetLevelId(level);
        if (string.IsNullOrEmpty(levelId)) return false;
        return PlayerPrefs.GetInt(CompletedKey(levelId), 0) == 1;
    }

    public bool IsLevelUnlocked(LevelSO level)
    {
        if (level == null) return false;
        string levelId = GetLevelId(level);
        if (string.IsNullOrEmpty(levelId)) return false;
        if (!PlayerPrefs.HasKey(UnlockedKey(levelId)))
        {
            return level.defaultUnlocked;
        }
        return PlayerPrefs.GetInt(UnlockedKey(levelId), 0) == 1;
    }

    public void SetLevelUnlocked(LevelSO level, bool unlocked)
    {
        if (level == null) return;
        string levelId = GetLevelId(level);
        if (string.IsNullOrEmpty(levelId)) return;
        PlayerPrefs.SetInt(UnlockedKey(levelId), unlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsNextLevel(LevelSO level)
    {
        if (level == null) return false;
        for (int i = 0; i < levels.Count; i++)
        {
            LevelSO entry = levels[i];
            if (entry == null) continue;
            if (!IsLevelUnlocked(entry)) continue;
            if (IsLevelCompleted(entry)) continue;
            return entry == level;
        }
        return false;
    }

    public bool WasCompletedWithinTime(LevelSO level)
    {
        string levelId = GetLevelId(level);
        if (string.IsNullOrEmpty(levelId)) return false;
        return PlayerPrefs.GetInt(CompletedWithinTimeKey(levelId), 0) == 1;
    }

    public float GetBestTime(LevelSO level)
    {
        string levelId = GetLevelId(level);
        if (string.IsNullOrEmpty(levelId)) return -1f;
        return PlayerPrefs.GetFloat(BestTimeKey(levelId), -1f);
    }

    void LoadSceneForLevel(LevelSO level)
    {
        if (level == null || string.IsNullOrWhiteSpace(level.sceneName))
        {
            Debug.LogWarning("LevelSelector: level has no scene name.");
            return;
        }

        if (useSceneTransition)
        {
            SceneTransition.GetInstance().TransitionToScene(level.sceneName);
        }
        else
        {
            SceneManager.LoadScene(level.sceneName);
        }
    }

    void UpdateCurrentLevelIndexFromScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        currentLevelIndex = GetLevelIndexByScene(sceneName);
    }

    int GetLevelIndexByScene(string sceneName)
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] == null) continue;
            if (levels[i].sceneName == sceneName)
            {
                return i;
            }
        }

        return -1;
    }

    void SaveCompletion(LevelSO level, LevelCompletionParams completionParams)
    {
        string levelId = GetLevelId(level);
        if (string.IsNullOrEmpty(levelId))
        {
            Debug.LogWarning("LevelSelector: level has no id.");
            return;
        }

        PlayerPrefs.SetInt(CompletedKey(levelId), 1);
        if (completionParams.completedWithinTime)
        {
            PlayerPrefs.SetInt(CompletedWithinTimeKey(levelId), 1);
        }

        if (completionParams.completionTimeSeconds > 0f)
        {
            float bestTime = PlayerPrefs.GetFloat(BestTimeKey(levelId), -1f);
            if (bestTime < 0f || completionParams.completionTimeSeconds < bestTime)
            {
                PlayerPrefs.SetFloat(BestTimeKey(levelId), completionParams.completionTimeSeconds);
            }
        }

        PlayerPrefs.Save();
    }

    void EnsureInitialUnlock()
    {
        if (levels.Count == 0) return;

        bool anyUnlocked = false;
        for (int i = 0; i < levels.Count; i++)
        {
            if (IsLevelUnlocked(levels[i]))
            {
                anyUnlocked = true;
                break;
            }
        }

        if (!anyUnlocked)
        {
            SetLevelUnlocked(levels[0], true);
        }
    }

    void UnlockNextLevel(LevelSO currentLevel)
    {
        int index = levels.IndexOf(currentLevel);
        if (index < 0) return;
        int nextIndex = index + 1;
        if (nextIndex < levels.Count && levels[nextIndex] != null)
        {
            SetLevelUnlocked(levels[nextIndex], true);
        }
    }

    void OverrideLevels(List<LevelSO> newLevels)
    {
        if (newLevels == null || newLevels.Count == 0) return;
        levels = new List<LevelSO>(newLevels);
        UpdateCurrentLevelIndexFromScene();
        EnsureInitialUnlock();
    }

    string GetLevelId(LevelSO level)
    {
        if (level == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(level.levelId)) return level.levelId;
        if (!string.IsNullOrWhiteSpace(level.sceneName)) return level.sceneName;
        return level.name;
    }

    string CompletedKey(string levelId)
    {
        return $"LevelCompleted_{levelId}";
    }

    string CompletedWithinTimeKey(string levelId)
    {
        return $"LevelCompletedWithinTime_{levelId}";
    }

    string BestTimeKey(string levelId)
    {
        return $"LevelBestTime_{levelId}";
    }

    string UnlockedKey(string levelId)
    {
        return $"LevelUnlocked_{levelId}";
    }
}
