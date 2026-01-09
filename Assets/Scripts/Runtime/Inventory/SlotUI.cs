using UnityEngine;
using UnityEngine.UI;

namespace ThiccTapeman.Inventory
{
    public class SlotUI : MonoBehaviour
    {
        public Image selectedImage;
        public Image iconImage;
        public TMPro.TMP_Text amountText;

        private Slot slot;

        public void SetSlot(Slot slot)
        {
            this.slot = slot;
            UpdateUI();
        }

        public void UpdateUI()
        {
            if (slot.itemSO != null)
            {
                iconImage.sprite = slot.itemSO.icon;
                iconImage.enabled = true;
                amountText.text = slot.Amount > 1 ? slot.Amount.ToString() : "";
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
                amountText.text = "";
            }
        }
    }
}