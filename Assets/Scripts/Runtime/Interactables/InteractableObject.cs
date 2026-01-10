using UnityEngine;

public abstract class InteractableObject : MonoBehaviour
{
    [Header("Prompt")]
    public Vector3 promptOffset = Vector3.up;

    public abstract void Interact(GameObject interactor);

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawCube(transform.position + promptOffset, Vector3.one);
    }
}
