using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct PlayerPos {
    public PlayerPos(Vector3 _Position, Quaternion _Rotation){
        Position = _Position;
        Rotation = _Rotation;
    }
    public Vector3 Position;
    public Quaternion Rotation;
}

[RequireComponent(typeof(NetworkObject))]
public class PlayerAvatar : NetworkBehaviour {
    [ServerRpc]
    public void UpdatePosServerRpc(PlayerPos p) {
        m_PlayerPos.Value = p;
    }
    public enum Slot {
        PRIMARY, SECONDARY
    };
    public const float GRAVITY = -10f;  //in case of zero gravity this need to change
    public Transform groundCheck;
    public Transform dropPoint;
    public Transform PrimaryItemDisplay;  // left hand
    public Transform SecondaryItemDisplay;  // back
    public LayerMask groundLayer;
    public TMP_Text nameText;
    public Material normalMaterial;
    public Material transparentMaterial;
    public Transform CameraLookAt;
    private NetworkVariable<int> m_ActiveAnimation
            = new NetworkVariable<int>(default, default, NetworkVariableWritePermission.Owner);
    private NetworkVariable<PlayerPos> m_PlayerPos
            = new NetworkVariable<PlayerPos>();
    private NetworkVariable<NetworkObjectReference> m_PrimaryItem
            = new NetworkVariable<NetworkObjectReference>(default, default, NetworkVariableWritePermission.Owner);
    private NetworkVariable<NetworkObjectReference> m_SecondaryItem
            = new NetworkVariable<NetworkObjectReference>(default, default, NetworkVariableWritePermission.Owner);

    private Room m_CurrentRoom = null;
    public Room CurrentRoom {
        get => m_CurrentRoom;
        set {
            m_CurrentRoom = value;
        }
    }

    private List<int> m_MovementLocks = new List<int>();
    private Coroutine m_SpeedBoostCoroutine = null;
    private CharacterController m_Controller;
    private float m_MovementSpeed = 3f;
    private bool m_IsGrounded = false;

    [SerializeField]
    [Range(0f,1f)]
    private float m_Health = Mathf.Clamp(1f, 0f, 1f);

    // Places where items are attached
    private Animator m_PlayerAnimator;
    private PersistentPlayer m_LocalPlayer;
    private Vector3 m_Velocity;
    private SkinnedMeshRenderer m_PlayerMesh;
    [SerializeField]
    private float m_LungCapacity = 1f;

    public void Start() {
        if (CameraLookAt == null) {
            CameraLookAt = transform;
        }
        m_Controller = GetComponent<CharacterController>();
        m_PlayerAnimator = GetComponent<Animator>();
        m_PlayerMesh = GetComponentInChildren<SkinnedMeshRenderer>();
    }

    void Update() {
        //m_PlayerAnimator.SetInteger("active_animation", m_ActiveAnimation.Value);
        OxygenRegulation(Time.deltaTime);
        if (IsClient) {
            if (IsOwner) {
                ProcessInput();
            } else {
                UpdatePos();
            }
            UpdateNameTag();
        }
    }

    public void Teleport(Transform target) {
        bool prev_controller_enabled = m_Controller.enabled;
        m_Controller.enabled = false;
        transform.SetPositionAndRotation(target.position, target.rotation);
        m_Controller.enabled = prev_controller_enabled;
    }
    public void OxygenRegulation(float delta_time){
        if (m_CurrentRoom == null) return;
        float oxygen = ((m_LungCapacity - 0.01f + (0.02f* m_CurrentRoom.RoomOxygen)));
        if(oxygen <= 0f){
            m_LungCapacity = 0f;
            //Debug.Log("Your Chocking");
        }
        else if(oxygen >= 1f){
            m_LungCapacity = 1f;
        }
        else{
            m_LungCapacity = oxygen;
            //Debug.Log(delta_time);
            //Debug.Log("palyer Oxygen:" + m_LungCapacity + "\n room: " + m_CurrentRoom.Name + ",Ox-Level: " + m_CurrentRoom.RoomOxygen);
        }
    }

