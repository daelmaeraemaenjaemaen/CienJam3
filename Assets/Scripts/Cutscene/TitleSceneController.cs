using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneController : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private Button startButton;
    [SerializeField] private string mapSceneName = "Map";

    [Header("BGM")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private AudioClip titleBgmClip;
    [SerializeField, Range(0f, 1f)] private float titleBgmVolume = 0.7f;
    [SerializeField] private bool playBgmOnStart = true;

    private void OnEnable()
    {
        ShowCursorForTitle();

        if (startButton != null)
            startButton.onClick.AddListener(LoadMapScene);
    }

    private void Start()
    {
        ShowCursorForTitle();

        if (playBgmOnStart)
            PlayTitleBGM();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            QuitGame();
    }

    private void OnDisable()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(LoadMapScene);
    }

    public void LoadMapScene()
    {
        if (string.IsNullOrWhiteSpace(mapSceneName))
            return;

        GetAudioManager()?.StopBGM();
        SceneManager.LoadScene(mapSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("TitleSceneController: Quit requested.");
#endif
        Application.Quit();
    }

    public void PlayTitleBGM()
    {
        AudioManager manager = GetAudioManager();
        if (manager == null || titleBgmClip == null)
            return;

        manager.StopBGM();
        manager.SetBGMVolume(titleBgmVolume);
        manager.PlayBGM(titleBgmClip, true);
    }

    private AudioManager GetAudioManager()
    {
        return AudioManager.Instance != null ? AudioManager.Instance : audioManager;
    }

    private void ShowCursorForTitle()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
