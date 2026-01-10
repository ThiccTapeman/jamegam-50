using ThiccTapeman.Player.Reset;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Vector3 spawnPointOffset;
    [SerializeField] private Slot[] setInventory;
    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private CandleStack candleStack;
    [SerializeField] private int amountOfCandlesToLight;

    private bool hasUnlocked = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag("PlayerGhost")) return;
        if (!collision.transform.CompareTag("Player")) return;
        if (collision.isTrigger) return;
        if (!hasUnlocked)
        {
            hasUnlocked = true;
            ResetManager.GetInstance().SetSpawnPoint(transform.position + spawnPointOffset, setInventory);
            UpdateVisuals();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position + spawnPointOffset, Vector3.one);
    }

    private void UpdateVisuals()
    {
        particleSystem.Play();
        candleStack.Light(amountOfCandlesToLight);
    }
}
