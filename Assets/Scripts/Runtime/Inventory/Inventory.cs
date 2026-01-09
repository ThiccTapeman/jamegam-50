using UnityEngine;

//
//this class with Slot.cs handles the input (old input system now for testing), keeping fruits and display the remaining amount of them
//

public class Inventory : MonoBehaviour
{
    public Slot RewindSlot;
    public Slot FreezeSlot;
    public Slot SpeedSlot;

    private void Update()
    {
        RewindSlot.ColldownTick();
        FreezeSlot.ColldownTick();
        SpeedSlot.ColldownTick();

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ActivateRewind();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            ActivateFreeze();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            ActivateSpeed();
        }
    }

    public void ActivateRewind()
    {
        RewindSlot.TryUse();
    }

    public void ActivateFreeze()
    {
        FreezeSlot.TryUse();
    }

    public void ActivateSpeed()
    {
        SpeedSlot.TryUse();
    }
}
