using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RadioUIController : MonoBehaviour, IUIFocusCloseReceiver
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform clickArea;
    [SerializeField] private UIFocusManager focusManager;
    [SerializeField] private TMP_Text radioNumberText;

    [Header("Buttons")]
    [SerializeField] private Button button0;
    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    [SerializeField] private Button button3;
    [SerializeField] private Button button4;
    [SerializeField] private Button button5;
    [SerializeField] private Button button6;
    [SerializeField] private Button button7;
    [SerializeField] private Button button8;
    [SerializeField] private Button button9;
    [SerializeField] private Button dotButton;

    [Header("Audio")]
    [SerializeField] private AudioSource radioAudioSource;
    [SerializeField] private AudioClip happyCienSongClip;
    [SerializeField] private AudioClip channelFourClip;
    [SerializeField] private AudioClip staticClip;
    [SerializeField] private AudioClip buttonPressClip;
    [SerializeField, Range(0f, 1f)] private float radioVolume = 0.5f;
    [SerializeField] private AudioManager audioManager;

    [Header("Input")]
    [SerializeField] private int maxInputLength = 5;

    private string currentInput = string.Empty;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        WireButtons();
    }

    public void Open()
    {
        currentInput = string.Empty;
        UpdateText();
        StopRadioSound();
        GetFocusManager()?.OpenPanel(panelRoot, clickArea);
    }

    public void AppendDigit(int digit)
    {
        AppendText(Mathf.Clamp(digit, 0, 9).ToString());
    }

    public void AppendDot()
    {
        AppendText(".");
    }

    public void OnFocusPanelClosed()
    {
        StopRadioSound();
        currentInput = string.Empty;
        UpdateText();
    }

    private void AppendText(string value)
    {
        if (string.IsNullOrEmpty(value) || currentInput.Length >= maxInputLength)
            return;

        currentInput += value;
        UpdateText();
        PlayButtonSound();
        UpdateRadioSound();
    }

    private void UpdateText()
    {
        if (radioNumberText != null)
            radioNumberText.text = currentInput;
    }

    private void UpdateRadioSound()
    {
        if (radioAudioSource == null)
            return;

        AudioClip nextClip = null;

        if (string.IsNullOrWhiteSpace(currentInput))
        {
            StopRadioSound();
            return;
        }

        if (currentInput == "91.9")
            nextClip = happyCienSongClip;
        else if (currentInput == "96.1")
            nextClip = channelFourClip;
        else
            nextClip = staticClip;

        if (nextClip == null)
        {
            StopRadioSound();
            return;
        }

        if (radioAudioSource.clip == nextClip && radioAudioSource.isPlaying)
            return;

        radioAudioSource.Stop();
        radioAudioSource.clip = nextClip;
        radioAudioSource.loop = true;
        radioAudioSource.volume = radioVolume;
        radioAudioSource.Play();
    }

    private void StopRadioSound()
    {
        if (radioAudioSource != null)
            radioAudioSource.Stop();
    }

    private void PlayButtonSound()
    {
        AudioManager manager = audioManager != null ? audioManager : AudioManager.Instance;
        if (manager != null)
            manager.PlaySFX(buttonPressClip);
    }

    private void WireButtons()
    {
        AddDigitListener(button0, 0);
        AddDigitListener(button1, 1);
        AddDigitListener(button2, 2);
        AddDigitListener(button3, 3);
        AddDigitListener(button4, 4);
        AddDigitListener(button5, 5);
        AddDigitListener(button6, 6);
        AddDigitListener(button7, 7);
        AddDigitListener(button8, 8);
        AddDigitListener(button9, 9);

        if (dotButton != null)
            dotButton.onClick.AddListener(AppendDot);
    }

    private void AddDigitListener(Button button, int digit)
    {
        if (button != null)
            button.onClick.AddListener(() => AppendDigit(digit));
    }

    private UIFocusManager GetFocusManager()
    {
        if (focusManager != null)
            return focusManager;

        focusManager = FindObjectOfType<UIFocusManager>();
        return focusManager;
    }
}