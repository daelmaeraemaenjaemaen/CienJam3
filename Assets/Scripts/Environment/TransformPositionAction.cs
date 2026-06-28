using System.Collections;
using UnityEngine;

public class TransformPositionAction : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private bool useCurrentAsClosedPosition = true;
    [SerializeField] private Vector3 closedLocalPosition;
    [SerializeField] private Vector3 openLocalPosition;
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioManager audioManager;

    private Coroutine moveRoutine;
    private bool isOpen;

    private void Awake()
    {
        if (targetTransform == null)
            targetTransform = transform;

        if (targetTransform != null && useCurrentAsClosedPosition)
            closedLocalPosition = targetTransform.localPosition;
    }

    public void Open()
    {
        MoveTo(openLocalPosition, true);
    }

    public void Close()
    {
        MoveTo(closedLocalPosition, false);
    }

    public void SetOpenPosition(Vector3 localPosition)
    {
        openLocalPosition = localPosition;
    }

    private void MoveTo(Vector3 targetPosition, bool open)
    {
        if (targetTransform == null)
            return;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveRoutine(targetPosition, open));
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition, bool open)
    {
        Vector3 startPosition = targetTransform.localPosition;
        float elapsed = 0f;

        if (open)
            PlayClip(openClip);

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / moveDuration);
            targetTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        targetTransform.localPosition = targetPosition;
        isOpen = open;
        moveRoutine = null;
    }

    private void PlayClip(AudioClip clip)
    {
        AudioManager manager = audioManager != null ? audioManager : AudioManager.Instance;
        if (manager != null)
            manager.PlaySFX(clip);
    }
}