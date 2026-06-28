using UnityEngine;

public class RadioInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private RadioUIController radioUIController;
    [SerializeField] private FlashlightBatteryController flashlightBatteryController;
    [SerializeField] private InteractionTextController interactionTextController;
    [SerializeField] private string interactText = "[E] Look";
    [SerializeField] private string insufficientBatteryText = "배터리가 1개 필요합니다.";
    [SerializeField] private bool requireBatteryOnFirstOpen = true;
    [SerializeField] private int requiredSpareBatteryCount = 1;

    private bool hasConsumedBattery;

    public string GetInteractText()
    {
        if (NeedsBattery() && !HasEnoughBattery())
            return insufficientBatteryText;

        return interactText;
    }

    public void Interact(GameObject interactor)
    {
        if (NeedsBattery())
        {
            FlashlightBatteryController batteryController = GetBatteryController(interactor);
            if (batteryController == null)
            {
                Debug.LogWarning("[RadioInteractable] FlashlightBatteryController is not assigned.");
                return;
            }

            if (!batteryController.TryConsumeSpareBattery(requiredSpareBatteryCount))
            {
                ShowInsufficientBatteryText();
                return;
            }

            hasConsumedBattery = true;
        }

        if (radioUIController != null)
            radioUIController.Open();
    }

    private bool NeedsBattery()
    {
        return requireBatteryOnFirstOpen && !hasConsumedBattery;
    }

    private bool HasEnoughBattery()
    {
        FlashlightBatteryController batteryController = GetBatteryController(null);
        return batteryController == null || batteryController.GetSpareBatteryCount() >= Mathf.Max(1, requiredSpareBatteryCount);
    }

    private FlashlightBatteryController GetBatteryController(GameObject interactor)
    {
        if (flashlightBatteryController != null)
            return flashlightBatteryController;

        if (interactor != null)
            flashlightBatteryController = interactor.GetComponentInParent<FlashlightBatteryController>();

        return flashlightBatteryController;
    }

    private void ShowInsufficientBatteryText()
    {
        if (interactionTextController != null)
            interactionTextController.ShowText(insufficientBatteryText);
    }
}
