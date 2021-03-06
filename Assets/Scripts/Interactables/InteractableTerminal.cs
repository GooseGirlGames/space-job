using UnityEngine;

public class InteractableTerminal : RangedInteractableBase
{
    public TerminalCanvas Display;
    private Canvas m_TargetCanvas;
    private CameraSwap m_CameraSwap;
    private bool m_LocalPlayerIsInteracting = false;

    private void Awake() {
        m_CameraSwap = GetComponent<CameraSwap>();
        m_TargetCanvas = GetComponentInChildren<Canvas>();
        Display.StuffKeeper.transform.SetParent(m_TargetCanvas.transform, false);
        
    }
    // Start is called before the first frame update
    protected override void Interaction(){
        NeedsPower = !m_LocalPlayerIsInteracting;  // always allow exiting terminal
        m_LocalPlayerIsInteracting = !m_LocalPlayerIsInteracting;
        Display.SetInteractableEnable(m_LocalPlayerIsInteracting);
        GetComponent<Collider>().enabled = !m_LocalPlayerIsInteracting;
        if (m_LocalPlayerIsInteracting) {
            m_CameraSwap.SwitchTo();
            if (!ShipManager.Instance.HasPower) {
                // HCI:  Only lock movement when we can interact with the terminal
                PlayerManager.Instance.LocalPlayer.Avatar.LockMovement(GetHashCode());
            }
        } else {
            PlayerManager.Instance.LocalPlayer.Avatar.ReleaseMovementLock(GetHashCode());
            m_CameraSwap.SwitchAway();
        }
    }
}
