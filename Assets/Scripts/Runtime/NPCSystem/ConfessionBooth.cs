using UnityEngine;

public class ConfessionBooth : MonoBehaviour
{
    [SerializeField] private NPCDialogueData dialogue;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bellSound;

    private bool playerInRange;

    private void Awake()
    {
        // Ensure audio source exists
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            DialogueManager.Instance.StartDialogue(dialogue);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            playerInRange = false;
    }

    public void PlayBellSound()
    {
        if (audioSource != null && bellSound != null)
        {
            audioSource.PlayOneShot(bellSound);
        }
    }
}