    public void SetActiveAnimation(int animation_index) {
        //Debug.Log("Interaction Animation Triggered: "+ animation_index);
        m_ActiveAnimation.Value = animation_index;
        //m_PlayerAnimator.SetInteger("active_animation", animation_index);
    }

    public void OnAnimationChange(int previous, int current){
        if (!m_PlayerAnimator) {
            return;  // bye
        }
        //Debug.Log(m_LocalPlayer.PlayerName + "-> old ani: "+ previous + ", new ani:" + current);
        //m_PlayerAnimator.SetInteger("active_animation", current);
        if(current == 0){
            m_PlayerAnimator.SetTrigger("idle");
        }
        if(current == 1){
            m_PlayerAnimator.SetTrigger("walk");
        }
        if(current == 2){
            m_PlayerAnimator.SetTrigger("interact");
        }
        if(current == 3){
            m_PlayerAnimator.SetTrigger("armwave");
        }
        if(current == 4){
            m_PlayerAnimator.SetTrigger("sit");
        }
        if(current == 5){
            m_PlayerAnimator.SetTrigger("jump");
        }
        if(current == 6){
            m_PlayerAnimator.SetTrigger("drink");
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        m_PrimaryItem.OnValueChanged += OnPrimaryItemChanged;
        m_SecondaryItem.OnValueChanged += OnSecondaryItemChanged;
        m_ActiveAnimation.OnValueChanged += OnAnimationChange;
        Debug.Log("[PlayerAvatar/OnNetworkSpawn] PlayerAvatar spanwed " + name + " owned by " + OwnerClientId);
        Setup();
    }

    public void Setup() {
        // Setup for all players
        foreach (var player in FindObjectsOfType<PersistentPlayer>()) {
            if (player.OwnerClientId == OwnerClientId) {
                m_LocalPlayer = player;
                break;
            }
        }
        if (m_LocalPlayer == null) {
            Debug.LogWarning("Player object not found for '" + name + "'");
            return;
        }

        name = m_LocalPlayer.PlayerName;
        nameText.text = m_LocalPlayer.PlayerName;

        Debug.Log("Found persistentplayer object " + m_LocalPlayer.name + " for avatar " + name + " Owner? " + IsOwner);

        // Setup for local player
        if (IsOwner) {
            CameraSwap.UpdateLookAt(this);
        }

        // more setup
        OnPrimaryItemChanged(m_PrimaryItem.Value, m_PrimaryItem.Value);
        OnSecondaryItemChanged(m_SecondaryItem.Value, m_SecondaryItem.Value);
        OnAnimationChange(m_ActiveAnimation.Value, m_ActiveAnimation.Value);
    }

    void OnGUI() {
        if (IsClient) {
            //UpdateNameTag();
        }
    }
    public void PerformGroundCheck() {
        m_IsGrounded = Physics.CheckSphere(groundCheck.position,
                GroundCheck.GROUND_CHECK_RADIUS,
                groundLayer
        );
    }
    void UpdateNameTag() {
        nameText.gameObject.transform.rotation = CameraBrain.Instance.ActiveCameraTransform.rotation;
    }
    void ProcessInput() {
        PerformGroundCheck();
        if (m_IsGrounded && m_Velocity.y < 0) {
            m_Velocity.y = -2f;
        }

        if (MovementLocked) {
            return;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        var direction = new Vector3(horizontalInput, 0, verticalInput);

        var cameraDirection = CameraBrain.Instance.ActiveCameraTransform.rotation;
        direction = Vector3.Normalize(cameraDirection * direction);
        direction.y = 0;  // no flying allowed!

        m_Controller.Move(direction * Time.deltaTime * m_MovementSpeed);
        if((direction * Time.deltaTime * m_MovementSpeed) != Vector3.zero){
            m_ActiveAnimation.Value = 1;
        }
        else{
            m_ActiveAnimation.Value = 0;
        }
        m_Controller.transform.LookAt(m_Controller.transform.position + direction);

        m_Velocity.y += GRAVITY * Time.deltaTime;
        m_Controller.Move(m_Velocity * Time.deltaTime);

        var p = new PlayerPos();
        p.Position = m_Controller.transform.position;
        p.Rotation = m_Controller.transform.rotation;
        
        if (Input.GetKeyDown(KeyCode.Alpha1)){
            //armwava dance
            m_ActiveAnimation.Value = 3;
            //HidePlayer(true);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)){
            //jumpingjacks
            m_ActiveAnimation.Value = 5;
            //HidePlayer(false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)){
            //drink
            m_ActiveAnimation.Value = 6;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4)){
            //sit
            m_ActiveAnimation.Value = 4;
        }

        if (Input.GetKeyDown(KeyCode.Q)) {
            if (!HasInventorySpace(Slot.PRIMARY)) {
                m_ActiveAnimation.Value = 2;
                DropItem(Slot.PRIMARY);
            }
        }

        if (Input.GetKey(KeyCode.Alpha9)){
            if(CurrentRoom.RoomOxygen < 1f){
                CurrentRoom.RoomOxygen = CurrentRoom.RoomOxygen + 0.1f;
            }
        }

        if (Input.GetKey(KeyCode.Alpha0)){
            if(CurrentRoom.RoomOxygen > 0f){
                CurrentRoom.RoomOxygen = CurrentRoom.RoomOxygen - 0.1f;
            }
        }

        if (Input.mouseScrollDelta.y != 0) {
            // swap items
            NetworkObjectReference tmp = m_PrimaryItem.Value;
            m_PrimaryItem.Value = m_SecondaryItem.Value;
            m_SecondaryItem.Value = tmp;
            // ShowInInventory **should** be called automatically, because the networkvariable changed
        }

        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            if (!HasInventorySpace(Slot.PRIMARY)) {
                var item = GetInventoryItem(Slot.PRIMARY).GetComponentInChildren<InteractableBase>();
                if (item != null) {
                    int animation = item.SelfInteraction(this);
                    if (animation != -1) {
                        m_ActiveAnimation.Value = animation;
                    }
                }
            }
        }

        UpdatePosServerRpc(p);
    }
    void UpdatePos() {
        //m_PlayerAnimator.SetFloat("speed", 0.1f);
        transform.position = m_PlayerPos.Value.Position;
        transform.rotation = m_PlayerPos.Value.Rotation;
    }

