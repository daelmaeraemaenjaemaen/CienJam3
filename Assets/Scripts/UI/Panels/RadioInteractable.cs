using UnityEngine;

public class RadioInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private RadioUIController radioUIController;
    [SerializeField] private string interactText = "[E] Look";

    public string GetInteractText()
    {
        return interactText;
    }

    public void Interact(GameObject interactor)
    {
        if (radioUIController != null)
            radioUIController.Open();
    }
}