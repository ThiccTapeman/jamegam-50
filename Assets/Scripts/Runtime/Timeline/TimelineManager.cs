using System.Collections.Generic;
using UnityEngine;

namespace ThiccTapeman.Timeline
{
    public sealed class TimelineManager : MonoBehaviour
    {
        private static TimelineManager instance;

        // Ensure singleton instance
        public static TimelineManager GetInstance()
        {
            if (instance == null)
            {
                instance = FindObjectOfType<TimelineManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("TimelineManager");
                    instance = obj.AddComponent<TimelineManager>();
                }
            }
            return instance;
        }

        [Header("Recording")]
        [Min(0.01f)] public float recordInterval = 0.02f;
        [Min(1f)] public float historySeconds = 30f;

        /// <summary>
        /// Global timeline time used for recording + rewinding.
        /// </summary>
        public float TimeNow { get; private set; }

        /// <summary>
        /// Playback time for branches (pauses when StopTime is active).
        /// </summary>
        public float BranchTime { get; private set; }

        public bool IsTimePaused { get; private set; }

        readonly List<TimelineObject> objects = new List<TimelineObject>();
        readonly List<PauseSegment> pauseSegments = new List<PauseSegment>();
        float recordTimer;
        float pauseStartTime = -1f;

        public readonly struct PauseSegment
        {
            public readonly float start;
            public readonly float end;

            public PauseSegment(float start, float end)
            {
                this.start = start;
                this.end = end;
            }
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // Live world keeps moving, so recording time keeps advancing always
            TimeNow += dt;

            // Branch playback time pauses/resumes
            if (!IsTimePaused)
                BranchTime += dt;

            // Record live objects at interval (uses TimeNow)
            recordTimer += dt;
            if (recordTimer >= recordInterval)
            {
                float t = TimeNow;
                for (int i = 0; i < objects.Count; i++)
                {
                    var obj = objects[i];
                    if (obj != null) obj.RecordState(t);
                }
                recordTimer = 0f;
            }

            // Drive branch playback using BranchTime (freezes when paused)
            float bt = BranchTime;
            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                if (obj != null) obj.TickBranchPlayback(bt);
            }
        }

        public void Register(TimelineObject obj)
        {
            if (obj == null) return;
            if (!objects.Contains(obj)) objects.Add(obj);
        }

        public void Unregister(TimelineObject obj)
        {
            objects.Remove(obj);
        }

        // --- Stop/Resume time (branches freeze, live keeps moving) ---

        public void PauseTime()
        {
            if (IsTimePaused) return;
            IsTimePaused = true;
            pauseStartTime = TimeNow;
        }

        public void ResumeTime()
        {
            if (!IsTimePaused) return;
            IsTimePaused = false;
            if (pauseStartTime >= 0f)
            {
                pauseSegments.Add(new PauseSegment(pauseStartTime, TimeNow));
                pauseStartTime = -1f;
            }
        }

        public void SetTimelineReferencePoint()
        {
            TimeNow = 0f;
            BranchTime = 0f;
            recordTimer = 0f;

            IsTimePaused = false;
            pauseSegments.Clear();
            pauseStartTime = -1f;

            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                if (obj == null) continue;

                obj.ResetTimelineState();
                obj.RecordState(TimeNow);
            }
        }

        // --- Rewind ---

        public void Rewind(float seconds)
        {
            Debug.Log("Rewinding " + seconds + " seconds");
            if (seconds <= 0f) return;

            float fromTime = TimeNow;
            float toTime = Mathf.Max(0f, fromTime - seconds);
            List<PauseSegment> pauseSegmentsForBranch = BuildPauseSegmentsForBranch(fromTime);

            if (IsTimePaused && pauseStartTime >= 0f)
            {
                pauseSegments.Add(new PauseSegment(pauseStartTime, fromTime));
                pauseStartTime = -1f;
            }

            // Rewind both clocks to keep everything aligned after a rewind
            TimeNow = toTime;
            BranchTime = toTime;

            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                if (obj == null) continue;

                obj.OnGlobalRewind(toTime, fromTime, spawnBranchTime: BranchTime, pauseSegments: pauseSegmentsForBranch);
                obj.TrimHistoryOlderThan(toTime - historySeconds);
            }

            TrimPauseSegmentsToTime(toTime);
            TrimPauseSegmentsOlderThan(toTime - historySeconds);

            if (IsTimePaused)
                pauseStartTime = TimeNow;
        }

        List<PauseSegment> BuildPauseSegmentsForBranch(float upToTime)
        {
            if (pauseSegments.Count == 0 && (!IsTimePaused || pauseStartTime < 0f))
                return null;

            var list = new List<PauseSegment>(pauseSegments.Count + 1);

            for (int i = 0; i < pauseSegments.Count; i++)
            {
                var seg = pauseSegments[i];
                if (seg.start >= upToTime) break;

                float end = Mathf.Min(seg.end, upToTime);
                if (end > seg.start)
                    list.Add(new PauseSegment(seg.start, end));
            }

            if (IsTimePaused && pauseStartTime >= 0f && pauseStartTime < upToTime)
                list.Add(new PauseSegment(pauseStartTime, upToTime));

            return list;
        }

        void TrimPauseSegmentsToTime(float time)
        {
            for (int i = pauseSegments.Count - 1; i >= 0; i--)
            {
                var seg = pauseSegments[i];
                if (seg.start >= time)
                {
                    pauseSegments.RemoveAt(i);
                    continue;
                }

                if (seg.end > time)
                    pauseSegments[i] = new PauseSegment(seg.start, time);
            }
        }

        void TrimPauseSegmentsOlderThan(float olderThanTime)
        {
            for (int i = pauseSegments.Count - 1; i >= 0; i--)
            {
                var seg = pauseSegments[i];
                if (seg.end < olderThanTime)
                {
                    pauseSegments.RemoveAt(i);
                    continue;
                }

                if (seg.start < olderThanTime)
                    pauseSegments[i] = new PauseSegment(olderThanTime, seg.end);
            }
        }
    }
}
