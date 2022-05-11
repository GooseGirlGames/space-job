using UnityEngine;

public class MapCam : MonoBehaviour {
    // (-MaxValue, -MaxValue) => (0, 0) on map,
    // (+MaxValue, +MaxValue) => (1024, 1024) on map,
    private static readonly float m_MaxValue = 5.0f;

    [SerializeField]
    private Transform m_ShipIcon;
    [SerializeField]
    private Transform m_FlagIcon;
    [SerializeField]
    private Transform m_DirectionIcon;

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

        UpdateGoal();
    }

    public static float Angle(Vector2 vec) {
        if (vec.y >= 0) return Mathf.Acos(vec.x/vec.magnitude);
        return -Mathf.Acos(vec.x/vec.magnitude);
    }

    void UpdateGoal() {
        Vector2 goal_pos = ShipManager.Instance.GetGoal();
        float x = Convert(goal_pos.x);
        float y = m_ShipIcon.transform.position.y;
        float z = Convert(goal_pos.y);

        m_FlagIcon.position = new Vector3(x, y, z);

        Vector2 target_direction = (ShipManager.Instance.GetGoal() - ShipManager.Instance.GetShipPosition());
        target_direction.Normalize();

        float angle_rad = Angle(target_direction);
        m_DirectionIcon.localRotation = Quaternion.Euler(0, 0, angle_rad * 180f/Mathf.PI);
    }
}
