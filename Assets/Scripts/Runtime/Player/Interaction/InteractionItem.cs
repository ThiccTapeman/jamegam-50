using System.Collections.Generic;
using ThiccTapeman.Input;
using UnityEngine;

public sealed class InteractionItem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private string actionMap = "Player";
    [SerializeField] private string interactAction = "Interact";
    [SerializeField] private float interactCooldown = 0.1f;

    [Header("Inventory Fallback")]
    [SerializeField] private bool disableInventoryInput = true;
    [SerializeField] private bool useInventoryWhenNoInteractable = true;

    [Header("Prompt")]
    [SerializeField] private GameObject promptPrefab;

    private InputManager inputManager;
    private InputItem interactInput;
    private InventoryManager inventoryManager;

    private readonly List<InteractableObject> nearby = new List<InteractableObject>();
    private float lastInteractTime = -1f;
    private GameObject promptInstance;

    private void Start()
    {
        inputManager = InputManager.GetInstance();
        interactInput = inputManager.GetAction(actionMap, interactAction);

        inventoryManager = InventoryManager.GetInstance();
        if (disableInventoryInput && inventoryManager != null)
            inventoryManager.SetHandleInput(false);
    }

    private void Update()
    {
        if (interactInput == null) return;
        if (interactInput.ReadValue<float>() <= 0.5f) return;
        if (Time.time - lastInteractTime < interactCooldown && lastInteractTime != -1f) return;

        lastInteractTime = Time.time;

        var target = GetClosestInteractable();
        UpdatePrompt(target);
        if (target != null)
        {
            target.Interact(gameObject);
            return;
        }

        if (useInventoryWhenNoInteractable && inventoryManager != null)
            inventoryManager.TryUseCurrentItem();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return;

        var interactable = collision.GetComponentInParent<InteractableObject>();
        if (interactable == null) return;
        if (nearby.Contains(interactable)) return;

        nearby.Add(interactable);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision == null) return;

        var interactable = collision.GetComponentInParent<InteractableObject>();
        if (interactable == null) return;

        nearby.Remove(interactable);
    }

    private void LateUpdate()
    {
        if (promptPrefab == null) return;

        var target = GetClosestInteractable();
        UpdatePrompt(target);
    }

    private void OnDisable()
    {
        nearby.Clear();
        SetPromptActive(false);
    }

    private InteractableObject GetClosestInteractable()
    {
        InteractableObject best = null;
        float bestDist = float.MaxValue;

        for (int i = nearby.Count - 1; i >= 0; i--)
        {
            var candidate = nearby[i];
            if (candidate == null)
            {
                nearby.RemoveAt(i);
                continue;
            }

            float dist = Vector2.Distance(transform.position, candidate.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        return best;
    }

    private void UpdatePrompt(InteractableObject target)
    {
        if (promptPrefab == null) return;

        if (target == null)
        {
            SetPromptActive(false);
            return;
        }

        if (promptInstance == null)
        {
            promptInstance = Instantiate(promptPrefab);
        }

        promptInstance.transform.position = target.transform.position + target.promptOffset;
        SetPromptActive(true);
    }

    private void SetPromptActive(bool active)
    {
        if (promptInstance == null) return;
        if (promptInstance.activeSelf == active) return;
        promptInstance.SetActive(active);
    }
}