    public void HidePlayer(bool on){
        if(on){
            m_PlayerMesh.sharedMaterial = transparentMaterial;
        }
        else{
            m_PlayerMesh.sharedMaterial = normalMaterial;
        }
    }

    private void ShowInInventory(Transform hand, NetworkObject item) {
        MeshRenderer itemRend = item.GetComponentInChildren<MeshRenderer>();
        MeshRenderer handRend = hand.GetComponent<MeshRenderer>();
        handRend.materials = itemRend.materials;
        handRend.GetComponent<MeshFilter>().mesh = itemRend.GetComponent<MeshFilter>().mesh;
        itemRend.enabled = false;

        // Make overall world scale of object in hand match the item's scale.
        float itemscale = SceneManager.GetActiveScene().name == "SampleScene" ? 0.4f: 1.0f;
        handRend.transform.localScale = Vector3.Scale(transform.localScale, item.transform.lossyScale) * itemscale;

        handRend.enabled = true;
    }
    private void HideInventorySlot(Transform hand) {
            MeshRenderer handRend = hand.GetComponent<MeshRenderer>();
            handRend.enabled = false;
    }

    // Called for both local and other players
    public void OnPrimaryItemChanged(NetworkObjectReference prev, NetworkObjectReference current) {
        if (Util.NetworkObjectReferenceIsEmpty(current)) {  // dropped item
            HideInventorySlot(PrimaryItemDisplay);
        } else {  // picked up item
            ShowInInventory(PrimaryItemDisplay, current);
        }
    }
    // Called for both local and other players
    public void OnSecondaryItemChanged(NetworkObjectReference prev, NetworkObjectReference current) {
        if (Util.NetworkObjectReferenceIsEmpty(current)) {  // dropped item
            HideInventorySlot(SecondaryItemDisplay);
        } else {  // picked up item
            ShowInInventory(SecondaryItemDisplay, current);
        }
    }

