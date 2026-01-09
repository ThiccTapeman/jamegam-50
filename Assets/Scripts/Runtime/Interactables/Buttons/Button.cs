using UnityEngine;
using System;

public abstract class Button : MonoBehaviour
{
    [SerializeField] private AudioSource buttonAudioSource;
    [SerializeField] private AudioClip buttonPressClip;
    public Action<bool> OnButtonStateChanged;

    [SerializeField] private bool isPressed = false;

    private void Awake()
    {
        OnButtonStateChanged += HandleButtonStateChanged;
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