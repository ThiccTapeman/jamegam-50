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
        bool hasDeltaYParam;
        const string DeltaYParam = "DeltaY";

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
            hasDeltaYParam = animator != null && HasAnimatorParameter(animator, DeltaYParam, AnimatorControllerParameterType.Float);

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
                if (col != null) col.enabled = enabled;
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
