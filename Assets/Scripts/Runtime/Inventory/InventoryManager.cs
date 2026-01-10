using System;
using System.Collections.Generic;
using ThiccTapeman.Input;
using ThiccTapeman.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

//
//this class with Slot.cs handles the input (old input system now for testing), keeping fruits and display the remaining amount of them
//

public class InventoryManager : MonoBehaviour
{
    private static InventoryManager instance;

    public Action OnInventoryChanged;
    public Action OnCurrentSlotChanged;

    public static InventoryManager GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<InventoryManager>();
            if (instance == null)
            {
                GameObject obj = new GameObject("InventoryManager");
                instance = obj.AddComponent<InventoryManager>();
            }
        }
        return instance;
    }

    [Header("Inventory Settings")]
    public List<Slot> slots = new List<Slot>();
    [SerializeField] private int maxSlots = 5;
    [SerializeField] private float useCooldown = 1f;
    [SerializeField] public bool handleUseInput = true;

    // Privates
    private InputManager inputManager;
    private InputItem useAction;
    private InputItem scrollAction;
    private InputItem[] slotActions;

    public int currentSlotIndex = 0;

    private float lastUseTime = -1f;

    private void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        inputManager = InputManager.GetInstance();

        Debug.Log("Setting up InventoryManager with max slots: " + maxSlots);
        Debug.Log("Setting up InventoryManager with slots: " + slots.Count);

        useAction = inputManager.GetAction("Player", "Interact");
        scrollAction = inputManager.GetAction("Player", "Scroll");

        slotActions = new InputItem[9];
        for (int i = 0; i < slotActions.Length; i++)
        {
            slotActions[i] = inputManager.GetAction("Player", $"Slot{i + 1}");
        }
    }

    private void Update()
    {
        if (handleUseInput)
            HandleUseInput();

        HandleScrollInput();
        HandleSlotHotkeys();
    }

    private void HandleUseInput()
    {
        if (useAction == null) return;
        if (useAction.ReadValue<float>() <= 0.5f) return;

        TryUseCurrentItem();
    }

    private void HandleScrollInput()
    {
        if (scrollAction == null) return;
        float scrollValue = scrollAction.ReadValue<float>();
        if (scrollValue != 0)
        {
            if (slots.Count == 0) return;

            if (scrollValue > 0)
            {
                currentSlotIndex = (currentSlotIndex + 1) % slots.Count;
                OnCurrentSlotChanged?.Invoke();
                Debug.Log($"Switched to slot {currentSlotIndex} with a: {slots[currentSlotIndex].itemSO?.itemName ?? "Empty"}");
            }
            else if (scrollValue < 0)
            {
                currentSlotIndex = (currentSlotIndex - 1 + slots.Count) % slots.Count;
                OnCurrentSlotChanged?.Invoke();
                Debug.Log($"Switched to slot {currentSlotIndex} with a: {slots[currentSlotIndex].itemSO?.itemName ?? "Empty"}");
            }

        }
    }

    private void HandleSlotHotkeys()
    {
        if (slotActions == null || slots.Count == 0) return;

        int max = Mathf.Min(slots.Count, slotActions.Length);
        for (int i = 0; i < max; i++)
        {
            var action = slotActions[i];
            if (action == null) continue;
            if (!action.GetTriggered(true)) continue;

            currentSlotIndex = i;
            OnCurrentSlotChanged?.Invoke();
            Debug.Log($"Switched to slot {currentSlotIndex} with a: {slots[currentSlotIndex].itemSO?.itemName ?? "Empty"}");
            break;
        }
    }

    private bool UseCurrentItem()
    {
        if (slots.Count == 0) return false;

        Slot currentSlot = slots[currentSlotIndex];
        bool used = currentSlot.TryUse();

        if (used)
        {
            OnInventoryChanged?.Invoke();
            Debug.Log($"Used item in slot {currentSlotIndex}. Remaining amount: {currentSlot.Amount}");
        }
        else
        {
            Debug.Log($"Item in slot {currentSlotIndex} is on cooldown or out of stock.");
        }

        return used;
    }

    public bool TryUseCurrentItem()
    {
        if (Time.time - lastUseTime < useCooldown && lastUseTime != -1) return false;

        lastUseTime = Time.time;
        return UseCurrentItem();
    }

    public void SetHandleInput(bool enabled)
    {
        handleUseInput = enabled;
    }


    public void SetInventory(Slot[] newSlots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i > newSlots.Length - 1)
            {
                slots[i].itemSO = null;
                slots[i].Amount = 0;
                continue;
            }
            slots[i].itemSO = newSlots[i].itemSO;
            slots[i].Amount = newSlots[i].Amount;
        }
        OnInventoryChanged?.Invoke();

    }


    public void AddItem(ItemSO itemSO, int amount)
    {
        // Check if item already exists in a slot
        foreach (var slot in slots)
        {
            if (slot.itemSO == itemSO)
            {
                slot.Amount += amount;
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        // Find an empty slot
        foreach (var slot in slots)
        {
            if (slot.itemSO == null)
            {
                slot.itemSO = itemSO;
                slot.Amount = amount;
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        Debug.LogWarning("No empty slots available to add the item.");
    }

    public void RemoveItem(ItemSO itemSO, int amount)
    {
        foreach (var slot in slots)
        {
            if (slot.itemSO.itemName == itemSO.itemName)
            {
                slot.Amount -= amount;
                if (slot.Amount <= 0)
                {
                    slot.itemSO = null;
                    slot.Amount = 0;
                }
                OnInventoryChanged?.Invoke();
                return;
            }
        }
        Debug.LogWarning("Item not found in inventory.");
    }
}
