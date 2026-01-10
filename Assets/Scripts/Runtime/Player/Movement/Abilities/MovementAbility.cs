using UnityEngine;
using ThiccTapeman.Input;

namespace ThiccTapeman.Player.Movement
{
    [CreateAssetMenu(fileName = "MovementAbility", menuName = "ThiccTapeman/Player/Movement/Abilities/MovementAbility")]
    public class MovementAbility : PlayerMovementAbility
    {
        [Header("Input Settings")]
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";

        [Header("Movement Settings")]
        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private float jumpSpeed = 7f;

        [Header("Smoothing")]
        [SerializeField] private float groundAccel = 70f;
        [SerializeField] private float groundDecel = 90f;
        [SerializeField] private float airAccel = 45f;
        [SerializeField] private float airDecel = 45f;

        [Header("Ground Check Settings")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private float coyoteTime = 0.2f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Wall Settings")]
        [SerializeField] private string wallSlideTag = "WallSlide";
        [SerializeField] private float wallCheckDistance = 0.2f;
        [SerializeField] private float wallSlideSpeed = 2f;
        [SerializeField] private float wallJumpSpeed = 7f;
        [SerializeField] private float wallCheckDisableTime = 0.2f;

        [Header("Ray / Cast Insets")]
        [SerializeField] private float raycastInset = 0.05f;

        [Header("Wall Cast (robust)")]
        [SerializeField] private float wallCastExtra = 0.02f;
        [SerializeField] private int wallCastHits = 4;

        private InputItem moveAction;
        private InputItem jumpAction;

        private Collider2D playerCollider;
        private int levelMask;

        private float lastGroundedTime = -Mathf.Infinity;
        private bool isGrounded;

        private bool isTouchingWall;
        private int wallDirection; // -1 left, +1 right
        private float wallCheckDisableUntil;
        private bool wallAllowsSlide;
        private bool wallAllowsJump;

        private RaycastHit2D[] castHits;
        private ContactFilter2D levelFilter;

        // --- buffered inputs ---
        private Vector2 cachedMoveInput;
        private float jumpPressedTime = -Mathf.Infinity;

        private Animator anim;
        private SpriteRenderer sr;
        private int facing = 1; // 1 right, -1 left

        public override void AwakeAbility(InputManager inputManager, Rigidbody2D rb, Animator animator)
        {
            this.inputManager = inputManager;
            this.rb = rb;

            moveAction = inputManager.GetAction("Player", moveActionName);
            jumpAction = inputManager.GetAction("Player", jumpActionName);

            playerCollider = rb != null ? rb.GetComponent<Collider2D>() : null;
            levelMask = LayerMask.GetMask("Level");

            castHits = new RaycastHit2D[Mathf.Max(1, wallCastHits)];
            levelFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = levelMask,
                useTriggers = false
            };

            cachedMoveInput = Vector2.zero;
            jumpPressedTime = -Mathf.Infinity;

            this.anim = animator;
            sr = anim != null ? anim.GetComponentInChildren<SpriteRenderer>() : null;
            facing = 1;
        }

        public override void UpdateAbility()
        {
            // Cache input in Update (render rate)
            cachedMoveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

            // Buffer jump press
            if (jumpAction != null && jumpAction.GetTriggered(true))
                jumpPressedTime = Time.time;
        }


        public override void FixedUpdateAbility()
        {
            UpdateEnvironmentChecks();

            ApplyHorizontalMovementSmooth(cachedMoveInput);
            ApplyWallSlideClamp();

            TryConsumeBufferedJump();
            UpdateAnimatorAndFlip();
        }

