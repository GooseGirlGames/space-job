using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerAnimation {
    NONE,
    ARMWAVE,
    INTERACT,
    SIT,
    JUMP,
    DRINK,
    SPRAY,
    HIT,
    SIT_IDLE,
}

public enum PlayerAminationBool {
    HOLDING_PLATE,
}

public class PlayerAnimationController : NetworkBehaviour {
    private Animator m_PlayerAnimator;
    private PlayerAvatar m_Player;

    [SerializeField]
    private InputActionReference
            m_EmoteAction1,
            m_EmoteAction2,
            m_EmoteAction3,
            m_EmoteAction4;

    private void Start() {
        m_PlayerAnimator = GetComponent<Animator>();
        m_Player = GetComponent<PlayerAvatar>();
    }

    private void TriggerAnimationLocally(PlayerAnimation animation) {
        m_PlayerAnimator.SetTrigger(animation.ToString().ToLower());
    }

    private void SetBoolLocally(PlayerAminationBool anim_bool, bool val) {
        m_PlayerAnimator.SetBool(anim_bool.ToString().ToLower(), val);
    }

    public void TriggerAnimation(PlayerAnimation animation) {
        if (!IsOwner) {
            Debug.LogWarning("Only owner should call TriggerAnimation!");
            return;
        }
        if (animation == PlayerAnimation.NONE) {
            return;
        }
        var state = m_PlayerAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0);
        var transition = m_PlayerAnimator.GetAnimatorTransitionInfo(layerIndex: 0);
        if (state.IsTag("Normal") && transition.nameHash == 0) {
            TriggerAnimationLocally(animation);
            TriggerAnimationServerRpc(animation);
        }
    }

    public void SetBool(PlayerAminationBool anim_bool, bool val) {
        if (!IsOwner) {
            Debug.LogWarning("Only owner should call SetBool!");
            return;
        }
        var state = m_PlayerAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0);
        var transition = m_PlayerAnimator.GetAnimatorTransitionInfo(layerIndex: 0);
        SetBoolLocally(anim_bool, val);
        SetBoolServerRpc(anim_bool, val);
    }

    [ClientRpc]
    private void TriggerAnimationClientRpc(PlayerAnimation animation) {
        if (IsOwner) {
            return;  // Animation already triggered locally
        }
        if (animation == PlayerAnimation.NONE) {
            return;
        }

        TriggerAnimationLocally(animation);
    }

    [ClientRpc]
    private void SetBoolClientRpc(PlayerAminationBool anim_bool, bool val) {
        if (IsOwner) {
            return;  // Animation already triggered locally
        }

        SetBoolLocally(anim_bool, val);
    }

    [ServerRpc]
    private void TriggerAnimationServerRpc(PlayerAnimation animation) {
        TriggerAnimationClientRpc(animation);
    }


    [ServerRpc]
    private void SetBoolServerRpc(PlayerAminationBool anim_bool, bool val) {
        SetBoolClientRpc(anim_bool, val);
    }

    public void OnSpeedChange(float speed){
        m_PlayerAnimator?.SetFloat("speed", speed);
    }

    void Update() {
        if (IsOwner) {
            if (m_EmoteAction1.action.WasPerformedThisFrame()) {
                TriggerAnimation(PlayerAnimation.ARMWAVE);
            }
            if (m_EmoteAction2.action.WasPerformedThisFrame()) {
                TriggerAnimation(PlayerAnimation.JUMP);
            }
            if (m_EmoteAction3.action.WasPerformedThisFrame()) {
                TriggerAnimation(PlayerAnimation.DRINK);
                
            }
            if (m_EmoteAction4.action.WasPerformedThisFrame()) {
                TriggerAnimation(PlayerAnimation.SIT);
            }
        }
    }
}
