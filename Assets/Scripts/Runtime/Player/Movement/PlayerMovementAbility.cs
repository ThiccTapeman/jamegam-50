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

        // Animation-only tick when movement is locked.
        public virtual void FixedUpdateAnimationOnly() { }

        protected AudioSource GetOrCreateAudioSource(int index)
        {
            if (rb == null) return null;

            var sources = rb.GetComponents<AudioSource>();
            if (index >= 0 && index < sources.Length)
                return sources[index];

            AudioSource created = null;
            for (int i = sources.Length; i <= index; i++)
            {
                created = rb.gameObject.AddComponent<AudioSource>();
                created.playOnAwake = false;
            }

            return created;
        }
    }
}
