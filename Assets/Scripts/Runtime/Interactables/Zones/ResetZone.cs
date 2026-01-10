using ThiccTapeman.Timeline;
using UnityEngine;
using ThiccTapeman.Player.Reset;

public class ResetZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return;

        var timelineObject = collision.GetComponent<TimelineObject>();
        if (timelineObject != null && timelineObject.IsBranchInstance)
        {
            Destroy(collision.gameObject);
            return;
        }

        if (!collision.CompareTag("Player") || collision.isTrigger) return;

        ResetManager.GetInstance().Reset();
    }
}
