using System.Collections;
using ThiccTapeman.Input;
using ThiccTapeman.Timeline;
using UnityEngine;

namespace ThiccTapeman.Player.Reset
{
    /// <summary>
    /// Manages resetting the player and other resettable objects to their initial states.
    /// Usage: Call <code>ResetManager.GetInstance().Reset()</code> to reset all registered objects.
    /// Usage: Objects can subscribe to the <code>OnReset</code> event to handle their own reset logic.
    /// </summary>
    public class ResetManager : MonoBehaviour
    {
        private static ResetManager instance;
        private InputManager inputManager;
        private InputItem resetAction;

        private Vector2 currentSpawnPoint;
        private GameObject playerInstance;

        private Slot[] resetInventory;
        [SerializeField] private float resetCooldownSeconds = 0.25f;
        private float lastResetTime = -10f;
        private Coroutine timelineReferenceRoutine;

        private static Slot[] CloneSlots(System.Collections.Generic.IList<Slot> source)
        {
            if (source == null) return null;

            var copy = new Slot[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                var slot = source[i];
                copy[i] = new Slot
                {
                    itemSO = slot != null ? slot.itemSO : null,
                    Amount = slot != null ? slot.Amount : 0
                };
            }

            return copy;
        }



        private void Awake()
        {
            // Insure singleton
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(this.gameObject);
            }

            if (playerInstance == null)
            {
                playerInstance = GameObject.FindWithTag("Player");
                currentSpawnPoint = playerInstance.transform.position;
                resetInventory = CloneSlots(InventoryManager.GetInstance().slots);
            }


        }

        private void Start()
        {
            inputManager = InputManager.GetInstance();
            resetAction = inputManager.GetAction("Player", "Reset");
        }

        public static ResetManager GetInstance()
        {
            // Insure singleton
            if (instance == null)
            {
                instance = FindObjectOfType<ResetManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("ResetManager");
                    instance = obj.AddComponent<ResetManager>();
                }
            }
            return instance;
        }

        private void Update()
        {
            if (resetAction == null) return;
            if (Time.time - lastResetTime < resetCooldownSeconds) return;
            if (resetAction.GetTriggered(true))
            {
                Reset();
            }
        }

        public delegate void ResetAction();
        public event ResetAction OnReset;

        public void Reset(bool resetPlayerPosition = true)
        {
            if (Time.time - lastResetTime < resetCooldownSeconds) return;
            lastResetTime = Time.time;
            if (resetPlayerPosition)
            {
                playerInstance.transform.position = currentSpawnPoint;
            }

            InventoryManager.GetInstance().SetInventory(resetInventory);
            OnReset?.Invoke();
            if (timelineReferenceRoutine != null)
                StopCoroutine(timelineReferenceRoutine);
            timelineReferenceRoutine = StartCoroutine(SetTimelineReferencePointNextFrame());
            Debug.Log("All resettable objects have been reset.");
        }

        public void SetSpawnPoint(Vector2 spawnPoint, Slot[] slots)
        {
            currentSpawnPoint = spawnPoint;
            resetInventory = CloneSlots(slots);
            LevelTimer.GetInstance().SetCheckpointReferenceToCurrent();
            Reset(false);
        }

        private IEnumerator SetTimelineReferencePointNextFrame()
        {
            // Wait for transforms/physics to settle after teleport + reset listeners.
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();
            TimelineManager.GetInstance().SetTimelineReferencePoint();
            timelineReferenceRoutine = null;
        }
    }
}
