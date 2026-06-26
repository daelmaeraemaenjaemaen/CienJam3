using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BatteryUIController : MonoBehaviour
{
    [SerializeField] private Image flashBatteryImage;
    [SerializeField] private Sprite[] flashBatterySprites;
    [SerializeField] private TMP_Text spareBatteryNumberText;

    public void UpdateFlashBatteryUI(int currentCells)
    {
        if (flashBatteryImage == null || flashBatterySprites == null || flashBatterySprites.Length == 0)
            return;

        int spriteIndex = Mathf.Clamp(currentCells, 0, flashBatterySprites.Length - 1);
        flashBatteryImage.sprite = flashBatterySprites[spriteIndex];
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
}
