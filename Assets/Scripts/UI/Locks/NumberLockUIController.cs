using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NumberLockUIController : MonoBehaviour, IUIFocusCloseReceiver
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform clickArea;
    [SerializeField] private UIFocusManager focusManager;

    [Header("Digits")]
    [SerializeField] private TMP_Text[] digitTexts = new TMP_Text[4];
    [SerializeField] private Button[] upButtons = new Button[4];
    [SerializeField] private Button[] downButtons = new Button[4];
    [SerializeField] private int[] answerDigits = new int[4];
    [SerializeField] private bool resetDigitsOnOpen = true;

    [Header("Solved")]
    [SerializeField] private bool closeOnSolved = true;
    [SerializeField] private bool disableAfterSolved = true;
    [SerializeField] private UnityEvent onSolved;

    [Header("Audio")]
    [SerializeField] private AudioClip buttonPressClip;
    [SerializeField] private AudioClip solvedClip;
    [SerializeField] private AudioManager audioManager;

    private readonly int[] currentDigits = new int[4];
    private bool isSolved;

    public bool IsSolved => isSolved;

    private void Awake()
    {
        if (panelRoot == null)
        {
            Debug.LogWarning($"{GetLogPrefix()} {nameof(panelRoot)} is not assigned. Falling back to this GameObject.");
            panelRoot = gameObject;
        }

        ValidateInspectorSetup();
        WireButtons();
        UpdateDigitTexts();
    }

    public void Open()
    {
        if (isSolved && disableAfterSolved)
            return;

        if (resetDigitsOnOpen)
            ResetDigits();

        GetFocusManager()?.OpenPanel(panelRoot, clickArea);
    }

    public void ResetDigits()
    {
        for (int i = 0; i < currentDigits.Length; i++)
            currentDigits[i] = 0;

        UpdateDigitTexts();
    }

    public void IncreaseDigit(int index)
    {
        if (!IsValidIndex(index))
            return;

        currentDigits[index] = (currentDigits[index] + 1) % 10;
        OnDigitChanged();
    }

    public void DecreaseDigit(int index)
    {
        if (!IsValidIndex(index))
            return;

        currentDigits[index]--;
        if (currentDigits[index] < 0)
            currentDigits[index] = 9;

        OnDigitChanged();
    }

    public int GetDigit(int index)
    {
        return IsValidIndex(index) ? currentDigits[index] : 0;
    }

    public void OnFocusPanelClosed()
    {
    }

    private void OnDigitChanged()
    {
        PlayClip(buttonPressClip);
        UpdateDigitTexts();

        if (MatchesAnswer())
            Solve();
    }

    private void Solve()
    {
        if (isSolved)
            return;

        isSolved = true;
        PlayClip(solvedClip);

        onSolved?.Invoke();

        if (closeOnSolved)
            CloseSolvedPanel();
    }

    private void CloseSolvedPanel()
    {
        if (panelRoot == null)
        {
            Debug.LogWarning($"{GetLogPrefix()} {nameof(panelRoot)} is not assigned, so the solved panel cannot be closed.");
            return;
        }

        UIFocusManager manager = GetFocusManager();
        if (manager != null)
            manager.ClosePanel(panelRoot);
        else
            panelRoot.SetActive(false);
    }

    private bool MatchesAnswer()
    {
        if (answerDigits == null || answerDigits.Length < currentDigits.Length)
            return false;

        for (int i = 0; i < currentDigits.Length; i++)
        {
            if (currentDigits[i] != Mathf.Clamp(answerDigits[i], 0, 9))
                return false;
        }

        return true;
    }

    private void UpdateDigitTexts()
    {
        if (digitTexts == null)
            return;

        for (int i = 0; i < digitTexts.Length && i < currentDigits.Length; i++)
        {
            if (digitTexts[i] != null)
                digitTexts[i].text = currentDigits[i].ToString();
        }
    }

    private void WireButtons()
    {
        if (upButtons != null)
        {
            for (int i = 0; i < upButtons.Length; i++)
            {
                int index = i;
                if (upButtons[i] != null)
                    upButtons[i].onClick.AddListener(() => IncreaseDigit(index));
            }
        }

        if (downButtons != null)
        {
            for (int i = 0; i < downButtons.Length; i++)
            {
                int index = i;
                if (downButtons[i] != null)
                    downButtons[i].onClick.AddListener(() => DecreaseDigit(index));
            }
        }
    }

    private void ValidateInspectorSetup()
    {
        ValidateTextArray(digitTexts, nameof(digitTexts));
        ValidateButtonArray(upButtons, nameof(upButtons));
        ValidateButtonArray(downButtons, nameof(downButtons));

        if (answerDigits == null || answerDigits.Length != currentDigits.Length)
            Debug.LogWarning($"{GetLogPrefix()} {nameof(answerDigits)} should have {currentDigits.Length} entries.");
    }

    private void ValidateTextArray(TMP_Text[] texts, string fieldName)
    {
        if (texts == null)
        {
            Debug.LogWarning($"{GetLogPrefix()} {fieldName} is not assigned.");
            return;
        }

        if (texts.Length != currentDigits.Length)
            Debug.LogWarning($"{GetLogPrefix()} {fieldName} should have {currentDigits.Length} entries. Current: {texts.Length}.");

        for (int i = 0; i < texts.Length && i < currentDigits.Length; i++)
        {
            if (texts[i] == null)
                Debug.LogWarning($"{GetLogPrefix()} {fieldName}[{i}] is not assigned.");
        }
    }

    private void ValidateButtonArray(Button[] buttons, string fieldName)
    {
        if (buttons == null)
        {
            Debug.LogWarning($"{GetLogPrefix()} {fieldName} is not assigned.");
            return;
        }

        if (buttons.Length != currentDigits.Length)
            Debug.LogWarning($"{GetLogPrefix()} {fieldName} should have {currentDigits.Length} entries. Current: {buttons.Length}.");

        for (int i = 0; i < buttons.Length && i < currentDigits.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                Debug.LogWarning($"{GetLogPrefix()} {fieldName}[{i}] is not assigned.");
                continue;
            }

            if (!button.interactable)
                Debug.LogWarning($"{GetLogPrefix()} {fieldName}[{i}] button is not interactable: {button.name}");

            if (button.targetGraphic == null)
            {
                Debug.LogWarning($"{GetLogPrefix()} {fieldName}[{i}] button has no target graphic: {button.name}");
                continue;
            }

            if (!button.targetGraphic.raycastTarget)
                Debug.LogWarning($"{GetLogPrefix()} {fieldName}[{i}] target graphic raycastTarget is off: {button.name}");
        }
    }

    private string GetLogPrefix()
    {
        return $"[NumberLockUIController:{gameObject.name}]";
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < currentDigits.Length;
    }

    private void PlayClip(AudioClip clip)
    {
        AudioManager manager = audioManager != null ? audioManager : AudioManager.Instance;
        if (manager != null)
            manager.PlaySFX(clip);
    }

    private UIFocusManager GetFocusManager()
    {
        if (focusManager != null)
            return focusManager;

        focusManager = FindObjectOfType<UIFocusManager>();
        return focusManager;
    }
}
