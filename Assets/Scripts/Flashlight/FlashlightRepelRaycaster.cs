using UnityEngine;

public class FlashlightRepelRaycaster : MonoBehaviour
{
    [SerializeField] private FlashlightBatteryController flashlightBatteryController;
    [SerializeField] private Transform flashlightRayOrigin;
    [SerializeField] private float repelRayDistance = 12f;
    [SerializeField] private LayerMask enemyFaceLayerMask = ~0;
    [SerializeField] private bool drawDebugRay = true;

    private void Update()
    {
        if (!drawDebugRay || flashlightRayOrigin == null)
            return;

        Color rayColor = CanCastRepelRay() ? Color.cyan : Color.gray;
        Debug.DrawRay(flashlightRayOrigin.position, flashlightRayOrigin.forward * repelRayDistance, rayColor);
    }

    public Ray GetRepelRay()
    {
        if (flashlightRayOrigin == null)
        {
            Debug.LogWarning("FlashlightRepelRaycaster needs a flashlightRayOrigin reference.");
            return new Ray(Vector3.zero, Vector3.forward);
        }

        return new Ray(flashlightRayOrigin.position, flashlightRayOrigin.forward);
    }

    public bool TryGetHitFace(out RaycastHit hit)
    {
        hit = default(RaycastHit);

        if (!CanCastRepelRay())
            return false;

        Ray ray = new Ray(flashlightRayOrigin.position, flashlightRayOrigin.forward);
        return Physics.Raycast(ray, out hit, repelRayDistance, enemyFaceLayerMask);
    }

    private bool CanCastRepelRay()
    {
        if (flashlightBatteryController == null)
            return false;

        if (!flashlightBatteryController.CanRepelEnemy())
            return false;

        if (flashlightRayOrigin == null)
            return false;

        return true;
    }
}
