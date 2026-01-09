using ThiccTapeman.Inventory;
using UnityEngine;

public class Pickupable : MonoBehaviour
{
    [SerializeField] private ItemSO item;
    [SerializeField] private int quantity = 1;

    public void Pickup()
    {
        InventoryManager.GetInstance().AddItem(item, quantity);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger entered with " + other.name);
        if (other.CompareTag("Player"))
        {
            Pickup();
        }
    }
}
