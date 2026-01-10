using UnityEngine;

public class InfinateLoopTilemap : MonoBehaviour
{
    [SerializeField] float moveDistance = 1f;
    [SerializeField] float moveSpeed = 1f; // units per second

    Vector3 startPosition;
    float moveTimer;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (moveSpeed <= 0f || moveDistance <= 0f) return;

        float duration = moveDistance / moveSpeed;
        moveTimer += Time.deltaTime;

        float t = Mathf.Clamp01(moveTimer / duration);
        transform.position = Vector3.Lerp(startPosition, startPosition + Vector3.left * moveDistance, t);

        if (t >= 1f)
        {
            transform.position = startPosition;
            moveTimer = 0f;
        }
    }
}
