using UnityEngine;

public class FlashlightPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform flashlightHoldPoint;
    [SerializeField] private Vector3 equippedLocalPosition = new Vector3(0.35f, -0.25f, 0.45f);
    [SerializeField] private Vector3 equippedLocalEulerAngles = Vector3.zero;
    [SerializeField] private FlashlightBatteryController flashlightBatteryController;
    [SerializeField] private bool disableColliderAfterPickup = true;
    [SerializeField] private Collider pickupCollider;

    private bool isPickedUp;

    public string GetInteractText()
    {
        return "[E] pick";
    }

    public void Interact(GameObject interactor)
    {
        if (isPickedUp)
            return;

        if (flashlightHoldPoint == null)
        {
            Debug.LogWarning("FlashlightPickup needs a flashlightHoldPoint reference.");
            return;
        }

        if (flashlightBatteryController == null && interactor != null)
            flashlightBatteryController = interactor.GetComponent<FlashlightBatteryController>();

        isPickedUp = true;

        transform.SetParent(flashlightHoldPoint);
        transform.localPosition = equippedLocalPosition;
        transform.localEulerAngles = equippedLocalEulerAngles;

        if (disableColliderAfterPickup && pickupCollider != null)
            pickupCollider.enabled = false;

        if (flashlightBatteryController != null)
            flashlightBatteryController.OnFlashlightPickedUp();
        else
            Debug.LogWarning("FlashlightPickup could not find a FlashlightBatteryController.");
    }
}
