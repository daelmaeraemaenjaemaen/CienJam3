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

        if (TryGetFirstNonPlayerHit(ray, out RaycastHit hit))
            currentInteractable = GetInteractableFromHit(hit);

        if (currentInteractable != null)
        {
            string interactText = currentInteractable.GetInteractText();
            if (string.IsNullOrEmpty(interactText))
                HideInteractionText();
            else
                ShowInteractionText(interactText);
        }
        else
        {
            HideInteractionText();
        }
    }

    private void ShowInteractionText(string message)
    {
        if (interactionTextController != null)
            interactionTextController.ShowText(message);
    }

    private void HideInteractionText()
    {
        if (interactionTextController != null)
            interactionTextController.HideText();
    }

    private bool TryGetFirstNonPlayerHit(Ray ray, out RaycastHit firstHit)
    {
        firstHit = default;

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            interactDistance,
            interactLayerMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length <= 0)
            return false;

        SortHitsByDistance(hits);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || IsPlayerCollider(hitCollider))
                continue;

            firstHit = hits[i];
            return true;
        }

        return false;
    }

    private IInteractable GetInteractableFromHit(RaycastHit hit)
    {
        IInteractable interactable = hit.collider.GetComponent<IInteractable>();

        if (interactable == null)
            interactable = hit.collider.GetComponentInParent<IInteractable>();

        return interactable;
    }

    private bool IsPlayerCollider(Collider hitCollider)
    {
        if (playerObject == null || hitCollider == null)
            return false;

        Transform hitTransform = hitCollider.transform;
        return hitTransform == playerObject.transform || hitTransform.IsChildOf(playerObject.transform);
    }

    private void SortHitsByDistance(RaycastHit[] hits)
    {
        for (int i = 0; i < hits.Length - 1; i++)
        {
            for (int j = i + 1; j < hits.Length; j++)
            {
                if (hits[j].distance >= hits[i].distance)
                    continue;

                RaycastHit temp = hits[i];
                hits[i] = hits[j];
                hits[j] = temp;
            }
        }
    }
}
