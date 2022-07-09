using UnityEngine;

public class ShipSteeringButton : InteractableBase {
    public enum Action {
        NONE,
        TOGGLE_THRUSTER_TURN_LEFT,
        TOGGLE_THRUSTER_TURN_RIGHT,
        TARGET_SPEED_UP,
        TARGET_SPEED_DOWN,
        TARGET_SPEED_SET_STOP,
        TARGET_SPEED_SET_AHEAD_SLOW,
        TARGET_SPEED_SET_AHEAD_FULL,
        TARGET_SPEED_SET_REVERSE_SLOW,
        TARGET_SPEED_SET_REVERSE_FULL,
    }

    [SerializeField]
    private Action m_Action;

    void Awake() {
        m_Mode = Mode.HOLD_DOWN;
    }

    protected override void Interaction() {
        switch (m_Action) {
            case (Action.TOGGLE_THRUSTER_TURN_LEFT):
                ShipManager.Instance.Steering.SetThrusterStateServerRpc(ShipSteering.Thruster.ROTATE_LEFT, true);
                break;
            case (Action.TOGGLE_THRUSTER_TURN_RIGHT):
                ShipManager.Instance.Steering.SetThrusterStateServerRpc(ShipSteering.Thruster.ROTATE_RIGHT, true);
                break;
            case (Action.TARGET_SPEED_UP):
                ShipManager.Instance.Steering.ChangeTargetVelocityServerRpc(up: true);
                break;
            case (Action.TARGET_SPEED_DOWN):
                ShipManager.Instance.Steering.ChangeTargetVelocityServerRpc(up: false);
                break;
            case (Action.TARGET_SPEED_SET_REVERSE_FULL):
                ShipManager.Instance.Steering.SetTargetVelocityServerRpc(0);
                break;
            case (Action.TARGET_SPEED_SET_REVERSE_SLOW):
                ShipManager.Instance.Steering.SetTargetVelocityServerRpc(1);
                break;
            case (Action.TARGET_SPEED_SET_STOP):
                ShipManager.Instance.Steering.SetTargetVelocityServerRpc(2);
                break;
            case (Action.TARGET_SPEED_SET_AHEAD_SLOW):
                ShipManager.Instance.Steering.SetTargetVelocityServerRpc(3);
                break;
            case (Action.TARGET_SPEED_SET_AHEAD_FULL):
                ShipManager.Instance.Steering.SetTargetVelocityServerRpc(4);
                break;
            default:
                break;
        }
    }

    protected override void StopInteraction() {
        switch (m_Action) {
            case (Action.TOGGLE_THRUSTER_TURN_LEFT):
                ShipManager.Instance.Steering.SetThrusterStateServerRpc(ShipSteering.Thruster.ROTATE_LEFT, false);
                break;
            case (Action.TOGGLE_THRUSTER_TURN_RIGHT):
                ShipManager.Instance.Steering.SetThrusterStateServerRpc(ShipSteering.Thruster.ROTATE_RIGHT, false);
                break;
            default:
                break;
        }
    }

    public override string FriendlyName() {
        switch (m_Action) {
            case (Action.TOGGLE_THRUSTER_TURN_LEFT):
                return "Turn Left";
            case (Action.TOGGLE_THRUSTER_TURN_RIGHT):
                return "Turn Right";
            case (Action.TARGET_SPEED_UP):
                return "Increase Speed";
            case (Action.TARGET_SPEED_DOWN):
                return "Decrease Speed";
            default:
                return "Ship Steering";
        }
    }
}
