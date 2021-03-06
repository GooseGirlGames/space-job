using UnityEngine;

public class NavTerminalConsole : MonoBehaviour {

    [SerializeField]
    private SevenSegmentDisplay m_AngleDisp;

    [SerializeField]
    private SpriteRenderer m_SpriteRendererFwd;
    [SerializeField]
    private SpriteRenderer m_SpriteRendererRev;
    [SerializeField]
    private SpriteRenderer m_SpriteRendererLeft;
    [SerializeField]
    private SpriteRenderer m_SpriteRendererRight;

    // Update is called once per frame
    void Update() {
        m_AngleDisp.DisplayNumber(ShipManager.Instance.Steering.GetAngularSpeed(), 1);
        m_SpriteRendererFwd.enabled = ShipManager.Instance.Steering.GetThrusterState(ShipSteering.Thruster.TRANSLATE_FORWARD);
        m_SpriteRendererRev.enabled = ShipManager.Instance.Steering.GetThrusterState(ShipSteering.Thruster.TRANSLATE_BACKWARD);
        m_SpriteRendererLeft.enabled = ShipManager.Instance.Steering.GetThrusterState(ShipSteering.Thruster.ROTATE_LEFT);
        m_SpriteRendererRight.enabled = ShipManager.Instance.Steering.GetThrusterState(ShipSteering.Thruster.ROTATE_RIGHT);
    }
}
