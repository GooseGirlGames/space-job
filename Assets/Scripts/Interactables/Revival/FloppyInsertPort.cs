using UnityEngine;
using Unity.Netcode;


public class FloppyInsertPort : InteractableBase {
    public NetworkVariable<NetworkObjectReference> PlayerData = new NetworkVariable<NetworkObjectReference>();
    private RevivePodAnimation m_Anim;

    void Awake() {
        m_Anim = GetComponentInParent<RevivePodAnimation>();
        if (!m_Anim) {
            Debug.LogError("No RevivePodAnimation found for " + name);
        }
    }

    public override string FriendlyName() {
        return "Floppy Drive";
    }

    private void OnPlayerDataChanged(NetworkObjectReference prev, NetworkObjectReference curr) {
        bool prev_empty = Util.NetworkObjectReferenceIsEmpty(prev);
        bool curr_empty = Util.NetworkObjectReferenceIsEmpty(curr);
        if (prev_empty && !curr_empty) {
            m_Anim.TriggerFloppyInsert();
        }
        m_Anim.SetSafetyOpen(!curr_empty);
        m_Anim.SetDoorOpen(curr_empty);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        PlayerData.OnValueChanged += OnPlayerDataChanged;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        PlayerData.OnValueChanged -= OnPlayerDataChanged;
    }

    protected override void Interaction(){
        PlayerAvatar activePlayer = PlayerManager.Instance.LocalPlayer.Avatar;
        if (PlayerAvatar.IsHolding<RevivalFloppy>()) {
            DroppableInteractable item = Util.GetDroppableInteractable(activePlayer.GetInventoryItem(PlayerAvatar.Slot.PRIMARY));
            RevivalFloppy floppy = item.GetComponent<RevivalFloppy>();
            SetPlayerReferenceServerRpc(floppy.Player.Value);
            floppy.DespawnServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReferenceServerRpc(NetworkObjectReference reference) {
        var player = Util.TryGet<PlayerAvatar>(reference);
        Debug.Log("Floppy Inserted to revive " + (player != null ? player.name : "null") + "!");
        PlayerData.Value = reference;
    }
}
