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
            panelRoot = gameObject;

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

        if (closeOnSolved)
            GetFocusManager()?.ClosePanel(panelRoot);

        onSolved?.Invoke();
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