using UnityEngine;

[RequireComponent(typeof(Animator))]
public class StepButton : Button
{
    int stepCount = 0;
    private Animator anim;
    private void Awake()
    {
        anim = GetComponent<Animator>();
        anim.SetBool("isPressed", isPressed);
    }
    private void OnTriggerEnter2D(Collider2D other)

    {
        if ((other.CompareTag("Player") && !other.isTrigger) || other.CompareTag("PlayerGhost"))
        {
            stepCount++;
            if (stepCount == 1)
            {
                OnButtonStateChanged?.Invoke(true);
                anim.SetBool("isPressed", true);
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
                anim.SetBool("isPressed", false);
            }
        }
    }
}
