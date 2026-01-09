using UnityEngine;

public class StepButton : Button
{
    int stepCount = 0;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            stepCount++;
            if (stepCount == 1)
            {
                OnButtonStateChanged?.Invoke(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            stepCount--;
            if (stepCount == 0)
            {
                OnButtonStateChanged?.Invoke(false);
            }
        }
    }
}