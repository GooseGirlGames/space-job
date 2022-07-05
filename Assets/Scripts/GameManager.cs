using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

public class GameManager : MonoBehaviour {
    public static GameManager Instance = null;

    public ControllerInputHelper ControllerInput;
    [SerializeField]
    private Canvas m_LoadingScreen;
    [SerializeField]
    private TMP_Text m_LoadingScreenText;

    public InputActionReference InteractAction;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this);
        }
        DontDestroyOnLoad(this);
        ControllerInput = GetComponent<ControllerInputHelper>();
#if !UNITY_SERVER
        GetComponentInChildren<VideoPlayer>().enabled = true;
#endif
    }

    void Start() {
        NetworkManager.Singleton.OnInitialized += () => {
            NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
        };
    }

    void Update() {
        /* TODO MIGRATE TO INPUTSYSTEM
        if (Input.GetKeyDown(KeyCode.G)) {
            int n = QualitySettings.names.Length;
            int i = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel((i + 1) % n, true);
        }
        */
    }

    private void OnSceneLoad(ulong clientId, string scene, LoadSceneMode loadSceneMode, AsyncOperation op) {
        m_LoadingScreen.enabled = true;
        var ava = PlayerManager.Instance.LocalPlayer?.Avatar;
        if (ava) {
            ava.Spawned.Value = false;
        }
        StartCoroutine(LoadingScreenCoroutine(op));
    }

    private IEnumerator LoadingScreenCoroutine(AsyncOperation op) {
        while (!op.isDone) {
            float progress = Mathf.Clamp(op.progress/0.89f, 0.0f, 1.0f);
            m_LoadingScreenText.text = "Loading (" + (int)(100 * progress) + "%)";
            yield return new WaitForEndOfFrame();
        }

        m_LoadingScreenText.text = "Loading done.  Waiting for players...";
        while (!PlayerManager.Instance.AllPlayerAvatarsSpawned) {
            yield return new WaitForEndOfFrame();
        }
        m_LoadingScreen.enabled = false;
    }

}
