using UnityEngine;
using Unity.Netcode;

public class RespawnButton : InteractableBase {

    public Transform RevivePos;

    [SerializeField]
    private FloppyInsertPort m_Port;

    public override bool PlayerCanInteract() {
        bool floppy_inserted = !Util.NetworkObjectReferenceIsEmpty(m_Port.PlayerData.Value);
        return floppy_inserted && base.PlayerCanInteract();
    }

    public override string FriendlyName() {
        return "Respawn Button";
    }

    protected override void Interaction() {
        ReviveServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReviveServerRpc() {
        // setup
        var avatar = Util.TryGet<PlayerAvatar>(m_Port.PlayerData.Value);
        if (avatar == null) {
            Debug.LogError("Tried to revive, but could not find PlayerAvatar in FloppyInsertPort.m_Port");
            return;
        }

        // revive
        avatar.GetComponent<PlayerAvatar>().ReviveServerRpc(RevivePos.position, RevivePos.rotation);

        // cleanup
        m_Port.PlayerData.Value = new NetworkObjectReference();
    }
}
