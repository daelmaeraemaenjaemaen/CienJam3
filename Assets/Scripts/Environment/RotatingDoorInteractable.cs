using System.Collections;
using UnityEngine;

public class RotatingDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private bool useCurrentRotationAsClosed = true;
    [SerializeField] private Vector3 closedLocalEulerAngles;
    [SerializeField] private float openLocalYRotation = 145f;
    [SerializeField] private float rotateDuration = 0.5f;

    [Header("Prompt")]
    [SerializeField] private string openText = "[E] Open";
    [SerializeField] private string closeText = "[E] Close";

    [Header("Audio")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioManager audioManager;

    private bool isOpen;
    private bool isMoving;
    private Coroutine rotateRoutine;

    private void Awake()
    {
        if (doorTransform == null)
            doorTransform = transform;

        if (doorTransform != null && useCurrentRotationAsClosed)
            closedLocalEulerAngles = doorTransform.localEulerAngles;
    }

    public string GetInteractText()
    {
        return isOpen ? closeText : openText;
    }

    public void Interact(GameObject interactor)
    {
        if (doorTransform == null || isMoving)
            return;

        if (isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        SetOpenState(true);
    }

    public void Close()
    {
        SetOpenState(false);
    }

    private void SetOpenState(bool open)
    {
        if (doorTransform == null)
            return;

        if (rotateRoutine != null)
            StopCoroutine(rotateRoutine);

        Vector3 targetEuler = closedLocalEulerAngles;
        if (open)
            targetEuler.y = openLocalYRotation;

        rotateRoutine = StartCoroutine(RotateTo(targetEuler, open));
    }

    private IEnumerator RotateTo(Vector3 targetEuler, bool open)
    {
        isMoving = true;

        Quaternion startRotation = doorTransform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(targetEuler);
        float elapsed = 0f;

        PlayClip(open ? openClip : closeClip);

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = rotateDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / rotateDuration);
            doorTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        doorTransform.localRotation = targetRotation;
        isOpen = open;
        isMoving = false;
        rotateRoutine = null;
    }

    private void PlayClip(AudioClip clip)
    {
        AudioManager manager = audioManager != null ? audioManager : AudioManager.Instance;
        if (manager != null)
            manager.PlaySFX(clip);
    }
}