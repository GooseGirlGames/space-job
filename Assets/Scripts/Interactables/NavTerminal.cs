using UnityEngine;
using Unity.Netcode;
using TMPro;

// Single-User Terminal
public class NavTerminal : Interactable<int> {

    public static readonly int NOT_OCCUPIED = -1;

    public TMP_Text Text;

    private CameraSwap m_CameraSwap;

    public override void Start() {
        base.Start();
        m_CameraSwap = GetComponent<CameraSwap>();
        OnStateChange(NOT_OCCUPIED, NOT_OCCUPIED);
        if (IsServer) {
            m_State.Value = NOT_OCCUPIED;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryToggleOccupiedServerRpc(int playerId) {
        Debug.Log("Hello!" + playerId);
        if (Value == NOT_OCCUPIED) {  // no one at terminal
            m_State.Value = playerId;
        } else if (Value == playerId) {  // player left terminal
            m_State.Value = NOT_OCCUPIED;
        }
        // Otherwise, someone else is at the terminal.  it will remain occupied
    }

    protected override void Interaction() {
        TryToggleOccupiedServerRpc((int)NetworkManager.Singleton.LocalClientId);
    }

    public override void OnStateChange(int prev, int current) {
        // not ready yet
        if (!PlayerManager.Instance.LocalPlayer || !PlayerManager.Instance.LocalPlayer.Avatar) {
            return;
        }

        //Debug.Log("StateChange!" + current + ShipManager.Instance.GetShipPosition());
        if (current == NOT_OCCUPIED) {
            if (prev == (int)NetworkManager.Singleton.LocalClientId) {
                PlayerManager.Instance.LocalPlayer.Avatar.ReleaseMovementLock(GetHashCode());
                m_CameraSwap.SwitchAway();
            }
        } else {
            if (current == (int)NetworkManager.Singleton.LocalClientId) {
                m_CameraSwap.SwitchTo();
                PlayerManager.Instance.LocalPlayer.Avatar.LockMovement(GetHashCode());
            }
        }
    }

    public void DisplayText(string text) {
        Text.text = text;
    }
    public override void Update() {
        base.Update();
        DisplayText("Position: " + ShipManager.Instance.GetShipPosition()+
                    "\n Speed: " + ShipManager.Instance.GetShipSpeed()+ 
                    " ,Angle: " + ShipManager.Instance.GetShipAngle()+
                    "\n Dist: " + ShipManager.Instance.GetDistantToWin());
    }
}