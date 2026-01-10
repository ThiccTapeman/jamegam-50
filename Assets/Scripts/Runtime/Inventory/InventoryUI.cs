using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThiccTapeman.Inventory
{


    public class InventoryUI : MonoBehaviour
    {
        private InventoryManager inventoryManager;

        [SerializeField] private GameObject slotPrefab;

        [SerializeField] private SlotUI[] slotUIs;

        private void Start()
        {
            inventoryManager = InventoryManager.GetInstance();

            inventoryManager.OnInventoryChanged += UpdateUI;
            inventoryManager.OnCurrentSlotChanged += UpdateUI;

            InitUI();
        }

        private void OnEnable()
        {
            if (inventoryManager == null) return;

            inventoryManager.OnInventoryChanged += UpdateUI;
            inventoryManager.OnCurrentSlotChanged += UpdateUI;
        }
        private void OnDisable()
        {
            if (inventoryManager == null) return;

            inventoryManager.OnInventoryChanged -= UpdateUI;
            inventoryManager.OnCurrentSlotChanged -= UpdateUI;
        }
        private void OnDestroy()
        {
            if (inventoryManager == null) return;

            inventoryManager.OnInventoryChanged -= UpdateUI;
            inventoryManager.OnCurrentSlotChanged -= UpdateUI;
        }


        private void InitUI()
        {
            if (inventoryManager == null) return;

            Debug.Log("Initializing Inventory UI with " + inventoryManager.slots.Count + " slots.");

            slotUIs = new SlotUI[inventoryManager.slots.Count];

            foreach (var slot in inventoryManager.slots)
            {
                int index = inventoryManager.slots.IndexOf(slot);

                GameObject slotGameObject = Instantiate(slotPrefab, transform);
                slotGameObject.name = "Slot_" + index;
                SlotUI slotUI = slotGameObject.GetComponent<SlotUI>();
                slotUI.SetSlot(slot);

                slotUIs[index] = slotUI;
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            // Update the inventory UI based on the current state of the inventory
            for (int i = 0; i < slotUIs.Length; i++)
            {
                slotUIs[i].UpdateUI();
                slotUIs[i].selectedImage.enabled = (i == inventoryManager.currentSlotIndex);

            }
        }


    }
}