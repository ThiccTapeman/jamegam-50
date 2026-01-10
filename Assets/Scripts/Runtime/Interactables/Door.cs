using System;
using ThiccTapeman.Player.Reset;
using Unity.VisualScripting;
using UnityEngine;

public class Door : MonoBehaviour
{
    public enum DoorType
    {
        Sliding, Hinged
    }

    [Header("State")]
    [SerializeField] private bool isOpen = false;

    [Header("Input")]
    [SerializeField] private Button openButton;

    [Header("Sliding")]
    [SerializeField] private float slideSpeed = 1.5f;     // units per second
    [SerializeField] private float slideDistance = 2.0f;  // units
    [SerializeField] private bool useLocalSpace = true;
    [SerializeField] private Vector3 slideDirection = Vector3.up; // direction to open
    [SerializeField] private AudioSource openingSource;
    [SerializeField] private SoundManager.Sound openingSound;

    private Vector3 closedPos;
    private Vector3 openPos;

    private ResetManager resetManager;
    private bool isOpenByDefault = false;

    private void HandleButtonStateChanged(bool pressed)
    {
        isOpen = pressed;
        Debug.Log("Door is now " + (isOpen ? "Open" : "Closed"));
    }

    void Start()
    {
        isOpenByDefault = isOpen;
        // Cache positions
        if (useLocalSpace)
        {
            closedPos = transform.localPosition;
            openPos = closedPos + slideDirection.normalized * slideDistance;
        }
        else
        {
            closedPos = transform.position;
            openPos = closedPos + slideDirection.normalized * slideDistance;
        }

        if (openButton != null)
            openButton.OnButtonStateChanged += HandleButtonStateChanged;


        resetManager = ResetManager.GetInstance();
        if (resetManager != null)
            resetManager.OnReset += OnReset;
    }
    private void OnReset()
    {
        // Reset door to closed state
        isOpen = isOpenByDefault;
    }
    void Update()
    {
        Vector3 target = isOpen ? openPos : closedPos;
        bool isMoving = Vector3.Distance(useLocalSpace ? transform.localPosition : transform.position, target) > 0.001f;

        if (openingSource != null)
        {
            if (isOpen && isMoving)
            {
                if (openingSound != null && !openingSource.isPlaying)
                    SoundManager.PlaySound(openingSound, openingSource);
            }
            else if (openingSource.isPlaying)
            {
                openingSource.Stop();
            }
        }

        if (useLocalSpace)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                target,
                slideSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                slideSpeed * Time.deltaTime
            );
        }
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.OnButtonStateChanged -= HandleButtonStateChanged;
    }
}