        /// <summary>
        /// 
        /// </summary>
        private void UpdateAnimatorAndFlip()
        {
            if (anim == null || rb == null) return;

            Vector2 v = rb.linearVelocity;

            // DeltaX: use speed magnitude on X (positive float)
            anim.SetFloat("DeltaX", Mathf.Abs(v.x));

            // DeltaY: signed vertical velocity
            anim.SetFloat("DeltaY", v.y);

            // Ground / wall slide
            anim.SetBool("IsGrounded", isGrounded);
            anim.SetBool("IsWallSliding", !isGrounded && isTouchingWall && wallAllowsSlide);

            // Facing: prefer input, fallback to velocity
            float dir = Mathf.Abs(cachedMoveInput.x) > 0.01f ? cachedMoveInput.x : v.x;
            if (dir > 0.05f) facing = 1;
            else if (dir < -0.05f) facing = -1;

            if (sr != null)
                sr.flipX = (facing == -1);
        }

        /// <summary>
        /// Updates the grounded and wall contact states using raycasts and collider casts.
        /// </summary>
        private void UpdateEnvironmentChecks()
        {
            if (rb == null) return;

            Vector2 position = rb.position;
            Bounds bounds = playerCollider != null ? playerCollider.bounds : new Bounds(position, Vector3.zero);

            Vector2 min = bounds.min;
            Vector2 max = bounds.max;
            min.x += raycastInset; max.x -= raycastInset;
            min.y += raycastInset; max.y -= raycastInset;

            // Ground (2 rays)
            Vector2 groundLeftOrigin = new Vector2(min.x, min.y);
            Vector2 groundRightOrigin = new Vector2(max.x, min.y);

            RaycastHit2D groundLeftHit = Physics2D.Raycast(groundLeftOrigin, Vector2.down, groundCheckDistance, levelMask);
            RaycastHit2D groundRightHit = Physics2D.Raycast(groundRightOrigin, Vector2.down, groundCheckDistance, levelMask);

            if (groundLeftHit.collider != null || groundRightHit.collider != null)
            {
                isGrounded = true;
                lastGroundedTime = Time.time;
            }
            else
            {
                isGrounded = false;
            }

            // Wall
            if (Time.time < wallCheckDisableUntil)
            {
                isTouchingWall = false;
                wallDirection = 0;
                wallAllowsSlide = false;
                wallAllowsJump = false;
                return;
            }

            bool hitL = CastWall(Vector2.left, out RaycastHit2D leftHit);
            bool hitR = CastWall(Vector2.right, out RaycastHit2D rightHit);

            RaycastHit2D wallHit = hitL ? leftHit : hitR ? rightHit : default;

            isTouchingWall = wallHit.collider != null;
            wallDirection = hitL ? -1 : hitR ? 1 : 0;

            wallAllowsSlide = isTouchingWall && wallHit.collider.CompareTag(wallSlideTag);
            wallAllowsJump = wallAllowsSlide;
        }

        /// <summary>
        /// Casts the player's collider shape in the given direction to check for walls.
        /// </summary>
        /// <returns>If a wall was hit</returns>
        private bool CastWall(Vector2 dir, out RaycastHit2D bestHit)
        {
            bestHit = default;
            if (playerCollider == null) return false;

            int hitCount = playerCollider.Cast(dir, levelFilter, castHits, wallCheckDistance + wallCastExtra);

            float bestDist = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D h = castHits[i];
                if (h.collider == null) continue;

                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    bestHit = h;
                }
            }

