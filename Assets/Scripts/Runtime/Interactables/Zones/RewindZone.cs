using ThiccTapeman.Timeline;
using UnityEngine;

public class RewindZone : MonoBehaviour
{
    [SerializeField] float rewindSeconds = 1000f;
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

        if (!(collision.CompareTag("Player") && collision.isTrigger)) return;


        TimelineManager.GetInstance().Rewind(rewindSeconds);
    }
}
