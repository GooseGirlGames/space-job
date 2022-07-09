using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Light))]
public class FlashLightInHand : MonoBehaviour {

    public static float MAX_RAYCAST_DIST = 10000f;
    public Transform ItemRenderer;  // set by FlashLight in OnPickup
    public bool TrackingActive = false;

    private Light m_Light;
    public Light Light {
        get => m_Light;
    }

    void Start() {
        m_Light = GetComponent<Light>();
    }

    Vector3? GetPointerTarget() {
        var dev = GameManager.Instance.Input.currentControlScheme;

        if (dev == "Keyboard&Mouse") {
            RaycastHit hit;
            Ray ray = CameraBrain.Instance.OutputCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, MAX_RAYCAST_DIST)) {
                return hit.point;
            }
        } else {
            // TODO aim flashlight w/ controller
        }

        return null;
    }

    void Update() {
        if (!TrackingActive) {
            return;
        }
        var target = GetPointerTarget();
        if (target.HasValue) {
            ItemRenderer.LookAt(transform.position - (target.Value - transform.position));
        }

    }

    void OnDrawGizmos() {
        if (!CameraBrain.Instance) {
            return;
        }
        var target = GetPointerTarget();
        if (target.HasValue) {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(target.Value, 0.05f);
        }
    }
}
