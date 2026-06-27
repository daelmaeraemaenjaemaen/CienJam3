using UnityEngine;

public class InteractionRaycaster : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject playerObject;
    [SerializeField] private float interactDistance = 2.2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private LayerMask interactLayerMask = ~0;
    [SerializeField] private InteractionTextController interactionTextController;

    private IInteractable currentInteractable;

    private void Awake()
    {
        if (playerCamera == null)
            Debug.LogWarning("InteractionRaycaster: playerCamera is not assigned.");

        if (interactionTextController == null)
            Debug.LogWarning("InteractionRaycaster: interactionTextController is not assigned.");
    }

    private void Update()
    {
        UpdateCurrentInteractable();

        if (currentInteractable != null && Input.GetKeyDown(interactKey))
            currentInteractable.Interact(playerObject != null ? playerObject : gameObject);
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
            Debug.Log($"[InteractionRaycaster] Hit: {hit.collider.gameObject.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, Distance: {hit.distance}");

            IInteractable directInteractable = hit.collider.GetComponent<IInteractable>();
            IInteractable parentInteractable = null;

            currentInteractable = directInteractable;

            if (currentInteractable == null)
            {
                parentInteractable = hit.collider.GetComponentInParent<IInteractable>();
                currentInteractable = parentInteractable;
            }

            Debug.Log($"[InteractionRaycaster] Direct interactable: {directInteractable != null}, Parent interactable: {parentInteractable != null}");
        }
        else
        {
            Debug.Log("[InteractionRaycaster] Raycast hit nothing");
        }

        Debug.Log($"[InteractionRaycaster] currentInteractable is null: {currentInteractable == null}");

        if (currentInteractable != null)
            ShowInteractionText(currentInteractable.GetInteractText());
        else
            HideInteractionText();
    }

    private void ShowInteractionText(string message)
    {
        Debug.Log("[InteractionRaycaster] Show interaction text");

        if (interactionTextController != null)
            interactionTextController.ShowText(message);
    }

    private void HideInteractionText()
    {
        Debug.Log("[InteractionRaycaster] Hide interaction text");

        if (interactionTextController != null)
            interactionTextController.HideText();
    }
}
