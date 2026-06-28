using UnityEngine;

public class DiaryInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DiaryUIController diaryUIController;
    [SerializeField] private string interactText = "[E] Read";

    public string GetInteractText()
    {
        return interactText;
    }

    public void Interact(GameObject interactor)
    {
        if (diaryUIController != null)
            diaryUIController.Open();
    }
}