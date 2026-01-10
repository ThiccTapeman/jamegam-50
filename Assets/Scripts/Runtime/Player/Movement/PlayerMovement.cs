using UnityEngine;
using ThiccTapeman.Input;
using ThiccTapeman.Timeline;
using System.Collections.Generic;

namespace ThiccTapeman.Player.Movement
{
    public class PlayerMovement : MonoBehaviour
    {
        private static PlayerMovement instance;

        public static PlayerMovement GetInstance()
        {
            if (instance == null)
            {
                GameObject obj = GameObject.Find("Player");
                obj.TryGetComponent<PlayerMovement>(out instance);

                if (instance == null)
                {
                    Debug.LogError("PlayerMovement instance not found in the scene.");
                }
            }
            return instance;
        }

        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Animator animator;
        [SerializeField] private string actionMapPath;
        [SerializeField] private List<PlayerMovementAbility> abilities = new List<PlayerMovementAbility>();

        private InputManager inputManager;

        private void Awake()
        {
            // Ensure singleton
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Debug.LogWarning("Multiple instances of PlayerMovement detected. Destroying duplicate.");
                Destroy(this.gameObject);
                return;
            }

            // Setup InputManager
            inputManager = InputManager.GetInstance();
            inputManager.SetActionMapPath(actionMapPath);

            // Ensure references
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
                return;
            }
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            if (inputManager == null)
            {
                Debug.LogError("InputManager instance not found in the scene.");
                return;
            }
            if (rb == null)
            {
                Debug.LogError("Rigidbody2D component not found on the Player GameObject.");
                return;
            }
            if (animator == null)
            {
                Debug.LogError("Animator component was not found on the Player GameObject");
            }

            // Initialize abilities
            foreach (var ability in abilities)
            {
                ability.AwakeAbility(inputManager, rb, animator);
            }
        }



        bool IsMovementAllowed()
        {
            var manager = TimelineManager.TryGetInstance();
            if (manager != null && !manager.IsRunning) return false;
            if (DialogueManager.IsDialogueActive) return false;
            return true;
        }

        private void Update()
        {
            if (!IsMovementAllowed()) return;

            foreach (var ability in abilities)
            {
                ability.UpdateAbility();
            }
        }

        private void FixedUpdate()
        {
            if (!IsMovementAllowed()) return;

            foreach (var ability in abilities)
            {
                ability.FixedUpdateAbility();
            }
        }

        public bool HasAbility<T>() where T : PlayerMovementAbility
        {
            foreach (var ability in abilities)
            {
                if (ability is T)
                {
                    return true;
                }
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Rigidbody2D r = rb != null ? rb : GetComponent<Rigidbody2D>();
            Collider2D c = r != null ? r.GetComponent<Collider2D>() : GetComponent<Collider2D>();

            foreach (var ability in abilities)
                if (ability != null)
                    ability.DrawGizmos(r, c);
        }
#endif
    }
}

