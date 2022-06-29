using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

public class GameManager : MonoBehaviour {
    public static GameManager Instance = null;

    [SerializeField]
    private Canvas m_LoadingScreen;
    [SerializeField]
    private TMP_Text m_LoadingScreenText;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this);
        }
        DontDestroyOnLoad(this);
    }

    void Start() {
        NetworkManager.Singleton.OnInitialized += () => {
            NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
        };
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.G)) {
            int n = QualitySettings.names.Length;
            int i = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel((i + 1) % n, true);
        }
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
            m_LoadingScreenText.text = "Loading (" + (int)(100 * op.progress) + "%)";
            yield return new WaitForEndOfFrame();
        }

        m_LoadingScreenText.text = "Loading done.  Waiting for players...";
        while (!PlayerManager.Instance.AllPlayerAvatarsSpawned) {
            yield return new WaitForEndOfFrame();
        }
        m_LoadingScreen.enabled = false;
    }

}
