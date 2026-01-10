using System.Diagnostics;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ThiccTapeman.Effects
{
    public class EffectManager : MonoBehaviour
    {
        private static EffectManager instance;

        private List<Effect> activeEffects = new List<Effect>();



        public static EffectManager GetInstance()
        {
            if (instance == null)
            {
                GameObject effectManagerObject = new GameObject("EffectManager");
                instance = effectManagerObject.AddComponent<EffectManager>();
            }
            return instance;
        }

        public bool TryApplyEffect(EffectSO effectSO)
        {
            // No duplicate active effects allowed
            if (effectSO == null) return false;
            if (activeEffects.Exists(e => e.effectSO == effectSO))
            {
                var effect = activeEffects.Find(e => e.effectSO == effectSO);
                if (effect.isActive)
                {
                    return false;
                }

                effect.TryApplyEffect();
                return true;
            }

            // Effect didnt already exist, create new one
            Effect newEffect = new Effect(effectSO);
            bool applied = newEffect.TryApplyEffect();
            activeEffects.Add(newEffect);

            return applied;
        }

        public List<Effect> GetActiveEffects()
        {
            return activeEffects.Where(e => e.isActive).ToList();
        }

        private void Awake()
        {
            // Ensure singleton instance
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            for (int i = 0; i < activeEffects.Count; i++)
                activeEffects[i].UpdateEffect();
        }

    }
}