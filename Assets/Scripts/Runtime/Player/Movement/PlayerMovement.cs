using UnityEngine;
using ThiccTapeman.Input;
using System.Collections.Generic;

namespace ThiccTapeman.Player.Movement
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private string actionMapPath;
        [SerializeField] private List<PlayerMovementAbility> abilities = new List<PlayerMovementAbility>();

        private InputManager inputManager;

        private void Awake()
        {
            inputManager = InputManager.GetInstance();
            inputManager.SetActionMapPath(actionMapPath);

            // Ensure references
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
                return;
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

            // Initialize abilities
            foreach (var ability in abilities)
            {
                ability.AwakeAbility(inputManager, rb);
            }
        }



        private void Update()
        {
            foreach (var ability in abilities)
            {
                ability.UpdateAbility();
            }
        }

        private void FixedUpdate()
        {
            foreach (var ability in abilities)
            {
                ability.FixedUpdateAbility();
            }
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

