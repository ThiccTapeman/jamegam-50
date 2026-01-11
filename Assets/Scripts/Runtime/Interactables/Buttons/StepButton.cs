using UnityEngine;

public class StepButton : Button
{
    int stepCount = 0;
    public AudioSource upSource;
    public AudioSource downSource;
    [SerializeField] private SoundManager.Sound downSound;
    [SerializeField] private SoundManager.Sound upSound;
    private void OnTriggerEnter2D(Collider2D other)
    
    {
        if ((other.CompareTag("Player") && !other.isTrigger) || other.CompareTag("PlayerGhost"))
        {
            stepCount++;
            if (stepCount == 1)
            {
                OnButtonStateChanged?.Invoke(true);
                downSound.PlaySound(downSource);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if ((other.CompareTag("Player") && !other.isTrigger) || other.CompareTag("PlayerGhost"))
        {
            stepCount--;
            if (stepCount < 0) stepCount = 0;
            if (stepCount == 0)
            {
                OnButtonStateChanged?.Invoke(false);
                upSound.PlaySound(upSource);
            }
        }
    }
}
