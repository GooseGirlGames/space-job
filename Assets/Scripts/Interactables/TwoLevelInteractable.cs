using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/**
 * An interactable with child interactables.
 */
[RequireComponent(typeof(CameraSwap))]
public class TwoLevelInteractable : Interactable<int> {

    public static readonly int NOT_OCCUPIED = -1;

    // The terminal the local player is interacting with
    private static TwoLevelInteractable m_LocalPlayerInteracting;

    // Will be active iff local player is interacting with the NavTerminal
    [SerializeField]
    private List<InteractableBase> m_Interactables;

    private CameraSwap m_CameraSwap;

    private bool m_NeedsPowerInitial;

    public virtual void Awake() {
        m_CameraSwap = GetComponent<CameraSwap>();
    }

    public override void Start() {
        base.Start();
        OnStateChange(NOT_OCCUPIED, NOT_OCCUPIED);
        if (IsServer) {
            m_State.Value = NOT_OCCUPIED;
        }
        m_NeedsPowerInitial = NeedsPower;
        SetSecondaryButtonsActive(false);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        m_State.Value = NOT_OCCUPIED;
    }

    public override bool PlayerCanInteract() {
        return base.PlayerCanInteract() && !LocalPlayerIsInteracting();
    }

    public override float CooldownTime() {
        if (Value == NOT_OCCUPIED) {
            return 0.05f;
        }
        return base.CooldownTime();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TrySetOccupiedServerRpc(int playerId, bool try_occupy) {
        if (Value == NOT_OCCUPIED && try_occupy) {  // no one at terminal
            m_State.Value = playerId;
        } else if (Value == playerId && !try_occupy) {  // player left terminal
            m_State.Value = NOT_OCCUPIED;
        }
        // Otherwise, someone else is at the terminal.  it will remain occupied
    }

    protected override void Interaction() {
        int local_player = (int)NetworkManager.Singleton.LocalClientId;
        bool is_occupied_by_local_player = Value == local_player;
        TrySetOccupiedServerRpc(local_player, !is_occupied_by_local_player);
    }

    public override void OnStateChange(int prev, int current) {
        // not ready yet
        if (!PlayerManager.Instance.LocalPlayer || !PlayerManager.Instance.LocalPlayer.Avatar) {
            return;
        }

        if (current == NOT_OCCUPIED) {
            if (prev == (int)NetworkManager.Singleton.LocalClientId) { // local player left terminal
                PlayerManager.Instance.LocalPlayer.Avatar.ReleaseMovementLock(GetHashCode());
                m_CameraSwap.SwitchAway();
                NeedsPower = m_NeedsPowerInitial;
                SetSecondaryButtonsActive(false);
                TwoLevelInteractable.m_LocalPlayerInteracting = null;
                InteractableExitCanvas.Instance.SetVisible(false);
                GameManager.Instance.ControllerInput.MarkInteractableAvailable(this);
            }
        } else {
            if (current == (int)NetworkManager.Singleton.LocalClientId) { // local player entered terminal
                m_CameraSwap.SwitchTo();
                PlayerManager.Instance.LocalPlayer.Avatar.LockMovement(GetHashCode());
                NeedsPower = false;  // allow exit
                SetSecondaryButtonsActive(true);
                TwoLevelInteractable.m_LocalPlayerInteracting = this;
                InteractableExitCanvas.Instance.SetVisible(true);
                GameManager.Instance.ControllerInput.MarkInteractableUnavailable(this);
            }
        }
    }

    private void SetSecondaryButtonsActive(bool active) {
        foreach (var button in m_Interactables) {
            button.CanInteract = active;
            if (active) {
                GameManager.Instance.ControllerInput.MarkInteractableAvailable(button);
            } else {
                GameManager.Instance.ControllerInput.MarkInteractableUnavailable(button);
            }
        }
    }

    public override void Update() {
        base.Update();
        if (LocalPlayerIsInteracting()) { // local player at terminal
            Debug.Log("Player exiting " + FriendlyName() + ".");
            float interacting_for = Time.fixedTime - LastUse;  // been interacting for ... seconds
            var inputhelper = GameManager.Instance.ControllerInput;
            if (inputhelper.CancelActionUI.action.WasPerformedThisFrame()
                    || (Util.PlayerIsPressingMoveButton(/*inputhelper.MoveActionUI*/) && interacting_for > 1.0f)) {
                TryExitLocalPlayer();
            }
        }
    }

    public void TryExitLocalPlayer() {
        if (LocalPlayerIsInteracting()) {
            TrySetOccupiedServerRpc((int)NetworkManager.Singleton.LocalClientId, try_occupy: false);
        }
    }

    public static void TryExitLocalPlayerFromActive() {
        if (!m_LocalPlayerInteracting) {
            Debug.LogWarning("Local player doesn't seem to be interacting ");
            return;
        }
        m_LocalPlayerInteracting.TryExitLocalPlayer();
    }

    public bool LocalPlayerIsInteracting() {
        return m_State.Value == (int)NetworkManager.Singleton.LocalClientId;
    }
}
