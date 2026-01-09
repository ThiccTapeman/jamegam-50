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

        [Header("Input")]
        public string dashActionName = "Dash";
        public string moveActionName = "Move";

        private InputItem dashAction;
        private InputItem moveAction;

        private float lastDashTime = -Mathf.Infinity;
        private bool dashQueued;
        private Vector2 queuedDir = Vector2.right;

        public override void AwakeAbility(InputManager inputManager, Rigidbody2D rb)
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
        }

        public override void UpdateAbility()
        {
            if (rb == null || dashAction == null || moveAction == null) return;

            if (!dashAction.GetTriggered(true)) return;
            if (Time.time - lastDashTime < dashCooldown) return;

            Vector2 input = moveAction.ReadValue<Vector2>();
            queuedDir = input.sqrMagnitude > 0.01f ? input.normalized : Vector2.right;
            dashQueued = true;
        }

        public override void FixedUpdateAbility()
        {
            if (!dashQueued || rb == null) return;

            // Keep current vertical velocity, dash horizontally/diagonally
            Vector2 v = rb.linearVelocity;

            // Guarantee a dash speed (consistent feel)
            Vector2 dashVel = queuedDir * minDashSpeed;
            rb.linearVelocity = new Vector2(dashVel.x, v.y);

            // Add impulse on top (nice punch)
            rb.AddForce(queuedDir * dashImpulse, ForceMode2D.Impulse);

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
    }
}
