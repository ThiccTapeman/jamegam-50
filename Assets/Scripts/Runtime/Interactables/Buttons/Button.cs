using UnityEngine;
using System;
using ThiccTapeman.Player.Reset;

public abstract class Button : MonoBehaviour
{
    [SerializeField] private AudioSource buttonAudioSource;
    [SerializeField] private AudioClip buttonPressClip;
    public Action<bool> OnButtonStateChanged;

    [SerializeField] private bool isPressed = false;

    private ResetManager resetManager;
    private bool isPressedByDefault = false;
    private void Awake()
    {
        OnButtonStateChanged += HandleButtonStateChanged;
        isPressedByDefault = isPressed;
        resetManager = ResetManager.GetInstance();
        if (resetManager != null)
            resetManager.OnReset += OnReset;
    }
    private void OnReset()
    {
        // Reset button to released state
        isPressed = isPressedByDefault;
    }

    private void HandleButtonStateChanged(bool pressed)
    {
        if (buttonAudioSource != null && buttonPressClip != null)
        {
            buttonAudioSource.PlayOneShot(buttonPressClip);
        }

        if (pressed)
        {
            Debug.Log("Button Pressed");
        }
        else
        {
            Debug.Log("Button Released");
        }
    }

}