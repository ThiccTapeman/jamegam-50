using System.Collections.Generic;
using UnityEngine;

namespace ThiccTapeman.Timeline
{
    [DisallowMultipleComponent]
    public sealed class TimelineObject : MonoBehaviour
    {
        [Header("Branching")]
        public bool createBranches = true;
        [Range(0, 10)] public int maxBranches = 10;

        [Tooltip("Optional prefab used for branch visuals. If null, uses this GameObject.")]
        public GameObject branchPrefabOverride;

        [Header("Mode (runtime)")]
        [SerializeField] bool isBranchInstance = false;
        public bool IsBranchInstance => isBranchInstance;

        Rigidbody2D rb;
        Animator animator;
        SpriteRenderer sr;
        bool hasDeltaYParam;
        const string DeltaYParam = "DeltaY";

        // Live recording (only on the live object)
        readonly List<TimelineState> liveStates = new List<TimelineState>();

        // Stored branch recordings (owned by this object)
        readonly List<BranchData> branches = new List<BranchData>();

        // Spawned branch GameObjects (recreated on each rewind)
        readonly List<TimelineBranchPlayer> branchPlayers = new List<TimelineBranchPlayer>();

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            sr = GetComponentInChildren<SpriteRenderer>();
            hasDeltaYParam = animator != null && HasAnimatorParameter(animator, DeltaYParam, AnimatorControllerParameterType.Float);

            // Only register live objects
            if (!isBranchInstance)
                TimelineManager.GetInstance()?.Register(this);
        }

        void OnDestroy()
        {
            if (isBranchInstance) return;

            var manager = TimelineManager.TryGetInstance();
            if (manager != null) manager.Unregister(this);
        }

        // ---------------- Live recording ----------------

        public void RecordState(float t)
        {
            if (isBranchInstance) return;
            if (rb == null) return;

            bool hasAnimator = false;
            int animStateHash = 0;
            float animNormalizedTime = 0f;
            bool animLoop = false;

            if (animator != null)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                hasAnimator = true;
                animStateHash = state.fullPathHash;
                animNormalizedTime = state.normalizedTime;
                animLoop = state.loop || animNormalizedTime > 1f;
            }

            bool hasSpriteRenderer = false;
            bool spriteFlipX = false;

            if (sr != null)
            {
                hasSpriteRenderer = true;
                spriteFlipX = sr.flipX;
            }

            bool hasDeltaY = false;
            float deltaY = 0f;
            if (animator != null && hasDeltaYParam)
            {
                hasDeltaY = true;
                deltaY = animator.GetFloat(DeltaYParam);
            }

