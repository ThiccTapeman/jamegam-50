using ThiccTapeman.Timeline;
using UnityEngine;

namespace ThiccTapeman.Effects
{
    [CreateAssetMenu(menuName = "ThiccTapeman/Items/FreezeEffectSO", fileName = "FreezeEffectSO")]
    public class FreezeEffect : EffectSO
    {
        public override void ApplyEffect()
        {
            TimelineManager.GetInstance().PauseTime();
        }

        public override void OnEffectEnd()
        {
            TimelineManager.GetInstance().ResumeTime();
        }
    }
}
