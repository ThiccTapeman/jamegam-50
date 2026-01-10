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
                resetInventory = InventoryManager.GetInstance().slots.ToArray();
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
            if (resetAction.ReadValue<float>() > 0.5f)
            {
                Reset();
            }
        }

        public delegate void ResetAction();
        public event ResetAction OnReset;

        public void Reset(bool resetPlayerPosition = true)
        {
            if (resetPlayerPosition)
            {
                playerInstance.transform.position = currentSpawnPoint;
            }

            TimelineManager.GetInstance().SetTimelineReferencePoint();
            InventoryManager.GetInstance().SetInventory(resetInventory);

            OnReset?.Invoke();
            Debug.Log("All resettable objects have been reset.");
        }

        public void SetSpawnPoint(Vector2 spawnPoint, Slot[] slots)
        {
            currentSpawnPoint = spawnPoint;
            resetInventory = slots;
            Reset(false);
        }
    }
}