using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimedReturnSceneController : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private float returnDelay = 5f;

    [Header("BGM")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private AudioClip sceneMusicClip;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
    [SerializeField] private bool loopMusic;
    [SerializeField] private bool stopCurrentBGMOnStart = true;
    [SerializeField] private bool playMusicOnStart = true;

    private Coroutine returnRoutine;

    private void Start()
    {
        AudioManager manager = GetAudioManager();

        if (stopCurrentBGMOnStart)
            manager?.StopBGM();

        if (playMusicOnStart && manager != null && sceneMusicClip != null)
        {
            manager.SetBGMVolume(musicVolume);
            manager.PlayBGM(sceneMusicClip, loopMusic);
        }

        returnRoutine = StartCoroutine(ReturnAfterDelayRoutine());
    }

    private void OnDisable()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }
    }

    public void ReturnToTitleScene()
    {
        if (string.IsNullOrWhiteSpace(titleSceneName))
            return;

        SceneManager.LoadScene(titleSceneName);
    }

    private IEnumerator ReturnAfterDelayRoutine()
    {
        float delay = Mathf.Max(0f, returnDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ReturnToTitleScene();
    }

    private AudioManager GetAudioManager()
    {
        return audioManager != null ? audioManager : AudioManager.Instance;
    }
}