using UnityEngine;
using ThiccTapeman.Input;

namespace ThiccTapeman.Player.Movement
{
    public abstract class PlayerMovementAbility : ScriptableObject
    {
        [HideInInspector] public Rigidbody2D rb;
        [HideInInspector] public InputManager inputManager;

        public bool lockOtherAbilitiesDuringUse = false;

        public abstract void AwakeAbility(InputManager inputManager, Rigidbody2D rb, Animator animator);
        public abstract void DrawGizmos(Rigidbody2D rb, Collider2D col);

        // Input / non-physics
        public virtual void UpdateAbility() { }

        // Physics
        public virtual void FixedUpdateAbility() { }
    }
}
