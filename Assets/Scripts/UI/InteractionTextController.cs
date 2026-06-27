using TMPro;
using UnityEngine;

public class InteractionTextController : MonoBehaviour
{
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private bool hideByEmptyString = true;

    private void Start()
    {
        HideText();
    }

    public void ShowText(string message)
    {
        if (interactionText == null)
            return;

        if (!interactionText.gameObject.activeSelf)
            interactionText.gameObject.SetActive(true);

        interactionText.text = message;
    }

    public void HideText()
    {
        if (interactionText == null)
            return;

        if (hideByEmptyString)
        {
            if (!interactionText.gameObject.activeSelf)
                interactionText.gameObject.SetActive(true);

            interactionText.text = string.Empty;
        }
        else
        {
            interactionText.gameObject.SetActive(false);
        }
    }
}
