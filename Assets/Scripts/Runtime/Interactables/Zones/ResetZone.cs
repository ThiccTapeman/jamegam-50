using ThiccTapeman.Timeline;
using UnityEngine;
using ThiccTapeman.Player.Reset;

public class ResetZone : MonoBehaviour
{
    [SerializeField] float rewindSeconds = 1000f;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return;

        var timelineObject = collision.GetComponent<TimelineObject>();
        if (timelineObject != null && timelineObject.IsBranchInstance)
        {
            Destroy(collision.gameObject);
            return;
        }

        if (!collision.CompareTag("Player")) return;

        ResetManager.GetInstance().Reset();
    }
}
