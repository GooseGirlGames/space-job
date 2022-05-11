using UnityEngine;

public class MapCam : MonoBehaviour {
    // (-MaxValue, -MaxValue) => (0, 0) on map,
    // (+MaxValue, +MaxValue) => (1024, 1024) on map,
    private static readonly float m_MaxValue = 5.0f;

    [SerializeField]
    private Transform m_ShipIcon;

    private static float Convert(float map_pos) {
        float unclamped = ((map_pos - 512.0f) / 512.0f) * m_MaxValue;
        return Mathf.Clamp(unclamped, -2 * m_MaxValue, 2 * m_MaxValue);
    }

    // Update is called once per frame
    void Update() {
        if (!ShipManager.Instance) {
            return;
        }
        Vector2 ship_pos = ShipManager.Instance.GetShipPosition();
        float x = Convert(ship_pos.x);
        float y = transform.position.y;
        float z = Convert(ship_pos.y);
        transform.position = new Vector3(x, y, z);

        m_ShipIcon.localRotation = Quaternion.Euler(0, 0, ShipManager.Instance.GetShipAngle());
    }
}
