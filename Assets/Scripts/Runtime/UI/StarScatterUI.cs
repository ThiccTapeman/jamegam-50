using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StarScatterUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] RectTransform container;
    [SerializeField, Min(1)] int starCount = 10;
    [SerializeField, Range(0f, 0.5f)] float minSpacingScreenFraction = 0.05f;
    [SerializeField] bool regenerateOnEnable = true;
    [SerializeField] bool useLevelsFromManager = true;

    [Header("Prefabs")]
    [SerializeField] RectTransform starPrefab;
    [SerializeField] RectTransform linePrefab;
    [SerializeField] bool lineUsesHeightForLength = false;
    [SerializeField] float lineRotationOffsetDegrees = 0f;
    [SerializeField] float lineLengthPadding = 0f;
    [SerializeField] float lineWidthScale = 1f;

    [Header("Random")]
    [SerializeField] bool useSeed = true;
    [SerializeField] int seed = 12345;

    readonly List<RectTransform> spawnedStars = new List<RectTransform>();
    readonly List<RectTransform> spawnedLines = new List<RectTransform>();

    void OnEnable()
    {
        if (regenerateOnEnable)
        {
            Regenerate();
        }
    }

    public void Regenerate()
    {
        if (container == null || starPrefab == null)
        {
            Debug.LogWarning("StarScatterUI missing container or starPrefab.");
            return;
        }

        ClearSpawned();

        Random.State previousState = Random.state;
        if (useSeed)
        {
            Random.InitState(seed);
        }

        LevelSelector selector = useLevelsFromManager ? LevelSelector.GetInstance() : null;
        IReadOnlyList<LevelSO> levels = selector != null ? selector.GetLevels() : null;
        int targetCount = levels != null ? levels.Count : starCount;
        if (targetCount <= 0)
        {
            Debug.LogWarning("StarScatterUI has no levels or starCount set.");
            return;
        }

        Rect rect = container.rect;
        Vector2 starSize = GetPrefabSize(starPrefab) * 0.01f;
        Vector2 halfStar = starSize * 0.5f;
        float baseSpacing = Mathf.Min(rect.width, rect.height) * minSpacingScreenFraction;
        float minSpacing = Mathf.Max(baseSpacing, Mathf.Max(starSize.x, starSize.y));

        List<Vector2> positions = new List<Vector2>(targetCount);

        int attempts = 0;
        int maxAttempts = Mathf.Max(50, targetCount * 30);
        while (positions.Count < targetCount && attempts < maxAttempts)
        {
            attempts++;

            float x = Random.Range(rect.xMin + halfStar.x, rect.xMax - halfStar.x);
            float y = Random.Range(rect.yMin + halfStar.y, rect.yMax - halfStar.y);
            Vector2 candidate = new Vector2(x, y);

            if (IsFarEnough(candidate, positions, minSpacing))
            {
                positions.Add(candidate);
            }
        }

        if (positions.Count < targetCount)
        {
            Debug.LogWarning($"StarScatterUI could only place {positions.Count} / {targetCount} stars with current spacing.");
        }

        SortPositionsLeftToRight(positions);

        for (int i = 0; i < positions.Count; i++)
        {
            RectTransform star = Instantiate(starPrefab, container);
            ForceCenteredAnchors(star);
            star.anchoredPosition = positions[i];
            spawnedStars.Add(star);

            if (levels != null && i < levels.Count)
            {
                LevelSelectorButton button = star.GetComponent<LevelSelectorButton>();
                if (button == null)
                {
                    button = star.GetComponentInChildren<LevelSelectorButton>();
                }
                if (button != null)
                {
                    button.SetLevel(levels[i]);
                }
            }
        }

        if (linePrefab != null && positions.Count >= 2)
        {
            for (int i = 0; i < positions.Count - 1; i++)
            {
                SpawnLine(positions[i], positions[i + 1]);
            }
        }

        if (useSeed)
        {
            Random.state = previousState;
        }
    }

    public void ClearSpawned()
    {
        for (int i = 0; i < spawnedStars.Count; i++)
        {
            if (spawnedStars[i] != null)
                Destroy(spawnedStars[i].gameObject);
        }
        spawnedStars.Clear();

        for (int i = 0; i < spawnedLines.Count; i++)
        {
            if (spawnedLines[i] != null)
                Destroy(spawnedLines[i].gameObject);
        }
        spawnedLines.Clear();
    }

    bool IsFarEnough(Vector2 candidate, List<Vector2> existing, float minSpacing)
    {
        float minSqr = minSpacing * minSpacing;
        for (int i = 0; i < existing.Count; i++)
        {
            if ((existing[i] - candidate).sqrMagnitude < minSqr)
                return false;
        }
        return true;
    }

    Vector2 GetPrefabSize(RectTransform prefab)
    {
        if (prefab == null) return Vector2.zero;
        return prefab.rect.size;
    }

    void ForceCenteredAnchors(RectTransform rectTransform)
    {
        if (rectTransform == null) return;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
    }

    void SpawnLine(Vector2 from, Vector2 to)
    {
        RectTransform line = Instantiate(linePrefab, container);
        ForceCenteredAnchors(line);
        Vector2 dir = to - from;
        float distance = dir.magnitude;
        Vector2 midpoint = from + dir * 0.5f;

        line.anchoredPosition = midpoint;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + lineRotationOffsetDegrees;
        line.localRotation = Quaternion.Euler(0f, 0f, angle);

        Vector2 baseSize = line.sizeDelta;
        float baseLength = lineUsesHeightForLength ? baseSize.y : baseSize.x;
        float length = Mathf.Max(0f, distance + lineLengthPadding);
        float scale = baseLength > 0f ? length / baseLength : 1f;

        Vector3 localScale = line.localScale;
        if (lineUsesHeightForLength)
            localScale.y = scale;
        else
            localScale.x = scale;
        float widthScale = Mathf.Max(0f, lineWidthScale);
        if (lineUsesHeightForLength)
            localScale.x = widthScale;
        else
            localScale.y = widthScale;
        line.localScale = localScale;

        spawnedLines.Add(line);
    }

    void SortPositionsLeftToRight(List<Vector2> positions)
    {
        positions.Sort((a, b) => a.x.CompareTo(b.x));
        if (container != null && container.lossyScale.x < 0f)
        {
            positions.Reverse();
        }
    }
}
