using UnityEngine;

public class PlayerMovementVisualizer : MonoBehaviour
{
    void OnGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
