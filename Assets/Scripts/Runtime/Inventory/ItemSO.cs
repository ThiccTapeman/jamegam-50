using UnityEngine;

namespace ThiccTapeman.Inventory
{
    public abstract class ItemSO : ScriptableObject
    {
        public string itemName;
        public string description;
        public int maxStackSize;
        public float itemUseCooldown;
        public Sprite icon;
        public Sprite image;
        public SoundManager.Sound sound;

    }
}