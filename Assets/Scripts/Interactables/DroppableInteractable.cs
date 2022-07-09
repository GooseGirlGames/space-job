using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public abstract class DroppableInteractable : Interactable<int>{

    /**
     * These four will be disabled when the item is in a player's hand.
     */
    protected MeshRenderer m_Mesh;
    protected Rigidbody m_Rigidbody;
    protected List<Collider> m_AllCollider;
    protected NetworkTransform m_NetTransform;

    [Tooltip("Velocity with which the item is dropped.")]
    public float Velocity = 100;

    /**
     * m_State is either the ClientId of the player holding the item,
     * or this value if the item is not held by anyone.
     */
    public const int IN_WORLD = -1;

    [Tooltip("Position of PrimaryItemDisplay when Item is held")]
    public Vector3 PrimaryPos;
    [Tooltip("Rotation of PrimaryItemDisplay when Item is held")]
    public Vector3 PrimaryRot;
    [Tooltip("Position of SecondaryItemDisplay when Item is held")]
    public Vector3 SecondaryPos;
    [Tooltip("Rotation of SecondaryItemDisplay when Item is held")]
    public Vector3 SecondaryRot;

    /** Audio stuff */
    [SerializeField]
    protected AudioClip[] pickupSounds;
    [SerializeField]
    protected AudioClip[] interactionSounds;
    protected AudioSource audioSource;
    [SerializeField]
    protected AudioSource interactionAudioSource;
    

    /// <summary>
    /// Delegate for OnPickup and OnDrop events.
    /// </summary>
    public delegate void OnPickupDropDelegate(PlayerAvatar avatar);

    /// <summary>
    /// Delegate for OnSlotChanged event.
    /// </summary>
    public delegate void OnSlotChangedDelegate(PlayerAvatar avatar, PlayerAvatar.Slot new_slot);

    /// <summary>
    /// Called on all clients when the Item has been picked up.
    /// For local client, it is called immediateley (i.e. without waiting for the server).
    /// See <see cref="Flashlight"/> for how this may be used.
    /// </summary>
    public event OnPickupDropDelegate OnPickup;

    /// <summary>
    /// Called on all clients when the Item has been dropped.
    /// For local client, it is called immediateley (i.e. without waiting for the server).
    /// See <see cref="Flashlight"/> for how this may be used.
    /// </summary>
    public event OnPickupDropDelegate OnDrop;

    /// <summary>
    /// Called when the item is moved to another slot, i.e. when the player swapped inventory slots.
    /// Note that this is *not* called when the item is initially picked up
    /// For local client, it is called immediateley (i.e. without waiting for the server).
    /// See <see cref="Flashlight"/> for how this may be used.
    /// </summary>
    public event OnSlotChangedDelegate OnSlotChanged;

    /* ================== LIFECYCLE METHODS ================== */

    public virtual void Awake() {
        m_AllCollider = new List<Collider>(GetComponentsInParent<Collider>());
        m_AllCollider.AddRange(GetComponents<Collider>());
        m_Mesh = GetComponentInParent<MeshRenderer>();
        m_Rigidbody = GetComponentInParent<Rigidbody>();
        m_NetTransform = GetComponentInParent<NetworkTransform>();
        audioSource = GetComponent<AudioSource>(); 
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            m_State.Value = IN_WORLD;
        }
    }

    public override void OnNetworkDespawn() {
        // check if in players inventory, if true, update accordingly
        if (m_State.Value != IN_WORLD) {
            var local_ava = PlayerManager.Instance.LocalPlayer?.Avatar;
            if (local_ava) {
                foreach (var slot in Enum.GetValues(typeof(PlayerAvatar.Slot))) {
                    var item = local_ava.GetInventoryItem((PlayerAvatar.Slot) slot);
                    if (Util.GetDroppableInteractable(item) == this) {
                        Debug.Log("Player holds item about to despawn. clearing");
                        local_ava.DropItem((PlayerAvatar.Slot) slot);
                    }
                }
            }
        }

        base.OnNetworkDespawn();
    }


    /* ================== LISTENERS & EVENTS ================== */

    public override void OnStateChange(int previous, int current) {
        if (current != previous) {
            // inWorld changed, i.e. item was dropped or picked up
            UpdateWorldstate(current == IN_WORLD);
        }
    }

    public void InvokeOnPickup(PlayerAvatar avatar) {
        OnPickup?.Invoke(avatar);
    }

    public void InvokeOnDrop(PlayerAvatar avatar) {
        OnDrop?.Invoke(avatar);
    }

    public void InvokeOnSlotChange(PlayerAvatar avatar, PlayerAvatar.Slot new_slot) {
        OnSlotChanged?.Invoke(avatar, new_slot);
    }


    /* ================== REMOTE PROCEDURES (RPCs) ================== */

    [ServerRpc(RequireOwnership = false)]
    public virtual void SetHolderServerRpc(int holder_id){
        m_State.Value = holder_id;
    }

    /// <summary>
    /// Called by PlayerAvatar.DropItem, does the server-side bit of dropping an item. Note that calling this alone
    /// doesn't do everything needed to actually drop the item! Call DropItem on the Avatar instead!
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void DropServerRpc(Vector3 position) {
        m_Rigidbody.position = position;
        m_State.Value = IN_WORLD;
        m_Rigidbody.AddForce(Vector3.Normalize(PlayerManager.Instance.LocalPlayer.Avatar.transform.forward) * Velocity);

    }

    /// <summary>
    /// Despawn the item.  Note that until #55 is fixed, calling this while the item is in a player's hand is likely to
    /// cause ugly undefined behavior. https://github.com/GooseGirlGames/space-job/issues/55
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void DespawnServerRpc() {
        GetComponentInParent<NetworkObject>().Despawn(destroy: true);
    }


    /* ================== INTERACTION ================== */

    protected override void Interaction(){
        PlayerAvatar localPlayer = PlayerManager.Instance.LocalPlayer.Avatar;

        if (!localPlayer.HasInventorySpace()) {
            Debug.Log("Full of stuff");
            return;
        }

        m_IsInArea = false;
        localPlayer.AddToInventory(GetComponentInParent<NetworkObject>());
        SetHolderServerRpc((int) NetworkManager.Singleton.LocalClientId);
        AudioClip sound = GetRandomPickUpClip();
        if (sound && audioSource) {
            audioSource.PlayOneShot(sound);
        }
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

    protected override bool PlayerCanInteract() {
        return base.PlayerCanInteract() && Value == IN_WORLD;
    }


    /* ================== AUDIO ================== */

    protected AudioClip GetRandomInteractionClip(){
        return interactionSounds[UnityEngine.Random.Range(0, interactionSounds.Length)];
    }

    protected AudioClip GetRandomPickUpClip(){
        return pickupSounds[UnityEngine.Random.Range(0, pickupSounds.Length)];
    }

}
