using ThiccTapeman.Timeline;
using UnityEngine;

public class RewindZone : MonoBehaviour
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

        if (!(collision.CompareTag("Player") && collision.isTrigger)) return;


        TimelineManager.GetInstance().Rewind(rewindSeconds);
    }
}
