using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;

public class CameraBrain : MonoBehaviour {

    [SerializeField]
    private ScriptableRendererFeature m_WireframeRendererFeature;
    [SerializeField]
    private Material m_SkyboxWireframe;
    [SerializeField]
    private Material m_SkyboxDefault;
    [SerializeField]
    private ForwardRendererData m_RendererData;
    [SerializeField]
    private LayerMask m_DefaultOpaqueLayerMask;
    [SerializeField]
    private LayerMask m_WireframeOpaqueLayerMask;

    private Skybox m_Skybox;
    private bool m_IsBlending;
    private bool m_WireframeOnBlend = true;

    private Coroutine m_ShakeCoroutine = null;

    public CinemachineBlendDefinition BlendBetweenRooms;
    public CinemachineBlendDefinition BlendWithinRoom;
    private Blend m_CurrentBlend;
    public enum Blend { BETWEEN_ROOMS, WITHIN_ROOM };

    public bool WireframeOnBlend {
        get { return m_WireframeOnBlend; }
        set { m_WireframeOnBlend = value; }
    }

    public static CameraBrain Instance;
    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this);
        }

        m_Skybox = GetComponent<Skybox>();
    }


    public CinemachineBrain Brain;

    public ICinemachineCamera ActiveCamera {
        get {
            if (Brain.ActiveVirtualCamera != null) {
                return Brain.ActiveVirtualCamera;
            } else {
                return null;
            }
        }
    }

    public Camera OutputCamera {
        get => Brain.OutputCamera;
    }

    public GameObject ActiveCameraObject {
        get {
            if (Brain.ActiveVirtualCamera != null) {
                return Brain.ActiveVirtualCamera.VirtualCameraGameObject;
            } else {
                return null;
            }
        }
    }
    public Transform ActiveCameraTransform {
        get {
            GameObject cam = ActiveCameraObject;
            if (cam != null) {
                return ActiveCameraObject.transform;
            } else {
                return gameObject.transform;  // whatever
            }
        }
    }

    public void LookAt(Transform target) {
        Brain.ActiveVirtualCamera.LookAt = target;
    }

    private void SetWireframe(bool wireframe_on) {
        m_WireframeRendererFeature.SetActive(wireframe_on);
        m_RendererData.opaqueLayerMask = wireframe_on ? m_WireframeOpaqueLayerMask : m_DefaultOpaqueLayerMask;
        RenderSettings.skybox = wireframe_on ? m_SkyboxWireframe : m_SkyboxDefault;
    }

    public void SetBlendOverride(Blend blendType) {
        CinemachineBlendDefinition blend = blendType == Blend.WITHIN_ROOM ? BlendWithinRoom : BlendBetweenRooms;
        CinemachineCore.GetBlendOverride = (cam_from, cam_to, defaultBlend, owner) => {
            return blend;
        };
        m_CurrentBlend = blendType;
    }

    public void SetNoiseParameters(float amplitude, float frequency = 6f) {
        foreach (var cam in CameraSwap.Instances) {
            cam.SetNoiseParameters(amplitude, frequency);
        }
    }

    private IEnumerator QuickShakeCoroutine(float duration, float max_amplitude, int steps) {
        for (float t = 0f; t <= 1f; t += 1f/(float)steps) {
            float amp = max_amplitude * Mathf.Sqrt(4f * t * (1f - t));
            amp = Mathf.Clamp(amp, 0f, max_amplitude);
            SetNoiseParameters(amp);
            yield return new WaitForSeconds(duration/steps);
        }
        SetNoiseParameters(0);
        m_ShakeCoroutine = null;
    }

    public void QuickShake(float duration, float max_amplitude = 1f, int steps = 40) {
        if (m_ShakeCoroutine != null) {
            return;
        }
        m_ShakeCoroutine = StartCoroutine(QuickShakeCoroutine(duration, max_amplitude, steps));
    }

    void Update() {
        bool blending = Brain.IsBlending;
        if (blending != m_IsBlending) {
            if (m_WireframeOnBlend || !blending) {  // wireframe enabled, or we've just stopped blending
                SetWireframe(blending);
            }

            if (blending && m_CurrentBlend == Blend.BETWEEN_ROOMS) {
                PlayerManager.Instance.LocalPlayer.Avatar.LockMovement(GetHashCode());
            } else {
                PlayerManager.Instance.LocalPlayer.Avatar.ReleaseMovementLock(GetHashCode());
            }

            m_IsBlending = blending;
        }

        /* TODO MIGRATE TO INPUTSYSTEM
        if (Input.GetKeyDown(KeyCode.C)) {
            RaycastHit hit;
            var ray = OutputCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit)) {
                Debug.Log(hit.transform.name);
            }
        }
        */
    }

    void OnDestroy() {
        // Don't get stuck in Wireframe mode when stopping the game in the Editor while Wireframe is active
        SetWireframe(false);
    }

}
