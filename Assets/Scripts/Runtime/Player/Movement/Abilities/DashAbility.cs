using UnityEngine;
using ThiccTapeman.Input;

namespace ThiccTapeman.Player.Movement
{
    [CreateAssetMenu(fileName = "DashAbility", menuName = "ThiccTapeman/Player/Movement/Abilities/DashAbility")]
    public class DashAbility : PlayerMovementAbility
    {
        [Header("Dash")]
        public float dashImpulse = 12f;          // impulse amount (tune)
        public float minDashSpeed = 8f;          // guarantees movement even with high mass/drag
        public float dashCooldown = 0.35f;
        public float dashLockSeconds = 0.12f;

        [SerializeField] private SoundManager.SoundVariations dashes;
        [SerializeField] private int dashAudioSourceIndex = 1;

        [Header("Input")]
        public string dashActionName = "Dash";
        public string moveActionName = "Move";

        private InputItem dashAction;
        private InputItem moveAction;

        private float lastDashTime = -Mathf.Infinity;
        private bool dashQueued;
        private Vector2 queuedDir = Vector2.right;
        private float dashLockUntil = -Mathf.Infinity;
        private float cachedGravityScale = 1f;
        private bool isDashLocked;
        private Vector2 lastMoveDir = Vector2.right;

        private Animator anim;
        private SpriteRenderer sr;
        private AudioSource dashSource;

        public override void AwakeAbility(InputManager inputManager, Rigidbody2D rb, Animator animator)
        {
            this.inputManager = inputManager;
            this.rb = rb;

            // Get input actions
            dashAction = inputManager.GetAction("Player", dashActionName);
            moveAction = inputManager.GetAction("Player", moveActionName);

            if (dashAction == null)
            {
                Debug.LogError($"DashAbility: Dash action '{dashActionName}' not found in InputManager.");
            }

            if (moveAction == null)
            {
                Debug.LogError($"DashAbility: Move action '{moveActionName}' not found in InputManager.");
            }

            // Initialize state
            dashQueued = false;
            lastDashTime = 0;

            this.anim = animator;
            sr = anim != null ? anim.GetComponentInChildren<SpriteRenderer>() : null;
            dashSource = GetOrCreateAudioSource(dashAudioSourceIndex);
        }

        public override void UpdateAbility()
        {
            if (rb == null || dashAction == null || moveAction == null) return;

            Vector2 input = moveAction.ReadValue<Vector2>();
            if (input.sqrMagnitude > 0.01f)
                lastMoveDir = input.normalized;

            if (!dashAction.GetTriggered(true)) return;
            if (Time.time - lastDashTime < dashCooldown) return;

            queuedDir = input.sqrMagnitude > 0.01f ? input.normalized : GetDefaultDashDirection();
            dashQueued = true;
        }

        public override void FixedUpdateAbility()
        {
            if (rb == null) return;

            if (isDashLocked)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                if (Time.time >= dashLockUntil)
                {
                    rb.gravityScale = cachedGravityScale;
                    isDashLocked = false;
                }
            }

            if (!dashQueued) return;

            // Keep current vertical velocity, dash horizontally/diagonally
            Vector2 v = rb.linearVelocity;

            // Guarantee a dash speed (consistent feel)
            Vector2 dashVel = queuedDir * minDashSpeed;
            rb.linearVelocity = new Vector2(dashVel.x, 0f);

            // Add impulse on top (nice punch)
            rb.AddForce(queuedDir * dashImpulse, ForceMode2D.Impulse);

            anim?.SetTrigger("Dash");
            if (sr != null && Mathf.Abs(queuedDir.x) > 0.05f)
                sr.flipX = queuedDir.x < 0f;

            if (dashes != null && dashSource != null)
                SoundManager.PlaySound(dashes, dashSource);

            if (dashLockSeconds > 0f)
            {
                cachedGravityScale = rb.gravityScale;
                rb.gravityScale = 0f;
                isDashLocked = true;
                dashLockUntil = Time.time + dashLockSeconds;
            }

            lastDashTime = Time.time;
            dashQueued = false;
        }

        public override void DrawGizmos(Rigidbody2D rbRef, Collider2D colRef)
        {
            if (rbRef == null) return;
            Gizmos.color = Color.red;
            Vector3 o = rbRef.position;
            Gizmos.DrawLine(o, o + (Vector3)(queuedDir.normalized * 1.2f));
        }

        private Vector2 GetDefaultDashDirection()
        {
            if (lastMoveDir.sqrMagnitude > 0.01f)
                return lastMoveDir;

            if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.01f)
                return new Vector2(Mathf.Sign(rb.linearVelocity.x), 0f);

            if (sr != null)
                return sr.flipX ? Vector2.left : Vector2.right;

            return Vector2.right;
        }
    }
}
