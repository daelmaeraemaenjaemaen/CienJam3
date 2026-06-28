using System.Collections;
using TMPro;
using UnityEngine;

public class FlashlightBatteryController : MonoBehaviour
{
    [SerializeField] private Light flashlightLight;
    [SerializeField] private BatteryUIController batteryUIController;

    [SerializeField] private int maxFlashBatteryCells = 5;
    [SerializeField] private int currentFlashBatteryCells = 1;
    [SerializeField] private int spareBatteryCount = 0;
    [SerializeField] private int rechargeCellsPerSpareBattery = 3;
    [SerializeField] private int groundInitialBatteryCells = 1;

    [SerializeField] private float secondsPerBatteryCell = 30f;
    [SerializeField] private float baseIntensity = 10f;
    [SerializeField] private float baseRange = 12f;
    [SerializeField] private float minRangeMultiplier = 0.4f;

    [Header("Ground Flashlight Flicker")]
    [SerializeField] private bool enableGroundFlicker = true;
    [SerializeField] private float groundFlickerMinIntensity = 0.25f;
    [SerializeField] private float groundFlickerMaxIntensity = 1.3f;
    [SerializeField] private float groundFlickerChangeIntervalMin = 0.05f;
    [SerializeField] private float groundFlickerChangeIntervalMax = 0.25f;
    [SerializeField] private float groundFlickerSmoothSpeed = 12f;

    [Header("Manual Recharge")]
    [SerializeField] private KeyCode manualRechargeKey = KeyCode.R;
    [SerializeField] private bool enableManualRecharge = true;

    [Header("Recharge Hint")]
    [SerializeField] private TMP_Text rechargeHintText;
    [SerializeField] private string rechargeHintMessage = "[R] charge";
    [SerializeField] private float rechargeHintDuration = 10f;

    [SerializeField] private bool hasFlashlight = false;

    private float batteryTimer;
    private float currentGroundFlickerTargetIntensity;
    private float nextGroundFlickerChangeTime;
    private bool hasShownRechargeHint;
    private Coroutine rechargeHintCoroutine;

    private void Start()
    {
        currentGroundFlickerTargetIntensity = groundFlickerMaxIntensity;
        nextGroundFlickerChangeTime = Time.time;

        if (!hasFlashlight)
            currentFlashBatteryCells = groundInitialBatteryCells;

        currentFlashBatteryCells = Mathf.Clamp(currentFlashBatteryCells, 0, maxFlashBatteryCells);
        spareBatteryCount = Mathf.Max(0, spareBatteryCount);

        if (currentFlashBatteryCells <= 0)
            HandleEmptyFlashBattery();
        else if (flashlightLight != null)
            flashlightLight.enabled = true;

        if (batteryUIController != null)
            batteryUIController.SetBatteryUIVisible(hasFlashlight);

        if (hasFlashlight)
            UpdateLightFromBattery();
        else
            UpdateGroundFlicker();

        SetRechargeHintVisible(false);
        UpdateAllUI();
    }

    private void Update()
    {
        if (!hasFlashlight)
        {
            UpdateGroundFlicker();
            return;
        }

        if (enableManualRecharge && Input.GetKeyDown(manualRechargeKey))
            TryManualRecharge();

        if (currentFlashBatteryCells <= 0)
            return;

        if (flashlightLight != null && !flashlightLight.enabled)
            flashlightLight.enabled = true;

        batteryTimer += Time.deltaTime;

        if (batteryTimer < secondsPerBatteryCell)
            return;

        batteryTimer = 0f;
        currentFlashBatteryCells = Mathf.Max(0, currentFlashBatteryCells - 1);

        if (currentFlashBatteryCells <= 0)
            HandleEmptyFlashBattery();

        UpdateLightFromBattery();
        UpdateAllUI();
    }

    public void OnFlashlightPickedUp()
    {
        hasFlashlight = true;
        batteryTimer = 0f;
        currentFlashBatteryCells = Mathf.Clamp(currentFlashBatteryCells, 0, maxFlashBatteryCells);

        if (batteryUIController != null)
            batteryUIController.SetBatteryUIVisible(true);

        if (flashlightLight != null)
            flashlightLight.enabled = currentFlashBatteryCells > 0;

        UpdateLightFromBattery();
        UpdateAllUI();
    }

    public void AddSpareBattery(int amount)
    {
        int previousSpareBatteryCount = spareBatteryCount;
        spareBatteryCount = Mathf.Max(0, spareBatteryCount + amount);

        if (!hasShownRechargeHint && previousSpareBatteryCount <= 0 && spareBatteryCount > 0)
        {
            ShowRechargeHint();
            hasShownRechargeHint = true;
        }

        if (currentFlashBatteryCells <= 0)
        {
            HandleEmptyFlashBattery();
            UpdateLightFromBattery();
            UpdateAllUI();
            return;
        }

        UpdateSpareBatteryUI();
    }

