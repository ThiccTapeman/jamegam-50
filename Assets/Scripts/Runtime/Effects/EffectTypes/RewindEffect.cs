using ThiccTapeman.Player.Movement;
using ThiccTapeman.Timeline;
using UnityEngine;

namespace ThiccTapeman.Effects
{
    [CreateAssetMenu(menuName = "ThiccTapeman/Items/RewindEffectSO", fileName = "RewindEffectSO")]
    public class RewindEffect : EffectSO
    {
        public override void ApplyEffect()
        {
            // Rewind 1000 seconds
            TimelineManager.GetInstance().Rewind(1000f);
        }
    }
}