    public NetworkObject GetInventoryItem(Slot slot) {
        NetworkObjectReference reference;
        if (slot == Slot.PRIMARY) {
            reference = m_PrimaryItem.Value;
        } else {
            reference = m_SecondaryItem.Value;
        }

        NetworkObject o;
        if (reference.TryGet(out o)) {
            return o;
        }
        return null;
    }

    public bool HasInventorySpace(Slot slot) {
        return GetInventoryItem(slot) == null;
    }

    public bool HasInventorySpace() {
        return HasInventorySpace(Slot.PRIMARY) || HasInventorySpace(Slot.SECONDARY);
    }

    public void AddToInventory(NetworkObject item) {
        if (HasInventorySpace(Slot.PRIMARY)) {
            AddToInventory(Slot.PRIMARY, item);
        } else if (HasInventorySpace(Slot.SECONDARY)) {
            AddToInventory(Slot.SECONDARY, item);
        }
    }

    /*[ServerRpc(RequireOwnership=false)]
    public void PlayAnimationServerRpc(int i) {
        m_ActiveAnimation.Value = i;
        m_PlayerAnimator.SetInteger("active_animation", m_ActiveAnimation.Value);
    }*/
    public void AddToInventory(Slot slot, NetworkObject item) {
        //PlayAnimationServerRpc(2);
        if (slot == Slot.PRIMARY) {
            m_PrimaryItem.Value = item;
            ShowInInventory(PrimaryItemDisplay, item);  // optional, client-sided
        } else {
            m_SecondaryItem.Value = item;
            ShowInInventory(SecondaryItemDisplay, item);  // optional, client-sided
        }
    }

    public void DropItem(Slot slot) {
        NetworkObjectReference item;
        if (slot == Slot.PRIMARY) {
            item = m_PrimaryItem.Value;
            HideInventorySlot(PrimaryItemDisplay);
        } else {
            item = m_SecondaryItem.Value;
            HideInventorySlot(SecondaryItemDisplay);
        }

        NetworkObject o;
        if (!item.TryGet(out o)) {
            Debug.Log("No item here.");
        }

        DroppableInteractable cup = o.GetComponentInChildren<DroppableInteractable>();
        cup.DropServerRpc(dropPoint.position);

        if (slot == Slot.PRIMARY) {
            m_PrimaryItem.Value = new NetworkObjectReference();
        } else {
            m_SecondaryItem.Value = new NetworkObjectReference();
        }
    }

    public void LockMovement(int locker) {
        if (!m_MovementLocks.Contains(locker)) {
            m_MovementLocks.Add(locker);
        }
    }
    public void ReleaseMovementLock(int locker) {
        if (m_MovementLocks.Contains(locker)) {
            m_MovementLocks.Remove(locker);
        }
    }
    public bool MovementLocked {
        get => m_MovementLocks.Count > 0;
    }

    public void SpeedBoost() {
        if (m_SpeedBoostCoroutine == null) {
            m_SpeedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine());
        }
    }
    IEnumerator SpeedBoostCoroutine() {
        float speed_prev = m_MovementSpeed;
        m_MovementSpeed *= 3f;
        m_PlayerAnimator.speed = 3f;
        yield return new WaitForSeconds(4);
        m_MovementSpeed = speed_prev;
        m_PlayerAnimator.speed = 1f;
        m_SpeedBoostCoroutine = null;
    }

    public static bool IsHolding<T>() where T: DroppableInteractable {
        var primary_item = PlayerManager.Instance.LocalPlayer.Avatar.GetInventoryItem(PlayerAvatar.Slot.PRIMARY);
        return primary_item != null && primary_item.GetComponentInChildren<T>() != null;
    }
}
