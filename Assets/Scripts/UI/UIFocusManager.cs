using UnityEngine;

public interface IUIFocusCloseReceiver
{
    void OnFocusPanelClosed();
}

public class UIFocusManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dotObject;
    [SerializeField] private InteractionTextController interactionTextController;

    [Header("Player Control")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private InteractionRaycaster interactionRaycaster;

    [Header("Close Rules")]
    [SerializeField] private bool closeOnEscape = true;
    [SerializeField] private bool closeOnOutsideClick = true;

    private GameObject activePanel;
    private RectTransform activeClickArea;
    private bool gameplayInputLocked;

    public bool HasOpenPanel => activePanel != null && activePanel.activeInHierarchy;
    public GameObject ActivePanel => activePanel;

    private void Update()
    {
        if (!HasOpenPanel)
            return;

        if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseActivePanel();
            return;
        }

        if (closeOnOutsideClick && Input.GetMouseButtonDown(0) && IsPointerOutsideActivePanel())
            CloseActivePanel();
    }

    public void OpenPanel(GameObject panelRoot)
    {
        OpenPanel(panelRoot, panelRoot != null ? panelRoot.GetComponent<RectTransform>() : null);
    }

    public void OpenPanel(GameObject panelRoot, RectTransform clickArea)
    {
        if (panelRoot == null)
            return;

        if (activePanel != null && activePanel != panelRoot)
            CloseActivePanel(false);

        activePanel = panelRoot;
        activeClickArea = clickArea != null ? clickArea : panelRoot.GetComponent<RectTransform>();
        panelRoot.SetActive(true);

        SetGameplayInputLocked(true, true);

        if (dotObject != null)
            dotObject.SetActive(false);

        if (interactionTextController != null)
            interactionTextController.HideText();
    }

    public void CloseActivePanel()
    {
        CloseActivePanel(true);
    }

    public void ClosePanel(GameObject panelRoot)
    {
        if (panelRoot == null)
            return;

        if (activePanel == panelRoot)
        {
            CloseActivePanel();
            return;
        }

        panelRoot.SetActive(false);
    }

    public void SetGameplayInputLocked(bool locked, bool showCursor)
    {
        gameplayInputLocked = locked;

        if (playerController != null)
            playerController.SetMovementLocked(locked);

        if (mouseLook != null)
            mouseLook.SetLookLocked(locked);

        if (interactionRaycaster != null)
            interactionRaycaster.enabled = !locked;

        Cursor.visible = showCursor;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void RestoreGameplayInput()
    {
        SetGameplayInputLocked(false, false);

        if (dotObject != null)
            dotObject.SetActive(true);
    }

    private void CloseActivePanel(bool restoreInput)
    {
        if (activePanel == null)
        {
            if (restoreInput)
                RestoreGameplayInput();

            return;
        }

        NotifyPanelClosed(activePanel);
        activePanel.SetActive(false);
        activePanel = null;
        activeClickArea = null;

        if (restoreInput)
            RestoreGameplayInput();
    }

    private bool IsPointerOutsideActivePanel()
    {
        RectTransform clickArea = activeClickArea;

        if (clickArea == null && activePanel != null)
            clickArea = activePanel.GetComponent<RectTransform>();

        if (clickArea == null)
            return false;

        return !RectTransformUtility.RectangleContainsScreenPoint(clickArea, Input.mousePosition);
    }

    private void NotifyPanelClosed(GameObject panelRoot)
    {
        MonoBehaviour[] behaviours = panelRoot.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IUIFocusCloseReceiver receiver)
                receiver.OnFocusPanelClosed();
        }
    }
}