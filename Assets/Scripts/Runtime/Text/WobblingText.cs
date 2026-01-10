using UnityEngine;

public class WobblingText : MonoBehaviour
{
    [SerializeField] private float wobbleSpeed = 5f;
    [SerializeField] private float wobbleAmount = 0.1f;

    [SerializeField] private TMPro.TextMeshProUGUI textMeshPro;

    private Vector3 initialPosition;

    private void Start()
    {
        initialPosition = transform.localPosition;
    }

    private void Update()
    {
        float wobbleOffset = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount;
        transform.localPosition = initialPosition + new Vector3(0, wobbleOffset, 0);
    }
}
