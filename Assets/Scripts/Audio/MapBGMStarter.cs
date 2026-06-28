using UnityEngine;

public class MapBGMStarter : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private bool playOnStart;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.7f;

    private void Start()
    {
        if (!playOnStart)
            return;

        PlayMapBGM();
    }

    public void PlayMapBGM()
    {
        AudioManager manager = audioManager != null ? audioManager : AudioManager.Instance;
        if (manager == null)
            return;

        manager.SetBGMVolume(bgmVolume);
        manager.PlayBGM(bgmClip);
    }
}
