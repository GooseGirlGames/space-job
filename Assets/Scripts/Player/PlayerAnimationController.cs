using UnityEngine;
using Unity.Netcode;

public enum PlayerAnimation {
    NONE,
    ARMWAVE,
    INTERACT,
    SIT,
    JUMP,
    DRINK,
    SPRAY,
    PLATE_HOLD,
    HIT,
    SIT_IDLE,
}

public class PlayerAnimationController : NetworkBehaviour {
    private Animator m_PlayerAnimator;
    private PlayerAvatar m_Player;

    private void Start() {
        m_PlayerAnimator = GetComponent<Animator>();
        m_Player = GetComponent<PlayerAvatar>();
    }

    private void TriggerAnimationLocally(PlayerAnimation animation) {
        m_PlayerAnimator.SetTrigger(animation.ToString().ToLower());
    }

    public void TriggerAnimation(PlayerAnimation animation) {
        if (!IsOwner) {
            Debug.LogWarning("Only owner should call TriggerAnimation!");
            return;
        }
        var state = m_PlayerAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0);
        var transition = m_PlayerAnimator.GetAnimatorTransitionInfo(layerIndex: 0);
        if (state.IsTag("Normal") && transition.nameHash == 0) {
            TriggerAnimationLocally(animation);
            TriggerAnimationServerRpc(animation);
        }
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

    [ServerRpc]
    private void TriggerAnimationServerRpc(PlayerAnimation animation) {
        TriggerAnimationClientRpc(animation);
    }

    public void OnSpeedChange(float speed){
        m_PlayerAnimator?.SetFloat("speed", speed);
    }

    void Update() {
        if (IsOwner) {
            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                TriggerAnimation(PlayerAnimation.ARMWAVE);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2)) {
                TriggerAnimation(PlayerAnimation.JUMP);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3)) {
                TriggerAnimation(PlayerAnimation.DRINK);
                
            }
            if (Input.GetKeyDown(KeyCode.Alpha4)) {
                TriggerAnimation(PlayerAnimation.SIT);
            }
        }
    }
}
