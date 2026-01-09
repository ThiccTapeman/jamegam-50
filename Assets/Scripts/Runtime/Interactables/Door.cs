using System;
using Unity.VisualScripting;
using UnityEditor.UI;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private bool isOpen = false;
    [SerializeField] private Button openButton;

    private void HandleButtonStateChanged(bool pressed)
    {
        isOpen = pressed;
        // Here you would add the logic to animate the door opening/closing
        Debug.Log("Door is now " + (isOpen ? "Open" : "Closed"));
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        openButton.OnButtonStateChanged += HandleButtonStateChanged;
    }

    // Update is called once per frame
    void Update()
    {

    }
}



