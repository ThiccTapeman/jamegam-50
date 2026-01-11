using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectorButton : MonoBehaviour
{
    [SerializeField] LevelSO level;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] Image statusImage;
    [SerializeField] UnityEngine.UI.Button button;

    [Header("Colors")]
    [SerializeField] Color completedLateColor = Color.yellow;
    [SerializeField] Color completedInTimeColor = Color.green;
    [SerializeField] Color notCompletedColor = Color.white;
    [SerializeField] Color unlockedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] Color pulseColor = new Color(1f, 1f, 1f, 1f);

    [Header("Pulse")]
    [SerializeField, Min(0f)] float pulseSpeed = 2f;
    [SerializeField, Range(0f, 1f)] float pulseAmount = 0.25f;

    Color baseColor;
    bool shouldPulse;
    Coroutine refreshRoutine;

    void OnEnable()
    {
        if (nameText == null)
        {
            nameText = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (statusImage == null)
        {
            statusImage = GetComponent<Image>();
            if (statusImage == null)
            {
                statusImage = GetComponentInChildren<Image>();
            }
        }
        if (button == null)
        {
            button = GetComponent<UnityEngine.UI.Button>();
            if (button == null)
            {
                button = GetComponentInChildren<UnityEngine.UI.Button>();
            }
        }
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
        StartRefreshRoutine();
    }

    void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    void Update()
    {
        if (statusImage == null) return;
        if (!shouldPulse)
        {
            statusImage.color = baseColor;
            return;
        }

        float t = Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f;
        float u = t * pulseAmount;
        statusImage.color = Color.Lerp(baseColor, pulseColor, u);
    }

    public void SetLevel(LevelSO levelSO)
    {
        level = levelSO;
        StartRefreshRoutine();
    }

    public void Refresh()
    {
        if (level == null) return;

        if (nameText != null)
        {
            nameText.text = string.IsNullOrWhiteSpace(level.levelName) ? level.name : level.levelName;
        }

        if (statusImage == null) return;

        LevelSelector selector = LevelSelector.GetInstance();
        if (selector == null)
        {
            statusImage.color = notCompletedColor;
            return;
        }

        bool isUnlocked = selector.IsLevelUnlocked(level);
        Debug.Log("Is Unlocked: " + isUnlocked);
        if (button != null)
        {
            button.interactable = isUnlocked;
        }

        Debug.Log("Is Level Completed: " + selector.IsLevelCompleted(level));
        if (!selector.IsLevelCompleted(level))
        {
            shouldPulse = selector.IsNextLevel(level);
            baseColor = isUnlocked ? unlockedColor : lockedColor;
            if (!isUnlocked)
            {
                shouldPulse = false;
            }
            statusImage.color = baseColor;
            return;
        }

        shouldPulse = false;
        baseColor = selector.WasCompletedWithinTime(level) ? completedInTimeColor : completedLateColor;
        statusImage.color = baseColor;
    }

    void StartRefreshRoutine()
    {
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
        }
        refreshRoutine = StartCoroutine(RefreshWhenReady());
    }

    IEnumerator RefreshWhenReady()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return null;
            if (LevelSelector.GetInstance() != null)
            {
                Refresh();
                break;
            }
        }
        refreshRoutine = null;
    }

    void HandleClick()
    {
        Debug.Log("HandleCLick");
        if (level == null) return;
        LevelSelector selector = LevelSelector.GetInstance();
        if (selector == null) return;

        selector.LoadLevel(level);
    }
}
