using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class CutscenePlayer : MonoBehaviour
{
    [SerializeField] private GameObject cutsceneRoot;
    [SerializeField] private RawImage cutsceneImage;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip defaultClip;
    [SerializeField] private RenderTexture targetTexture;
    [SerializeField] private UIFocusManager focusManager;
    [SerializeField] private bool lockGameplayDuringCutscene = true;
    [SerializeField] private bool showCursorDuringCutscene;
    [SerializeField] private string sceneToLoadAfterFinish;
    [SerializeField] private UnityEvent onFinished;

    private bool isPlaying;

    private void Awake()
    {
        if (cutsceneRoot == null)
            cutsceneRoot = gameObject;

        ConfigureVideoOutput();

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.loopPointReached += HandleVideoFinished;
        }

        if (cutsceneRoot != null)
            cutsceneRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= HandleVideoFinished;
    }

    public void PlayDefault()
    {
        Play(defaultClip);
    }

    public void Play(VideoClip clip)
    {
        if (videoPlayer == null || clip == null)
            return;

        isPlaying = true;

        if (cutsceneRoot != null)
            cutsceneRoot.SetActive(true);

        if (lockGameplayDuringCutscene)
            GetFocusManager()?.SetGameplayInputLocked(true, showCursorDuringCutscene);

        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;
        videoPlayer.Play();
    }

    public void StopCutscene()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        FinishCutscene();
    }

    private void HandleVideoFinished(VideoPlayer source)
    {
        FinishCutscene();
    }

    private void FinishCutscene()
    {
        if (!isPlaying)
            return;

        isPlaying = false;

        if (cutsceneRoot != null)
            cutsceneRoot.SetActive(false);

        if (lockGameplayDuringCutscene)
            GetFocusManager()?.RestoreGameplayInput();

        onFinished?.Invoke();

        if (!string.IsNullOrWhiteSpace(sceneToLoadAfterFinish))
            SceneManager.LoadScene(sceneToLoadAfterFinish);
    }

    private void ConfigureVideoOutput()
    {
        if (targetTexture == null)
            return;

        if (videoPlayer != null)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = targetTexture;
        }

        if (cutsceneImage != null)
            cutsceneImage.texture = targetTexture;
    }

    private UIFocusManager GetFocusManager()
    {
        if (focusManager != null)
            return focusManager;

        focusManager = FindObjectOfType<UIFocusManager>();
        return focusManager;
    }
}