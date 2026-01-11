using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Game/Level", fileName = "Level")]
public class LevelSO : ScriptableObject
{
    [Header("Identification")]
    public string levelId;
    public string levelName;

    [TextArea]
    public string description;

    [Header("Scene")]
    public string sceneName;
#if UNITY_EDITOR
    public SceneAsset sceneAsset;
#endif

    [Header("Gameplay")]
    public int difficulty;
    public float targetTimeSeconds;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            levelId = name;
        }

        if (sceneAsset != null)
        {
            sceneName = sceneAsset.name;
        }
    }
#endif
}