    public bool TryManualRecharge()
    {
        if (!enableManualRecharge)
            return false;

        if (!hasFlashlight)
            return false;

        if (spareBatteryCount <= 0)
            return false;

        if (currentFlashBatteryCells >= maxFlashBatteryCells)
            return false;

        spareBatteryCount--;
        currentFlashBatteryCells = Mathf.Clamp(
            currentFlashBatteryCells + rechargeCellsPerSpareBattery,
            0,
            maxFlashBatteryCells
        );
        batteryTimer = 0f;

        if (flashlightLight != null)
            flashlightLight.enabled = currentFlashBatteryCells > 0;

        UpdateLightFromBattery();
        UpdateAllUI();
        HideRechargeHint();

        Debug.Log("Manual flashlight recharge.");
        return true;
    }

    public int GetCurrentFlashBatteryCells()
    {
        return currentFlashBatteryCells;
    }

    public int GetMaxFlashBatteryCells()
    {
        return maxFlashBatteryCells;
    }

    public int GetSpareBatteryCount()
    {
        return spareBatteryCount;
    }

    public bool TryConsumeSpareBattery(int amount)
    {
        int safeAmount = Mathf.Max(1, amount);
        if (spareBatteryCount < safeAmount)
            return false;

        spareBatteryCount -= safeAmount;
        UpdateAllUI();
        return true;
    }

    public bool HasFlashlight()
    {
        return hasFlashlight;
    }

    public bool HasFlashBattery()
    {
        return currentFlashBatteryCells > 0;
    }

    public bool CanRepelEnemy()
    {
        return hasFlashlight
            && currentFlashBatteryCells > 0
            && flashlightLight != null
            && flashlightLight.enabled;
    }

    private void HandleEmptyFlashBattery()
    {
        if (spareBatteryCount > 0)
        {
            spareBatteryCount--;
            currentFlashBatteryCells = Mathf.Clamp(rechargeCellsPerSpareBattery, 0, maxFlashBatteryCells);

            if (flashlightLight != null)
                flashlightLight.enabled = true;
        }
        else
        {
            currentFlashBatteryCells = 0;

            if (flashlightLight != null)
                flashlightLight.enabled = false;
        }
    }

    private void UpdateGroundFlicker()
    {
        if (!enableGroundFlicker)
            return;

        if (hasFlashlight)
            return;

        if (flashlightLight == null)
            return;

        if (currentFlashBatteryCells <= 0)
        {
            flashlightLight.enabled = false;
            return;
        }

        flashlightLight.enabled = true;

        if (Time.time >= nextGroundFlickerChangeTime)
        {
            currentGroundFlickerTargetIntensity = Random.Range(
                groundFlickerMinIntensity,
                groundFlickerMaxIntensity
            );

            float minInterval = Mathf.Min(groundFlickerChangeIntervalMin, groundFlickerChangeIntervalMax);
            float maxInterval = Mathf.Max(groundFlickerChangeIntervalMin, groundFlickerChangeIntervalMax);

            nextGroundFlickerChangeTime = Time.time + Random.Range(minInterval, maxInterval);
        }

        flashlightLight.intensity = Mathf.Lerp(
            flashlightLight.intensity,
            currentGroundFlickerTargetIntensity,
            Time.deltaTime * groundFlickerSmoothSpeed
        );
    }

    private void ShowRechargeHint()
    {
        if (rechargeHintText == null)
        {
            Debug.LogWarning("FlashlightBatteryController: rechargeHintText is not assigned.");
            return;
        }

        if (rechargeHintCoroutine != null)
            StopCoroutine(rechargeHintCoroutine);

        rechargeHintCoroutine = StartCoroutine(ShowRechargeHintRoutine());
    }

    private IEnumerator ShowRechargeHintRoutine()
    {
        rechargeHintText.gameObject.SetActive(true);
        rechargeHintText.text = rechargeHintMessage;

        yield return new WaitForSeconds(rechargeHintDuration);

        rechargeHintCoroutine = null;
        HideRechargeHint();
    }

    public void HideRechargeHint()
    {
        if (rechargeHintCoroutine != null)
        {
            StopCoroutine(rechargeHintCoroutine);
            rechargeHintCoroutine = null;
        }

        SetRechargeHintVisible(false);
    }

    private void SetRechargeHintVisible(bool visible)
    {
        if (rechargeHintText != null)
        {
            rechargeHintText.gameObject.SetActive(visible);

            if (!visible)
                rechargeHintText.text = string.Empty;
        }
    }

    private void UpdateLightFromBattery()
    {
        if (flashlightLight == null)
            return;

        currentFlashBatteryCells = Mathf.Clamp(currentFlashBatteryCells, 0, maxFlashBatteryCells);

        if (currentFlashBatteryCells <= 0)
        {
            flashlightLight.intensity = 0f;
            flashlightLight.range = 0f;
            flashlightLight.enabled = false;
            return;
        }

        float batteryRatio = maxFlashBatteryCells > 0
            ? (float)currentFlashBatteryCells / maxFlashBatteryCells
            : 0f;

        flashlightLight.enabled = true;
        flashlightLight.intensity = baseIntensity * batteryRatio;
        flashlightLight.range = baseRange * Mathf.Lerp(minRangeMultiplier, 1f, batteryRatio);
    }

    private void UpdateAllUI()
    {
        if (batteryUIController != null)
            batteryUIController.UpdateAll(currentFlashBatteryCells, spareBatteryCount);
    }

    private void UpdateSpareBatteryUI()
    {
        if (batteryUIController != null)
            batteryUIController.UpdateSpareBatteryUI(spareBatteryCount);
    }
}
