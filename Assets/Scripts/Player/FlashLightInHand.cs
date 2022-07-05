using UnityEngine;

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
        RaycastHit hit;
        /* TODO MIGRATE TO NEW INPUTSYSTEM
        Ray ray = CameraBrain.Instance.OutputCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, MAX_RAYCAST_DIST)) {
            return hit.point;
        }
        */

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
