using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThiccTapeman.Inventory;
using ThiccTapeman.Effects;

[Serializable]
public class Slot
{
    public int Amount;
    public ItemSO itemSO;
    public float Cooldown
    {
        get
        {
            if (itemSO != null)
                return itemSO.itemUseCooldown;
            return 0f;
        }
    }

    public bool TryUse()
    {
        if (itemSO == null) return false;

        if (itemSO is EffectSO effectItem)
        {
            if (EffectManager.GetInstance().TryApplyEffect(effectItem))
            {
                InventoryManager.GetInstance().RemoveItem(itemSO, 1);
            }
        }

        return true;
    }
}
