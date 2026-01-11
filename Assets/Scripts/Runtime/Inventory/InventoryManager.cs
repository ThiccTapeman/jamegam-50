using System;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using ThiccTapeman.Input;
using ThiccTapeman.Inventory;
using UnityEngine;
using UnityEngine.UI;

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
            instance = FindAnyObjectByType<InventoryManager>();
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
    [SerializeField] private int maxSlots = 2;
    [SerializeField] private float useCooldown = 1f;
    [SerializeField] public bool handleUseInput = true;
    [SerializeField] private UnityEngine.UI.Image firstItemImage;
    [SerializeField] private UnityEngine.UI.Image secondItemImage;

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

        // Subscribe to inventory changes to update images
        OnInventoryChanged += UpdateItemImages;
        OnCurrentSlotChanged += UpdateItemImages;
        
        // Initial update
        UpdateItemImages();
    }

    private void OnDestroy()
    {
        OnInventoryChanged -= UpdateItemImages;
        OnCurrentSlotChanged -= UpdateItemImages;
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
            // Swap slot 0 and slot 1 (slots 1 and 2)
            SwapSlots(0, 1);
        }
    }

    private void SwapSlots(int slotIndex1, int slotIndex2)
    {
        if (slots.Count < 2 || slotIndex1 < 0 || slotIndex2 < 0 || 
            slotIndex1 >= slots.Count || slotIndex2 >= slots.Count) 
            return;

        // Swap the items and amounts
        ItemSO tempItemSO = slots[slotIndex1].itemSO;
        int tempAmount = slots[slotIndex1].Amount;

        slots[slotIndex1].itemSO = slots[slotIndex2].itemSO;
        slots[slotIndex1].Amount = slots[slotIndex2].Amount;

        slots[slotIndex2].itemSO = tempItemSO;
        slots[slotIndex2].Amount = tempAmount;

        OnInventoryChanged?.Invoke();
        OnCurrentSlotChanged?.Invoke();
        Debug.Log($"Swapped slot {slotIndex1} and slot {slotIndex2}");
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
            
            // Use the item in the selected slot
            UseItemInSlot(i);
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

    private bool UseItemInSlot(int slotIndex)
    {
        if (slots.Count == 0 || slotIndex < 0 || slotIndex >= slots.Count) return false;
        
        // Check cooldown
        if (Time.time - lastUseTime < useCooldown && lastUseTime != -1) return false;

        Slot slot = slots[slotIndex];
        if (slot.itemSO == null) return false;

        lastUseTime = Time.time;
        bool used = slot.TryUse();
        if (used)
        {
            OnInventoryChanged?.Invoke();
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
        if (itemSO == null) return;
        foreach (var slot in slots)
        {
            if (slot == null || slot.itemSO == null) continue;
            if (slot.itemSO == itemSO)
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

    private void UpdateItemImages()
    {
        // Update first item image (slot 0)
        if (firstItemImage != null)
        {
            if (slots.Count > 0 && slots[0] != null && slots[0].itemSO != null && slots[0].itemSO.image != null)
            {
                firstItemImage.sprite = slots[0].itemSO.image;
                firstItemImage.enabled = true;
            }
            else
            {
                firstItemImage.sprite = null;
                firstItemImage.enabled = false;
            }
        }

        // Update second item image (slot 1)
        if (secondItemImage != null)
        {
            if (slots.Count > 1 && slots[1] != null && slots[1].itemSO != null && slots[1].itemSO.image != null)
            {
                secondItemImage.sprite = slots[1].itemSO.image;
                secondItemImage.enabled = true;
            }
            else
            {
                secondItemImage.sprite = null;
                secondItemImage.enabled = false;
            }
        }
    }
}
