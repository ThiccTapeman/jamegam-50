using ThiccTapeman.Timeline;
using UnityEngine;
using ThiccTapeman.Player.Reset;

public class ResetZone : MonoBehaviour
{
    private const float RewindTriggerIgnoreSeconds = 0.1f;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return;

        var timelineManager = TimelineManager.TryGetInstance();
        if (timelineManager != null && Time.time - timelineManager.LastRewindTime < RewindTriggerIgnoreSeconds)
            return;

        var timelineObject = collision.GetComponent<TimelineObject>();
        if (timelineObject != null && timelineObject.IsBranchInstance)
        {
            Destroy(collision.gameObject);
            return;
        }

        if (!collision.CompareTag("Player")) return;
        if (collision.isTrigger) return;

        ResetManager.GetInstance().Reset();
    }
}
