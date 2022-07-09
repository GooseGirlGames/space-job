using UnityEngine;
using Unity.Netcode;

public class FlashLight : DroppableInteractable {

    private Light m_Beam;
    private FlashLightInHand m_InHand;

    private NetworkVariable<bool> m_TurnedOn = new NetworkVariable<bool>(true);  // TODO unused for now
    private NetworkVariable<Quaternion> m_RotationInHand = new NetworkVariable<Quaternion>();

    public override string FriendlyName() {
        return "Flashlight";
    }

    public override void Awake() {
        base.Awake();
        m_Beam = GetComponentInChildren<Light>();

        OnPickup += (PlayerAvatar avatar) => {
            bool by_local_player = avatar.OwnerClientId == NetworkManager.Singleton.LocalClientId;

            m_InHand = avatar.GetComponentInChildren<FlashLightInHand>(includeInactive: true);
            m_InHand.gameObject.SetActive(true);
            m_InHand.TrackingActive = by_local_player;
            m_InHand.ItemRenderer = avatar.PrimaryItemDisplay;
            m_Beam.enabled = false;
        };

        OnDrop += (PlayerAvatar avatar) => {
            bool by_local_player = avatar.OwnerClientId == NetworkManager.Singleton.LocalClientId;

            avatar.GetComponentInChildren<FlashLightInHand>().gameObject.SetActive(false);
            m_Beam.enabled = true;
            m_InHand = null;
        };

        OnSlotChanged += (PlayerAvatar avatar, PlayerAvatar.Slot slot) => {
            Debug.Log("Slot changed for " + name);
            m_InHand.Light.enabled = (slot == PlayerAvatar.Slot.PRIMARY);
        };
    }

    public override void OnNetworkSpawn() {
            base.OnNetworkSpawn();
            m_State.OnValueChanged += OnHolderChanged;
            m_TurnedOn.OnValueChanged += OnTurnedOnChanged;
    }

    public override void OnNetworkDespawn() {
            base.OnNetworkDespawn();
            m_State.OnValueChanged -= OnHolderChanged;
            m_TurnedOn.OnValueChanged -= OnTurnedOnChanged;
    }

    public void OnHolderChanged(int prev, int curr) {
        if (curr == DroppableInteractable.IN_WORLD) {  // dropped
            m_Beam.enabled = true;
        } else {  // picked up
            m_Beam.enabled = false;
        }
    }

    public void OnRotatationUpdated(Quaternion _, Quaternion rot) {
        if (LocalClientIsHolding()) {
            return;
        }
    }

    public bool LocalClientIsHolding() {
        return (int)NetworkManager.Singleton.LocalClientId == m_State.Value;
    }

    [ServerRpc]
    public void SetTurnedOnServerRpc(bool turned_on) {
        // TODO, for now always on
    }

    public void OnTurnedOnChanged(bool _, bool turned_on) {
        // TODO
    }

    public override PlayerAnimation SelfInteraction(PlayerAvatar avatar) {
        /* TODO
        m_TurnedOn = !m_TurnedOn;
        m_Beam.enabled = m_TurnedOn;
        AudioClip sound = GetRandomInteractionClip();
        audioSource.PlayOneShot(sound);
        */
        return PlayerAnimation.INTERACT;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetRotationInHandServerRpc(Quaternion rot) {
        m_RotationInHand.Value = rot;
    }

    public override void Update() {
        base.Update();
        if (!m_InHand) {
            return;
        }

        if (m_InHand.TrackingActive) {
            // held by local client
            SetRotationInHandServerRpc(m_InHand.ItemRenderer.transform.rotation);
        } else {
            // held by other client
            m_InHand.ItemRenderer.transform.rotation = m_RotationInHand.Value;
        }
    }
}
