using ThiccTapeman.Input;
using ThiccTapeman.Timeline;
using TMPro;
using UnityEngine;

public class LevelTimerDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;

    // Update is called once per frame
    void Update()
    {
        timeText.text = $"{LevelTimer.GetInstance().ElapsedTime:F2}s";
    }
}