            liveStates.Add(new TimelineState(
                t,
                rb.position,
                rb.rotation,
                rb.linearVelocity,
                rb.angularVelocity,
                hasAnimator,
                animStateHash,
                animNormalizedTime,
                animLoop,
                hasSpriteRenderer,
                spriteFlipX,
                hasDeltaY,
                deltaY
            ));
        }

        public void TrimHistoryOlderThan(float olderThanTime)
        {
            RemoveOlderThan(liveStates, olderThanTime);

            // Optional: trim stored branches too
            for (int i = 0; i < branches.Count; i++)
                RemoveOlderThan(branches[i].states, olderThanTime);
        }

        static void RemoveOlderThan(List<TimelineState> list, float olderThanTime)
        {
            int removeCount = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].time < olderThanTime) removeCount++;
                else break;
            }
            if (removeCount > 0) list.RemoveRange(0, removeCount);
        }

        void TrimLiveFutureAfter(float time)
        {
            // Remove states with time > given time
            for (int i = liveStates.Count - 1; i >= 0; i--)
            {
                if (liveStates[i].time > time) liveStates.RemoveAt(i);
                else break;
            }
        }

        public void ResetTimelineState()
        {
            if (isBranchInstance) return;

            liveStates.Clear();
            branches.Clear();
            DestroyAllBranchInstances();
        }

        // ---------------- Rewind ----------------

        /// <summary>
        /// Called by TimelineManager when time rewinds from 'fromTime' to 'toTime'.
        /// Destroys all branch instances and recreates them fresh.
        /// </summary>
        public void OnGlobalRewind(float toTime, float fromTime, float spawnBranchTime, List<TimelineManager.PauseSegment> pauseSegments)
        {
            if (isBranchInstance) return;
            if (liveStates.Count < 2) return;

            // 1) Store abandoned timeline as a new branch recording (up to fromTime)
            if (createBranches && maxBranches > 0)
            {
                List<TimelineState> branchRecording = ExtractHistoryUpTo(liveStates, fromTime);
                if (branchRecording != null && branchRecording.Count >= 2)
                {
                    branchRecording = ApplyPauseSegments(branchRecording, pauseSegments);
                    branches.Add(new BranchData(branchRecording));

                    while (branches.Count > maxBranches)
                        branches.RemoveAt(0);
                }
            }

            // 2) Snap live object back
            TimelineState at = SampleState(liveStates, toTime);
            ApplyLiveState(at);

            // 3) Delete live future after toTime
            TrimLiveFutureAfter(toTime);

            // 4) Destroy all previous branch instances
            DestroyAllBranchInstances();

            // 5) Recreate branch instances fresh (they will play from start immediately using BranchTime)
            RecreateBranchInstances(spawnGlobalTime: spawnBranchTime);
        }

        // ---------------- Branch playback ----------------

        // Manager calls this each frame with BranchTime (which pauses)
        public void TickBranchPlayback(float branchTime)
        {
            if (isBranchInstance) return;

            for (int i = 0; i < branchPlayers.Count; i++)
            {
                var p = branchPlayers[i];
                if (p != null) p.Tick(branchTime);
            }
        }

        // ---------------- Branch instance lifecycle ----------------

        void DestroyAllBranchInstances()
        {
            for (int i = 0; i < branchPlayers.Count; i++)
            {
                var p = branchPlayers[i];
                if (p != null) Destroy(p.gameObject);
            }
            branchPlayers.Clear();
        }

        void RecreateBranchInstances(float spawnGlobalTime)
        {
            for (int i = 0; i < branches.Count; i++)
            {
                var data = branches[i];
                if (data == null || data.states == null || data.states.Count < 2) continue;

                GameObject prefab = branchPrefabOverride != null ? branchPrefabOverride : gameObject;
                GameObject g = Instantiate(prefab, transform.position, transform.rotation);

                // Mark as branch instance so it doesn't register/record
                var obj = g.GetComponent<TimelineObject>();
                if (obj != null) obj.isBranchInstance = true;

                // Physics should not fight playback
                var gRb = g.GetComponent<Rigidbody2D>();
                if (gRb != null)
                {
                    gRb.linearVelocity = Vector2.zero;
                    gRb.angularVelocity = 0f;
                    gRb.bodyType = RigidbodyType2D.Kinematic;
                    gRb.simulated = true;
                }

                var player = g.GetComponent<TimelineBranchPlayer>();
                if (player == null) player = g.AddComponent<TimelineBranchPlayer>();

                // Branch starts playing from beginning immediately when spawned
                player.Init(data.states, spawnGlobalTime);

                branchPlayers.Add(player);
            }
        }

        // ---------------- Sampling helpers ----------------

        static List<TimelineState> ExtractHistoryUpTo(List<TimelineState> source, float endTime)
        {
            if (source == null || source.Count < 2) return null;

            float start = source[0].time;
            float end = Mathf.Clamp(endTime, start, source[^1].time);

            var list = new List<TimelineState>(source.Count);

            list.Add(SampleState(source, start));

            for (int i = 0; i < source.Count; i++)
            {
                float t = source[i].time;
                if (t > start && t < end)
                    list.Add(source[i]);
            }

            list.Add(SampleState(source, end));
            return list;
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

        static List<TimelineState> ApplyPauseSegments(
            List<TimelineState> source,
            List<TimelineManager.PauseSegment> pauses)
        {
            if (source == null || source.Count < 2 || pauses == null || pauses.Count == 0)
                return source;

            float startTime = source[0].time;
            float endTime = source[^1].time;

            var result = new List<TimelineState>(source.Count);
            int i = 0;

            for (int p = 0; p < pauses.Count; p++)
            {
                var pause = pauses[p];
                if (pause.end <= startTime || pause.start >= endTime) continue;

                float segStart = Mathf.Max(startTime, pause.start);
                float segEnd = Mathf.Min(endTime, pause.end);
                if (segEnd <= segStart) continue;

                while (i < source.Count && source[i].time < segStart)
                {
                    result.Add(source[i]);
                    i++;
                }

                TimelineState frozen = SampleState(source, segStart);

                if (result.Count == 0 || result[^1].time < segStart)
                {
                    result.Add(frozen);
                }
                else if (Mathf.Abs(result[^1].time - segStart) < 0.0001f)
                {
                    result[^1] = frozen;
                }

                while (i < source.Count && source[i].time <= segEnd)
                    i++;

                frozen.time = segEnd;
                if (result.Count == 0 || result[^1].time < segEnd)
                {
                    result.Add(frozen);
                }
                else if (Mathf.Abs(result[^1].time - segEnd) < 0.0001f)
                {
                    result[^1] = frozen;
                }
            }

            while (i < source.Count)
            {
                result.Add(source[i]);
                i++;
            }

            return result.Count >= 2 ? result : source;
        }

        static TimelineState SampleState(List<TimelineState> source, float t)
        {
            if (source == null || source.Count == 0) return default;

            if (t <= source[0].time) return source[0];
            if (t >= source[^1].time) return source[^1];

            int hi = 0;
            while (hi < source.Count && source[hi].time < t) hi++;
            int lo = Mathf.Max(0, hi - 1);

            TimelineState a = source[lo];
            TimelineState b = source[hi];

            float dt = b.time - a.time;
            float u = dt <= 0.0001f ? 0f : Mathf.Clamp01((t - a.time) / dt);

            return TimelineState.Lerp(a, b, u, t);
        }

        void ApplyLiveState(TimelineState s)
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;

            rb.position = s.position;
            rb.rotation = s.rotation;
            rb.linearVelocity = s.velocity;
            rb.angularVelocity = s.angularVelocity;

            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null && s.hasAnimator)
            {
                if (s.hasDeltaY)
                    animator.SetFloat(DeltaYParam, s.deltaY);

                float normalized = s.animLoop
                    ? Mathf.Repeat(s.animNormalizedTime, 1f)
                    : Mathf.Clamp01(s.animNormalizedTime);

                animator.Play(s.animStateHash, 0, normalized);
                animator.Update(0f);
            }

            if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && s.hasSpriteRenderer)
                sr.flipX = s.spriteFlipX;
        }

        // ---------------- Data types ----------------

        [System.Serializable]
        public sealed class BranchData
        {
            public readonly List<TimelineState> states;
            public BranchData(List<TimelineState> states) { this.states = states; }
        }

        [System.Serializable]
        public struct TimelineState
        {
            public float time;
            public Vector2 position;
            public float rotation;
            public Vector2 velocity;
            public float angularVelocity;
            public bool hasAnimator;
            public int animStateHash;
            public float animNormalizedTime;
            public bool animLoop;
            public bool hasSpriteRenderer;
            public bool spriteFlipX;
            public bool hasDeltaY;
            public float deltaY;

            public TimelineState(
                float time,
                Vector2 pos,
                float rot,
                Vector2 vel,
                float angVel,
                bool hasAnimator,
                int animStateHash,
                float animNormalizedTime,
                bool animLoop,
                bool hasSpriteRenderer,
                bool spriteFlipX,
                bool hasDeltaY,
                float deltaY)
            {
                this.time = time;
                position = pos;
                rotation = rot;
                velocity = vel;
                angularVelocity = angVel;
                this.hasAnimator = hasAnimator;
                this.animStateHash = animStateHash;
                this.animNormalizedTime = animNormalizedTime;
                this.animLoop = animLoop;
                this.hasSpriteRenderer = hasSpriteRenderer;
                this.spriteFlipX = spriteFlipX;
                this.hasDeltaY = hasDeltaY;
                this.deltaY = deltaY;
            }

            public static TimelineState Lerp(TimelineState a, TimelineState b, float u, float time)
            {
                TimelineState s = new TimelineState(
                    time,
                    Vector2.Lerp(a.position, b.position, u),
                    Mathf.LerpAngle(a.rotation, b.rotation, u),
                    Vector2.Lerp(a.velocity, b.velocity, u),
                    Mathf.Lerp(a.angularVelocity, b.angularVelocity, u),
                    false,
                    0,
                    0f,
                    false,
                    false,
                    false,
                    false,
                    0f
                );

                if (a.hasAnimator && b.hasAnimator)
                {
                    if (a.animStateHash == b.animStateHash)
                    {
                        s.hasAnimator = true;
                        s.animStateHash = a.animStateHash;
                        s.animLoop = a.animLoop;
                        s.animNormalizedTime = Mathf.Lerp(a.animNormalizedTime, b.animNormalizedTime, u);
                    }
                    else if (u < 0.5f)
                    {
                        s.hasAnimator = true;
                        s.animStateHash = a.animStateHash;
                        s.animNormalizedTime = a.animNormalizedTime;
                        s.animLoop = a.animLoop;
                    }
                    else
                    {
                        s.hasAnimator = true;
                        s.animStateHash = b.animStateHash;
                        s.animNormalizedTime = b.animNormalizedTime;
                        s.animLoop = b.animLoop;
                    }
                }
                else if (a.hasAnimator)
                {
                    s.hasAnimator = true;
                    s.animStateHash = a.animStateHash;
                    s.animNormalizedTime = a.animNormalizedTime;
                    s.animLoop = a.animLoop;
                }
                else if (b.hasAnimator)
                {
                    s.hasAnimator = true;
                    s.animStateHash = b.animStateHash;
                    s.animNormalizedTime = b.animNormalizedTime;
                    s.animLoop = b.animLoop;
                }

                if (a.hasSpriteRenderer && b.hasSpriteRenderer)
                {
                    s.hasSpriteRenderer = true;
                    s.spriteFlipX = (u < 0.5f) ? a.spriteFlipX : b.spriteFlipX;
                }
                else if (a.hasSpriteRenderer)
                {
                    s.hasSpriteRenderer = true;
                    s.spriteFlipX = a.spriteFlipX;
                }
                else if (b.hasSpriteRenderer)
                {
                    s.hasSpriteRenderer = true;
                    s.spriteFlipX = b.spriteFlipX;
                }

                if (a.hasDeltaY && b.hasDeltaY)
                {
                    s.hasDeltaY = true;
                    s.deltaY = Mathf.Lerp(a.deltaY, b.deltaY, u);
                }
                else if (a.hasDeltaY)
                {
                    s.hasDeltaY = true;
                    s.deltaY = a.deltaY;
                }
                else if (b.hasDeltaY)
                {
                    s.hasDeltaY = true;
                    s.deltaY = b.deltaY;
                }

                return s;
            }
        }
    }
}
