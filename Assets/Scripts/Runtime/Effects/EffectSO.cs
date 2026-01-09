using ThiccTapeman.Inventory;

namespace ThiccTapeman.Effects
{
    public abstract class EffectSO : ItemSO
    {
        public float effectDuration;

        public abstract void ApplyEffect();

        public virtual void OnEffectEnd() { }
    }
}
