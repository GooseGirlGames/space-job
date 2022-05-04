using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

/**
  * Mjam, that's some good coffee~

  Feckn delicious shit, oh no..
  */

public class Cup : Interactable<int> {
    private MeshRenderer m_Mesh;
    private Rigidbody m_Rigidbody;
    private List<Collider> m_AllCollider;
    private NetworkTransform m_NetTransform;
    public const int IN_WORLD = -1;

    // Cup-Specific
    private Int32 m_CupMaterialPattern = ~0x42CAFE43;
    private static int m_CupNumber = 0;
    public List<Material> Materials = new List<Material>();

    public bool pickedUp = false;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            m_State.Value = IN_WORLD;
        }
    }

    protected override void Interaction() {
        /*
        if (m_State.Value != IN_WORLD) {
            return;
        }
        */

        PlayerAvatar localPlayer = PlayerManager.Instance.LocalPlayer.Avatar;

        if (!localPlayer.HasInventorySpace()) {
            Debug.Log("Full of stuff");
            return;
        }

        m_IsInArea = false;
        localPlayer.AddToInventory(GetComponentInParent<NetworkObject>());

        SetServerRpc((int) NetworkManager.Singleton.LocalClientId); // weardes casting thing
    }

    public override int SelfInteraction(PlayerAvatar avatar) {
        avatar.SpeedBoost();
        return 6;
    }

    public override void OnStateChange(int previous, int current){

        if (current != previous) {
            // inWorld changed, i.e. item was dropped or
            // picked up
            UpdateWorldstate(current == IN_WORLD);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetServerRpc(int value){
        pickedUp = true;
        m_State.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DropServerRpc(Vector3 position) {
        GetComponentInParent<Rigidbody>().position = position;
        m_State.Value = IN_WORLD;
    }

    private void Awake() {
        m_AllCollider = new List<Collider>(GetComponentsInParent<Collider>());
        m_AllCollider.AddRange(GetComponents<Collider>());
        m_Mesh = GetComponentInParent<MeshRenderer>();
        m_Rigidbody = GetComponentInParent<Rigidbody>();
        m_NetTransform = GetComponentInParent<NetworkTransform>();

        int mat_idx = (m_CupMaterialPattern >> (m_CupNumber++ % 32)) & 1;
        GetComponent<MeshRenderer>().material = Materials[mat_idx];
    }

    public void UpdateWorldstate(bool inWorld) {
        foreach(Collider colli in m_AllCollider){
            colli.enabled = inWorld;
        }
        if (inWorld) {
            // Disabling the renderer after picking the item up is done by the
            // playeravatar, because it needs access to the renderer to steal
            // its mesh and materials
            m_Mesh.enabled = true;
        }
        m_Rigidbody.isKinematic = !inWorld; // TODO does this work? 
        m_NetTransform.enabled = inWorld;
    }

    public bool isPickedUp() {
        return pickedUp;
    }

}
