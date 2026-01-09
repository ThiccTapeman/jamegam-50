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

        Rigidbody2D rb;

        public void Init(List<TimelineObject.TimelineState> branchStates, float spawnBranchTime)
        {
            states = branchStates;
            this.spawnBranchTime = spawnBranchTime;

            recordingStartTime = states[0].time;
            recordingEndTime = states[^1].time;

            rb = GetComponent<Rigidbody2D>();

            Apply(Sample(recordingStartTime));
        }

        public void Tick(float branchTime)
        {
            if (states == null || states.Count < 2) return;

            // Plays from beginning immediately when spawned:
            // target = (branchTime - spawnBranchTime) + recordingStartTime
            float target = (branchTime - spawnBranchTime) + recordingStartTime;

            // clamp at end
            target = Mathf.Clamp(target, recordingStartTime, recordingEndTime);

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
        }
    }
}
