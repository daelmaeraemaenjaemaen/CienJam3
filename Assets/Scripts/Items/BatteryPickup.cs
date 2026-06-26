using UnityEngine;

public class BatteryPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private FlashlightBatteryController flashlightBatteryController;
    [SerializeField] private int spareBatteryAmount = 1;

    public string GetInteractText()
    {
        return "[E] pick";
    }

    public void Interact(GameObject interactor)
    {
        FlashlightBatteryController targetController = flashlightBatteryController;

        if (targetController == null && interactor != null)
            targetController = interactor.GetComponent<FlashlightBatteryController>();

        if (targetController == null)
        {
            Debug.LogWarning("BatteryPickup could not find a FlashlightBatteryController.");
            return;
        }

        targetController.AddSpareBattery(spareBatteryAmount);
        Destroy(gameObject);
    }
}
