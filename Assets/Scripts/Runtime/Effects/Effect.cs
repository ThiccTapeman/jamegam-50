using UnityEngine;

namespace ThiccTapeman.Effects
{
    public class Effect
    {
        public EffectSO effectSO;
        public float startTime = -1;
        public float lastUsed = -1;
        private bool ended;

        public bool isActive
        {
            get
            {
                if (startTime == -1) return false;
                return Time.time < startTime + effectSO.effectDuration;
            }
        }

        public float timeRemaining
        {
            get
            {
                if (!isActive) return 0;
                return (startTime + effectSO.effectDuration) - Time.time;
            }
        }

        public Effect(EffectSO effectSO)
        {
            this.effectSO = effectSO;
        }

        public bool TryApplyEffect()
        {
            if (isActive) return false;

            if (lastUsed == -1 || Time.time - lastUsed > effectSO.itemUseCooldown)
            {
                effectSO.ApplyEffect();
                lastUsed = Time.time;
                startTime = Time.time;
                ended = false;
                return true;
            }
            return false;
        }

        public void UpdateEffect()
        {
            if (isActive) return;

            if (!ended && startTime != -1)
            {
                ended = true;
                effectSO.OnEffectEnd();
            }
        }
    }
}
