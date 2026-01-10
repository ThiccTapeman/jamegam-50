using ThiccTapeman.Inventory;
using ThiccTapeman.Player.Reset;
using UnityEngine;

public class Pickupable : MonoBehaviour
{
    [SerializeField] private ItemSO item;
    [SerializeField] private int quantity = 1;
    private SpriteRenderer spriteRenderer;

    private bool isPickedup = false;


    private void Start()
    {
        ResetManager.GetInstance().OnReset += OnReset;
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    private void OnReset()
    {
        isPickedup = false;
        UpdateVisuals();
    }

    public void Pickup()
    {
        if (isPickedup) return;
        InventoryManager.GetInstance().AddItem(item, quantity);
        isPickedup = true;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        spriteRenderer.enabled = !isPickedup;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Pickup();
        }
    }
}
