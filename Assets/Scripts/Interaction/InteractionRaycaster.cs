using UnityEngine;

public class InteractionRaycaster : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject playerObject;
    [SerializeField] private float interactDistance = 2.2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private LayerMask interactLayerMask = ~0;
    [SerializeField] private InteractionTextController interactionTextController;
    [SerializeField] private FlashlightBatteryController flashlightBatteryController;

    private IInteractable currentInteractable;

    private void Update()
    {
        UpdateCurrentInteractable();

        if (currentInteractable != null && Input.GetKeyDown(interactKey))
            currentInteractable.Interact(playerObject);
    }

    private void UpdateCurrentInteractable()
    {
        currentInteractable = null;

        if (playerCamera == null)
        {
            HideInteractionText();
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask))
        {
            currentInteractable = hit.collider.GetComponent<IInteractable>();

            if (currentInteractable == null)
                currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
        }

        if (currentInteractable != null)
            ShowInteractionText(currentInteractable.GetInteractText());
        else
            HideInteractionText();
    }

    private void ShowInteractionText(string message)
    {
        if (flashlightBatteryController != null)
            flashlightBatteryController.HideRechargeHint();

        if (interactionTextController != null)
            interactionTextController.ShowText(message);
    }

    private void HideInteractionText()
    {
        if (interactionTextController != null)
            interactionTextController.HideText();
    }
}
