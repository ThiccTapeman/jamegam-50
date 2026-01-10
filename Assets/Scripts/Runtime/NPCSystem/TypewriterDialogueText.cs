using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterText : MonoBehaviour
{
    [SerializeField] private float charactersPerSecond = 40f;

    private TextMeshProUGUI text;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    public void TypeText(string message)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeRoutine(message));
    }

    private IEnumerator TypeRoutine(string message)
    {
        text.text = message;
        text.ForceMeshUpdate();

        int totalCharacters = text.textInfo.characterCount;
        text.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalCharacters; i++)
        {
            text.maxVisibleCharacters = i;
            yield return new WaitForSeconds(1f / charactersPerSecond);
        }

        typingCoroutine = null;
    }

    public void Skip()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            text.maxVisibleCharacters = text.textInfo.characterCount;
        }
    }

    public bool IsTyping => typingCoroutine != null;
}