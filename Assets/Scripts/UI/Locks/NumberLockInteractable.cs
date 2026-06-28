using UnityEngine;

public class NumberLockInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private NumberLockUIController lockUIController;
    [SerializeField] private string interactText = "[E] Look";
    [SerializeField] private string solvedText = string.Empty;

    public string GetInteractText()
    {
        if (lockUIController != null && lockUIController.IsSolved)
            return solvedText;

        return interactText;
    }

    public void Interact(GameObject interactor)
    {
        if (lockUIController == null || lockUIController.IsSolved)
            return;

        lockUIController.Open();
    }
}