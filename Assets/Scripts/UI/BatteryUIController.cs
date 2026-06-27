using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BatteryUIController : MonoBehaviour
{
    [Header("Flash Battery UI")]
    [SerializeField] private RawImage flashBatteryImage;
    [SerializeField] private Texture[] flashBatteryTextures;

    [Header("Spare Battery UI")]
    [SerializeField] private TMP_Text spareBatteryNumberText;

    [Header("UI Visibility")]
    [SerializeField] private GameObject batteryIconObject;
    [SerializeField] private GameObject batteryNumberObject;
    [SerializeField] private GameObject flashBatteryObject;

    public void UpdateFlashBatteryUI(int currentCells)
    {
        if (flashBatteryImage == null || flashBatteryTextures == null || flashBatteryTextures.Length == 0)
            return;

        int textureIndex = Mathf.Clamp(currentCells, 0, flashBatteryTextures.Length - 1);
        flashBatteryImage.texture = flashBatteryTextures[textureIndex];
    }

    public void UpdateSpareBatteryUI(int spareCount)
    {
        if (spareBatteryNumberText == null)
            return;

        int safeCount = Mathf.Max(0, spareCount);
        spareBatteryNumberText.text = $"X {safeCount}";
    }

    public void UpdateAll(int currentCells, int spareCount)
    {
        UpdateFlashBatteryUI(currentCells);
        UpdateSpareBatteryUI(spareCount);
    }

    public void SetBatteryUIVisible(bool visible)
    {
        if (batteryIconObject != null)
            batteryIconObject.SetActive(visible);

        if (batteryNumberObject != null)
            batteryNumberObject.SetActive(visible);

        if (flashBatteryObject != null)
            flashBatteryObject.SetActive(visible);
    }
}
