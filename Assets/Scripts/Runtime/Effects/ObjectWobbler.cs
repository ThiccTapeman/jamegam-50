using UnityEngine;

public sealed class ObjectWobbler : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.25f;
    [SerializeField] private float frequency = 1f;
    [SerializeField] private bool useLocalSpace = true;

    private Vector3 startPosition;
    private float timeOffset;

    private void Awake()
    {
        startPosition = useLocalSpace ? transform.localPosition : transform.position;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float offset = Mathf.Sin((Time.time * frequency) + timeOffset) * amplitude;

        if (useLocalSpace)
            transform.localPosition = startPosition + Vector3.up * offset;
        else
            transform.position = startPosition + Vector3.up * offset;
    }
}
