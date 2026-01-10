using UnityEngine;

public class ConfessionBooth : InteractableObject
{
    [SerializeField] private NPCDialogueData dialogue;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bellSound;

    private void Start()
    {
        // Ensure audio source exists
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public override void Interact(GameObject interactor)
    {
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    public void PlayBellSound()
    {
        if (audioSource != null && bellSound != null)
        {
            audioSource.PlayOneShot(bellSound);
        }
    }
}