using UnityEngine;
using UnityEngine.UI;

public class ChaseDangerEffectController : MonoBehaviour
{
    [SerializeField] private RawImage redEffect;

    [SerializeField] private float blinkSpeed = 4f;
    [SerializeField] private float minAlpha = 0.05f;
    [SerializeField] private float maxAlpha = 0.18f;

    private bool isActive;

    private void Start()
    {
        StopDangerEffect();
    }

    private void Update()
    {
        if (!isActive || redEffect == null)
            return;

        float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        SetRedEffectAlpha(alpha);
    }

    public void StartDangerEffect()
    {
        isActive = true;
    }

    public void StopDangerEffect()
    {
        isActive = false;
        SetRedEffectAlpha(0f);
    }

    private void SetRedEffectAlpha(float alpha)
    {
        if (redEffect == null)
            return;

        Color color = redEffect.color;
        color.a = alpha;
        redEffect.color = color;
    }
}
