using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipStatusTerminal : Interactable<Empty>
{

    private CameraSwap m_CameraSwap;
    private bool m_LocalPlayerLookingAt = false;
    public override void Start() {
        base.Start();
        m_CameraSwap = GetComponent<CameraSwap>();
        m_InteractionRange.OnRangeTriggerExit += SwitchAwayIfPlayer;
    }

    public override void OnStateChange(Empty _, Empty __) {
    }

    public void SwitchAwayIfPlayer(Collider other) {
        if(PlayerManager.IsLocalPlayerAvatar(other)){
            m_CameraSwap.SwitchAway();
            m_LocalPlayerLookingAt = false;
        }
    }

    protected override void Interaction() {
        m_LocalPlayerLookingAt = !m_LocalPlayerLookingAt;
        if (m_LocalPlayerLookingAt) {
            m_CameraSwap.SwitchTo();
        } else {
            m_CameraSwap.SwitchAway();
        }
    }
}
