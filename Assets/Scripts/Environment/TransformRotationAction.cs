using System.Collections;
using UnityEngine;

public class TransformRotationAction : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 targetLocalEulerAngles = new Vector3(0f, 130f, 0f);
    [SerializeField] private float rotateDuration = 0.5f;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioManager audioManager;

    private Coroutine rotateRoutine;

    private void Awake()
    {
        if (targetTransform == null)
        {
            targetTransform = transform;
        }
    }

    public void RotateToTarget()
    {
        Debug.Log("[TransformRotationAction] RotateToTarget called.");

        if (targetTransform == null)
        {
            Debug.LogWarning("[TransformRotationAction] Target Transform is null.");
            return;
        }

        if (rotateRoutine != null)
        {
            StopCoroutine(rotateRoutine);
            rotateRoutine = null;
        }

        Quaternion targetRotation = Quaternion.Euler(targetLocalEulerAngles);

        PlayOpenSound();

        if (rotateDuration <= 0f)
        {
            targetTransform.localRotation = targetRotation;
            Debug.Log("[TransformRotationAction] Rotation applied instantly.");
            return;
        }

        rotateRoutine = StartCoroutine(RotateRoutine(targetRotation));
    }

    private IEnumerator RotateRoutine(Quaternion targetRotation)
    {
        Quaternion startRotation = targetTransform.localRotation;
        float elapsed = 0f;

        while (elapsed < rotateDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / rotateDuration);

            targetTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        targetTransform.localRotation = targetRotation;
        rotateRoutine = null;

        Debug.Log("[TransformRotationAction] Rotation finished.");
    }

    private void PlayOpenSound()
    {
        if (openClip == null)
        {
            return;
        }

        AudioManager manager = audioManager != null ? audioManager : AudioManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("[TransformRotationAction] AudioManager is null. Rotation will continue without sound.");
            return;
        }

        manager.PlaySFX(openClip);
    }
}