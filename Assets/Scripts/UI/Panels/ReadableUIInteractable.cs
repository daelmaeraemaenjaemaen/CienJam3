using UnityEngine;

public class ReadableUIInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactText = "[E] Read";
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform clickArea;
    [SerializeField] private UIFocusManager focusManager;

    public string GetInteractText()
    {
        return interactText;
    }

    public void Interact(GameObject interactor)
    {
        UIFocusManager manager = GetFocusManager();
        if (manager == null || panelRoot == null)
            return;

        manager.OpenPanel(panelRoot, clickArea);
    }

    private UIFocusManager GetFocusManager()
    {
        if (focusManager != null)
            return focusManager;

        focusManager = FindObjectOfType<UIFocusManager>();
        return focusManager;
    }
}