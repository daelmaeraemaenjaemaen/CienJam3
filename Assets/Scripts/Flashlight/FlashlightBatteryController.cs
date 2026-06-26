using UnityEngine;

public class FlashlightBatteryController : MonoBehaviour
{
    [SerializeField] private Light flashlightLight;
    [SerializeField] private BatteryUIController batteryUIController;

    [SerializeField] private int maxFlashBatteryCells = 5;
    [SerializeField] private int currentFlashBatteryCells = 5;
    [SerializeField] private int spareBatteryCount = 0;
    [SerializeField] private int rechargeCellsPerSpareBattery = 3;

    [SerializeField] private float secondsPerBatteryCell = 30f;
    [SerializeField] private float baseIntensity = 10f;
    [SerializeField] private float baseRange = 12f;
    [SerializeField] private float minRangeMultiplier = 0.4f;

    [SerializeField] private bool hasFlashlight = false;

    private float batteryTimer;

    private void Start()
    {
        currentFlashBatteryCells = Mathf.Clamp(currentFlashBatteryCells, 0, maxFlashBatteryCells);
        spareBatteryCount = Mathf.Max(0, spareBatteryCount);

        if (currentFlashBatteryCells <= 0)
            HandleEmptyFlashBattery();
        else if (flashlightLight != null)
            flashlightLight.enabled = true;

        UpdateLightFromBattery();
        UpdateAllUI();
    }

    private void Update()
    {
        if (currentFlashBatteryCells <= 0)
            return;

        if (flashlightLight != null && !flashlightLight.enabled)
            flashlightLight.enabled = true;

        if (!hasFlashlight)
            return;

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

        if (flashlightLight != null)
            flashlightLight.enabled = currentFlashBatteryCells > 0;

        UpdateLightFromBattery();
        UpdateAllUI();
    }

    public void AddSpareBattery(int amount)
    {
        spareBatteryCount = Mathf.Max(0, spareBatteryCount + amount);

        if (currentFlashBatteryCells <= 0)
        {
            HandleEmptyFlashBattery();
            UpdateLightFromBattery();
            UpdateAllUI();
            return;
        }

        UpdateSpareBatteryUI();
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
