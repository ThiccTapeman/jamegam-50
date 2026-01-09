using ThiccTapeman.Input;
using ThiccTapeman.Timeline;
using TMPro;
using UnityEngine;

public class TimelineTimeDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;

    // Update is called once per frame
    void Update()
    {
        timeText.text = $"{TimelineManager.GetInstance().TimeNow:F2}s";
    }
}
