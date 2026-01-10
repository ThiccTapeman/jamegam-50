using System.Collections.Generic;
using UnityEngine;

namespace ThiccTapeman.Timeline
{
    public sealed class TimelineBranchPlayer : MonoBehaviour
    {
        List<TimelineObject.TimelineState> states;

        // Uses BranchTime (which pauses) instead of TimeNow.
        float spawnBranchTime;
        float recordingStartTime;
        float recordingEndTime;
        bool isEnded;

        Rigidbody2D rb;
        Animator animator;
        SpriteRenderer sr;
        Collider2D[] colliders;
        bool[] colliderOriginalTriggers;
        bool hasDeltaYParam;
        bool hasDeltaXParam;
        bool hasIsGroundedParam;
        bool hasIsWallSlidingParam;
        const string DeltaYParam = "DeltaY";
        const string DeltaXParam = "DeltaX";
        const string IsGroundedParam = "IsGrounded";
        const string IsWallSlidingParam = "IsWallSliding";

        int levelMask;
        [SerializeField] float idleVelocityThreshold = 0.05f;

        public void Init(List<TimelineObject.TimelineState> branchStates, float spawnBranchTime)
        {
            states = branchStates;
            this.spawnBranchTime = spawnBranchTime;

            recordingStartTime = states[0].time;
            recordingEndTime = states[^1].time;

            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            sr = GetComponentInChildren<SpriteRenderer>();
            colliders = GetComponentsInChildren<Collider2D>();
            if (colliders != null && colliders.Length > 0)
            {
                colliderOriginalTriggers = new bool[colliders.Length];
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliderOriginalTriggers[i] = colliders[i] != null && colliders[i].isTrigger;
                }
            }
            hasDeltaYParam = animator != null && HasAnimatorParameter(animator, DeltaYParam, AnimatorControllerParameterType.Float);
            hasDeltaXParam = animator != null && HasAnimatorParameter(animator, DeltaXParam, AnimatorControllerParameterType.Float);
            hasIsGroundedParam = animator != null && HasAnimatorParameter(animator, IsGroundedParam, AnimatorControllerParameterType.Bool);
            hasIsWallSlidingParam = animator != null && HasAnimatorParameter(animator, IsWallSlidingParam, AnimatorControllerParameterType.Bool);
            levelMask = LayerMask.GetMask("Level");

            Apply(Sample(recordingStartTime));
            isEnded = false;

            var manager = TimelineManager.TryGetInstance();
            if (manager != null)
            {
                manager.OnPauseStateChanged += HandlePauseStateChanged;
                ApplyCollisionState(manager.IsTimePaused);
            }
            else
            {
                ApplyCollisionState(false);
            }
        }

        public void Tick(float branchTime)
        {
            if (states == null || states.Count < 2) return;
            if (isEnded) return;

            // Plays from beginning immediately when spawned:
            // target = (branchTime - spawnBranchTime) + recordingStartTime
            float target = (branchTime - spawnBranchTime) + recordingStartTime;

            // clamp at end
            target = Mathf.Clamp(target, recordingStartTime, recordingEndTime);

            if (target >= recordingEndTime)
            {
                var last = Sample(recordingEndTime);
                Apply(last);
                EndPlayback(last);
                return;
            }

            Apply(Sample(target));
        }

        TimelineObject.TimelineState Sample(float t)
        {
            if (t <= states[0].time) return states[0];
            if (t >= states[^1].time) return states[^1];

            int hi = 0;
            while (hi < states.Count && states[hi].time < t) hi++;
            int lo = Mathf.Max(0, hi - 1);

            var a = states[lo];
            var b = states[hi];

            float dt = b.time - a.time;
            float u = dt <= 0.0001f ? 0f : Mathf.Clamp01((t - a.time) / dt);

            return TimelineObject.TimelineState.Lerp(a, b, u, t);
        }

        void Apply(TimelineObject.TimelineState s)
        {
            if (rb != null)
            {
                rb.position = s.position;
                rb.rotation = s.rotation;
            }
            else
            {
                transform.position = s.position;
                transform.rotation = Quaternion.Euler(0f, 0f, s.rotation);
            }

            if (animator != null && s.hasAnimator)
            {
                if (s.hasDeltaY && hasDeltaYParam)
                    animator.SetFloat(DeltaYParam, s.deltaY);

                float normalized = s.animLoop
                    ? Mathf.Repeat(s.animNormalizedTime, 1f)
                    : Mathf.Clamp01(s.animNormalizedTime);

                animator.Play(s.animStateHash, 0, normalized);
                animator.Update(0f);
            }

            if (sr != null && s.hasSpriteRenderer)
                sr.flipX = s.spriteFlipX;
        }

        void EndPlayback(TimelineObject.TimelineState s)
        {
            if (rb == null)
            {
                isEnded = true;
                return;
            }

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = s.velocity;
            rb.angularVelocity = s.angularVelocity;
            isEnded = true;
            ApplyCollisionState(true);
        }

        void Update()
        {
            if (!isEnded) return;
            if (rb == null || animator == null) return;

            Vector2 v = rb.linearVelocity;
            bool grounded = levelMask != 0 && rb.IsTouchingLayers(levelMask);

            if (grounded && Mathf.Abs(v.x) < idleVelocityThreshold && Mathf.Abs(v.y) < idleVelocityThreshold)
            {
                v = Vector2.zero;
            }

            if (hasDeltaXParam)
                animator.SetFloat(DeltaXParam, Mathf.Abs(v.x));
            if (hasDeltaYParam)
                animator.SetFloat(DeltaYParam, v.y);
            if (hasIsGroundedParam)
                animator.SetBool(IsGroundedParam, grounded);
            if (hasIsWallSlidingParam)
                animator.SetBool(IsWallSlidingParam, false);
        }

        void HandlePauseStateChanged(bool isPaused)
        {
            ApplyCollisionState(isPaused);
        }

        void ApplyCollisionState(bool enabled)
        {
            if (colliders == null) return;
            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (col == null) continue;

                col.enabled = true;

                bool originalTrigger = colliderOriginalTriggers != null
                    && i >= 0
                    && i < colliderOriginalTriggers.Length
                    && colliderOriginalTriggers[i];

                if (isEnded || enabled)
                {
                    col.isTrigger = originalTrigger;
                }
                else
                {
                    col.isTrigger = true;
                }
            }
        }

        void OnDestroy()
        {
            var manager = TimelineManager.TryGetInstance();
            if (manager != null)
                manager.OnPauseStateChanged -= HandlePauseStateChanged;
        }

        static bool HasAnimatorParameter(Animator anim, string name, AnimatorControllerParameterType type)
        {
            if (anim == null) return false;
            foreach (var p in anim.parameters)
            {
                if (p.name == name && p.type == type)
                    return true;
            }
            return false;
        }
    }
}
