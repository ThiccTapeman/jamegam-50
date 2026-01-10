using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DialogueButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.grey;
    [SerializeField] private Color clickColor = Color.gray3;

    private TextMeshProUGUI buttonText;
    private bool isHovering = false;

    private void Awake()
    {
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            normalColor = buttonText.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (buttonText != null)
        {
            buttonText.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (buttonText != null)
        {
            buttonText.color = normalColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (buttonText != null)
        {
            buttonText.color = clickColor;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (buttonText != null)
        {
            buttonText.color = isHovering ? hoverColor : normalColor;
        }
    }
}