using UnityEngine;

public class MapBGMStarter : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioManager audioManager;

    private void Start()
    {
        AudioManager manager = audioManager != null ? audioManager : AudioManager.Instance;
        if (manager != null)
            manager.PlayBGM(bgmClip);
    }
}