            return bestHit.collider != null;
        }

        /// <summary>
        /// Applies horizontal movement with smoothing based on ground/air acceleration and deceleration.
        /// </summary>
        /// <param name="moveInput">The current movement input that is being applied</param>
        private void ApplyHorizontalMovementSmooth(Vector2 moveInput)
        {
            if (rb == null) return;

            float dt = Time.fixedDeltaTime;

            float targetX = moveInput.x * movementSpeed;

            // If pressing INTO wall in air, stop targeting into it (prevents gravity fighting)
            if (!isGrounded && isTouchingWall && wallDirection != 0)
            {
                if (Mathf.Sign(targetX) == wallDirection && Mathf.Abs(targetX) > 0.001f)
                    targetX = 0f;
            }

            Vector2 v = rb.linearVelocity;

            bool hasInput = Mathf.Abs(moveInput.x) > 0.01f;
            float accel = isGrounded
                ? (hasInput ? groundAccel : groundDecel)
                : (hasInput ? airAccel : airDecel);

            v.x = Mathf.MoveTowards(v.x, targetX, accel * dt);

            rb.linearVelocity = v;
        }

        /// <summary>
        /// Applies wall slide clamping to vertical velocity when touching a wall.
        /// </summary>
        private void ApplyWallSlideClamp()
        {
            if (rb == null) return;
            if (!isTouchingWall || isGrounded || !wallAllowsSlide) return;

            Vector2 v = rb.linearVelocity;

            if (v.y < -wallSlideSpeed)
                v.y = Mathf.MoveTowards(v.y, -wallSlideSpeed, 60f * Time.fixedDeltaTime);

            rb.linearVelocity = v;
        }

        /// <summary>
        /// Consumes a buffered jump input if conditions allow (grounded or wall jump).
        /// </summary>
        private void TryConsumeBufferedJump()
        {
            if (rb == null) return;

            // no buffered jump
            if (Time.time - jumpPressedTime > jumpBufferTime)
                return;

            // Wall jump
            if (isTouchingWall && !isGrounded && wallDirection != 0 && wallAllowsJump)
            {
                Vector2 dir = new Vector2(-wallDirection, 1f).normalized;
                rb.linearVelocity = dir * wallJumpSpeed;

                anim?.SetTrigger("Jump"); // Trigger animator

                wallCheckDisableUntil = Time.time + wallCheckDisableTime;
                jumpPressedTime = -Mathf.Infinity;
                return;
            }

            // Coyote / grounded jump
            if (Time.time - lastGroundedTime <= coyoteTime)
            {
                Vector2 v = rb.linearVelocity;
                v.y = jumpSpeed;
                rb.linearVelocity = v;

                anim?.SetTrigger("Jump"); // Trigger animator

                wallCheckDisableUntil = Time.time + wallCheckDisableTime;
                jumpPressedTime = -Mathf.Infinity; // consume
            }
        }


        public override void DrawGizmos(Rigidbody2D rbRef, Collider2D colRef)
        {
            if (rbRef == null) return;

            Gizmos.color = Color.yellow;

            Vector2 position = rbRef.position;
            Bounds bounds = colRef != null ? colRef.bounds : new Bounds(position, Vector3.zero);

            Vector2 min = bounds.min;
            Vector2 max = bounds.max;
            min.x += raycastInset; max.x -= raycastInset;
            min.y += raycastInset; max.y -= raycastInset;

            // Ground rays (same as UpdateEnvironmentChecks)
            Vector2 groundLeftOrigin = new Vector2(min.x, min.y);
            Vector2 groundRightOrigin = new Vector2(max.x, min.y);

            Gizmos.DrawLine(groundLeftOrigin, groundLeftOrigin + Vector2.down * groundCheckDistance);
            Gizmos.DrawLine(groundRightOrigin, groundRightOrigin + Vector2.down * groundCheckDistance);

            // Wall "distance" visualization for Cast (we can't draw the cast shape, but we can draw edge lines)
            float wallDist = wallCheckDistance + wallCastExtra;

            Vector2 leftLowerOrigin = new Vector2(min.x, min.y);
            Vector2 leftUpperOrigin = new Vector2(min.x, max.y);
            Vector2 rightLowerOrigin = new Vector2(max.x, min.y);
            Vector2 rightUpperOrigin = new Vector2(max.x, max.y);

            Gizmos.DrawLine(leftLowerOrigin, leftLowerOrigin + Vector2.left * wallDist);
            Gizmos.DrawLine(leftUpperOrigin, leftUpperOrigin + Vector2.left * wallDist);
            Gizmos.DrawLine(rightLowerOrigin, rightLowerOrigin + Vector2.right * wallDist);
            Gizmos.DrawLine(rightUpperOrigin, rightUpperOrigin + Vector2.right * wallDist);

            // Optional: show the inset bounds used
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = (Vector3)(max - min);
            Gizmos.DrawWireCube(center, size);
        }

    }
}
