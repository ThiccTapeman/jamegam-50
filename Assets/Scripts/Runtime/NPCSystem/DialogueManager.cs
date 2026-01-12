using UnityEngine;
using TMPro;
using UIButton = UnityEngine.UI.Button; 
using System.Collections;
using System.Collections.Generic;
using ThiccTapeman.Input;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public static bool IsDialogueActive { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI npcText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private UIButton choiceButtonPrefab;
    [SerializeField] private TypewriterText typewriter;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float fadeSpeed = 1.25f;
    [SerializeField] private float choicesFadeDelay = 0.15f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundManager.Sound confirmSound;

    private DialogueData currentDialogue;
    private DialogueState state;
    private bool isEndingDialogue;
    private string currentNPCName;


    private Queue<string> npcLineQueue = new Queue<string>();

    private void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
        
        if (dialogueCanvasGroup == null)
            dialogueCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
    }

    public void StartDialogue(DialogueData dialogue, string npcName = "???")
    {
        currentDialogue = dialogue;
        currentNPCName = npcName;
        IsDialogueActive = true;
        StartCoroutine(FadeInDialogue(dialogue));
    }

    
    private IEnumerator FadeInDialogue(DialogueData dialogue)
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

        SetSpeakerName(currentNPCName);

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
        confirmSound.PlaySound(audioSource);

        if (typewriter.IsTyping)
        {
            typewriter.Skip();
            return;
        }

        if (state == DialogueState.Intro || state == DialogueState.NPCResponding)
        {
            if (npcLineQueue.Count > 0)
            {
                SetSpeakerName(currentNPCName);
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
                    ShowLeaveButtonOnly();
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
                if (string.IsNullOrWhiteSpace(choice.choiceText))
                    continue;

                hasAvailableChoices = true;
                CreateChoiceButton(choice);
            }
        }

        if (!hasAvailableChoices)
        {
            TriggerEndDialogue();
            yield break;
        }

        yield return new WaitForSeconds(choicesFadeDelay);
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
            HandleChoiceSelection(choice.npcResponses);
        });
    }

    private void CreateLeaveButton()
    {
        UIButton button = Instantiate(choiceButtonPrefab, choicesContainer);
        button.GetComponentInChildren<TextMeshProUGUI>().text = "Leave";

        button.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleInButton(button.transform));

        button.onClick.AddListener(HandleLeaveSelected);
    }

    private IEnumerator ScaleInButton(Transform buttonTransform)
    {
        float elapsed = 0f;
        float duration = 0.2f;
        
        while (elapsed < duration)
        {
            if (buttonTransform == null) yield break; // Safety check
            
            float progress = elapsed / duration;
            float scale = Mathf.Lerp(0f, 1f, Mathf.SmoothStep(0f, 1f, progress));
            buttonTransform.localScale = Vector3.one * scale;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (buttonTransform != null)
            buttonTransform.localScale = Vector3.one;
    }

    private void HandleChoiceSelection(List<string> responses)
    {
        if (responses == null || responses.Count == 0)
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

        if (string.IsNullOrWhiteSpace(currentDialogue.endLine))
        {
            StartCoroutine(FadeOutAndEndDialogue());
            return;
        }

        npcLineQueue.Enqueue(currentDialogue.endLine);

        state = DialogueState.NPCResponding;
        isEndingDialogue = true;

        SetSpeakerName(currentNPCName);
        typewriter.TypeText(npcLineQueue.Dequeue());
    }

    private void StartNPCResponse(List<string> responses)
    {
        ClearChoices();
        npcLineQueue.Clear();

        foreach (var line in responses)
        {
            if (!string.IsNullOrWhiteSpace(line))
                npcLineQueue.Enqueue(line);
        }

        if (npcLineQueue.Count == 0)
        {
            TriggerEndDialogue();
            return;
        }

        state = DialogueState.NPCResponding;
        SetSpeakerName(currentNPCName);
        typewriter.TypeText(npcLineQueue.Dequeue());
    }

    private void ClearChoices()
    {
        StopAllCoroutines();
        
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);
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
        currentNPCName = null;
        isEndingDialogue = false;
        IsDialogueActive = false;
        
        // Reset canvas group alpha for next dialogue
        dialogueCanvasGroup.alpha = 1f;
    }

    private void ShowLeaveButtonOnly()
    {
        state = DialogueState.Choosing;
        SetSpeakerName("You");
        ClearChoices();
        CreateLeaveButton();
    }

    private void HandleLeaveSelected()
    {
        LevelSelector selector = LevelSelector.GetInstance();
        if (selector != null)
        {
            LevelSO current = selector.GetCurrentLevel();
            LevelCompletionParams completionParams = LevelCompletionParams.FromTime(
                LevelTimer.GetInstance().ElapsedTime,
                current != null ? current.targetTimeSeconds : 0f
            );
            selector.CompleteLevel(completionParams);
        }

        StartCoroutine(FadeOutAndEndDialogue());
    }
}
