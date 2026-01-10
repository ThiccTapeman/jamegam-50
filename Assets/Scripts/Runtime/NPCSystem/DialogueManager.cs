
using UnityEngine;
using TMPro;
using UIButton = UnityEngine.UI.Button; 
using System.Collections;
using System.Collections.Generic;
using ThiccTapeman.Input;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI npcText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private UIButton choiceButtonPrefab;
    [SerializeField] private TypewriterText typewriter;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private CanvasGroup choicesCanvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float fadeSpeed = 1.25f;
    [SerializeField] private float choicesFadeDelay = 0.15f;

    private NPCDialogueData currentDialogue;
    private DialogueState state;
    private bool isEndingDialogue;

    private Queue<string> npcLineQueue = new Queue<string>();

    private void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
        
        if (dialogueCanvasGroup == null)
            dialogueCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        if (choicesCanvasGroup == null)
            choicesCanvasGroup = choicesContainer.GetComponent<CanvasGroup>();
    }

    public void StartDialogue(NPCDialogueData dialogue)
    {
        currentDialogue = dialogue;
        StartCoroutine(FadeInDialogue(dialogue));
    }

    
    private IEnumerator FadeInDialogue(NPCDialogueData dialogue)
    {
        dialoguePanel.SetActive(true);
        dialogueCanvasGroup.alpha = 0f;

        npcText.text = "";
        typewriter.Skip(); 
    
        if (dialogue.choices != null)
        {
            foreach (var choice in dialogue.choices)
            {
                choice.hasBeenChosen = false;
            }
        }

        SetSpeakerName("???");

        while (dialogueCanvasGroup.alpha < 1f)
        {
            dialogueCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        dialogueCanvasGroup.alpha = 1f;

        typewriter.TypeText(dialogue.introLine);
        state = DialogueState.Intro;

        ClearChoices();
    }

    private void Update()
    {
        if (!dialoguePanel.activeSelf)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        if (typewriter.IsTyping)
        {
            typewriter.Skip();
            return;
        }

        if (state == DialogueState.Intro || state == DialogueState.NPCResponding)
        {
            if (npcLineQueue.Count > 0)
            {
                SetSpeakerName("???");
                typewriter.TypeText(npcLineQueue.Dequeue());
            }
            else
            {
                if (isEndingDialogue)
                {
                    ConfessionBooth booth = FindObjectOfType<ConfessionBooth>();
                    if (booth != null)
                    {
                        booth.PlayBellSound();
                    }
                    StartCoroutine(FadeOutAndEndDialogue());
                }
                else
                {
                    StartCoroutine(ShowChoicesWithFade());
                }
            }
        }
    }

    
    private IEnumerator ShowChoicesWithFade()
    {
        state = DialogueState.Choosing;
        
        yield return StartCoroutine(FadeOutText());
        
        SetSpeakerName("You");
        
        ClearChoices();
        bool hasAvailableChoices = false;

        if (currentDialogue.choices != null)
        {
            foreach (var choice in currentDialogue.choices)
            {
                if (choice.hasBeenChosen)
                    continue;

                hasAvailableChoices = true;
                CreateChoiceButton(choice);
            }
        }

        CreateLeaveButton();

        if (!hasAvailableChoices)
        {
            TriggerEndDialogue();
            yield break;
        }

        yield return new WaitForSeconds(choicesFadeDelay);
        yield return StartCoroutine(FadeInChoices());
    }

    private IEnumerator FadeOutText()
    {
        CanvasGroup textGroup = npcText.GetComponent<CanvasGroup>();
        if (textGroup == null)
            textGroup = npcText.gameObject.AddComponent<CanvasGroup>();

        while (textGroup.alpha > 0f)
        {
            textGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        
        npcText.text = "";
        textGroup.alpha = 1f; 
    }

    private IEnumerator FadeInChoices()
    {
        choicesCanvasGroup.alpha = 0f;
        
        while (choicesCanvasGroup.alpha < 1f)
        {
            choicesCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        choicesCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutChoices()
    {
        while (choicesCanvasGroup.alpha > 0f)
        {
            choicesCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
    }

    private void SetSpeakerName(string speakerName)
    {
        if (nameText != null)
        {
            nameText.text = speakerName;
        }
    }

    private void CreateChoiceButton(PlayerChoice choice)
    {
        UIButton button = Instantiate(choiceButtonPrefab, choicesContainer);
        button.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;

        button.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleInButton(button.transform));

        button.onClick.AddListener(() =>
        {
            choice.hasBeenChosen = true;
            StartCoroutine(HandleChoiceSelection(choice.npcResponses));
        });
    }

    private void CreateLeaveButton()
    {
        UIButton button = Instantiate(choiceButtonPrefab, choicesContainer);
        button.GetComponentInChildren<TextMeshProUGUI>().text = "Leave";

        button.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleInButton(button.transform));

        button.onClick.AddListener(() => StartCoroutine(HandleChoiceSelection(null)));
    }

    private IEnumerator ScaleInButton(Transform buttonTransform)
    {
        float elapsed = 0f;
        float duration = 0.2f;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float scale = Mathf.Lerp(0f, 1f, Mathf.SmoothStep(0f, 1f, progress));
            buttonTransform.localScale = Vector3.one * scale;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        buttonTransform.localScale = Vector3.one;
    }

    private IEnumerator HandleChoiceSelection(List<string> responses)
    {
        yield return StartCoroutine(FadeOutChoices());
        
        if (responses == null)
        {
            TriggerEndDialogue();
        }
        else
        {
            StartNPCResponse(responses);
        }
    }

    private void TriggerEndDialogue()
    {
        ClearChoices();
        npcLineQueue.Clear();

        npcLineQueue.Enqueue(currentDialogue.endLine);

        state = DialogueState.NPCResponding;
        isEndingDialogue = true;

        SetSpeakerName("???");
        typewriter.TypeText(npcLineQueue.Dequeue());
    }

    private void StartNPCResponse(List<string> responses)
    {
        ClearChoices();
        npcLineQueue.Clear();

        foreach (var line in responses)
            npcLineQueue.Enqueue(line);

        state = DialogueState.NPCResponding;
        SetSpeakerName("???");
        typewriter.TypeText(npcLineQueue.Dequeue());
    }

    private void ClearChoices()
    {
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);
            
        choicesCanvasGroup.alpha = 0f;
    }

    private IEnumerator FadeOutAndEndDialogue()
    {
        while (dialogueCanvasGroup.alpha > 0f)
        {
            dialogueCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        EndDialogue();
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        currentDialogue = null;
        isEndingDialogue = false;
        
        // Reset canvas group alpha for next dialogue
        dialogueCanvasGroup.alpha = 1f;
        choicesCanvasGroup.alpha = 0f;
    }
}