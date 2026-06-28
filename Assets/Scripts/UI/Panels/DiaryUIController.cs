using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiaryUIController : MonoBehaviour, IUIFocusCloseReceiver
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform clickArea;
    [SerializeField] private UIFocusManager focusManager;
    [SerializeField] private RawImage pageImage;
    [SerializeField] private Texture[] pages;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private AudioClip pageClip;
    [SerializeField] private AudioClip buttonClip;
    [SerializeField] private AudioManager audioManager;

    private int currentPageIndex;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (leftButton != null)
            leftButton.onClick.AddListener(ShowPreviousPage);

        if (rightButton != null)
            rightButton.onClick.AddListener(ShowNextPage);
    }

    public void Open()
    {
        currentPageIndex = 0;
        UpdatePage();
        GetFocusManager()?.OpenPanel(panelRoot, clickArea);
    }

    public void ShowPreviousPage()
    {
        if (currentPageIndex <= 0)
            return;

        currentPageIndex--;
        PlayPageSound();
        UpdatePage();
    }

    public void ShowNextPage()
    {
        if (pages == null || currentPageIndex >= pages.Length - 1)
            return;

        currentPageIndex++;
        PlayPageSound();
        UpdatePage();
    }

    public void OnFocusPanelClosed()
    {
    }

    private void UpdatePage()
    {
        if (pages != null && pages.Length > 0 && pageImage != null)
            pageImage.texture = pages[Mathf.Clamp(currentPageIndex, 0, pages.Length - 1)];

        if (leftButton != null)
            leftButton.gameObject.SetActive(currentPageIndex > 0);

        if (rightButton != null)
            rightButton.gameObject.SetActive(pages != null && currentPageIndex < pages.Length - 1);
    }

    private void PlayPageSound()
    {
        AudioManager manager = audioManager != null ? audioManager : AudioManager.Instance;
        if (manager == null)
            return;

        manager.PlaySFX(pageClip);
        manager.PlaySFX(buttonClip);
    }

    private UIFocusManager GetFocusManager()
    {
        if (focusManager != null)
            return focusManager;

        focusManager = FindObjectOfType<UIFocusManager>();
        return focusManager;
    }
}