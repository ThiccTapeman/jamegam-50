using UnityEngine;

namespace ThiccTapeman.Timeline
{
    public sealed class BranchColliderState : MonoBehaviour
    {
        public bool[] originalIsTrigger;

        public void Capture(Collider2D[] colliders)
        {
            if (colliders == null) return;
            originalIsTrigger = new bool[colliders.Length];

            for (int i = 0; i < colliders.Length; i++)
            {
                originalIsTrigger[i] = colliders[i] != null && colliders[i].isTrigger;
            }
        }
    }
}
