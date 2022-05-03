using UnityEngine;
using Unity.Netcode;
using TMPro;

[System.Serializable]
public struct PlayerPos {
    public Vector3 Position;
    public Quaternion Rotation;
}

[RequireComponent(typeof(NetworkObject))]
public class PlayerAvatar : NetworkBehaviour {
    private NetworkVariable<int> m_ActiveAnimation
            = new NetworkVariable<int>(default, default, NetworkVariableWritePermission.Owner);
    private NetworkVariable<PlayerPos> m_PlayerPos
            = new NetworkVariable<PlayerPos>();
    private NetworkVariable<NetworkObjectReference> m_PrimaryItem
            = new NetworkVariable<NetworkObjectReference>(default, default, NetworkVariableWritePermission.Owner);
    private NetworkVariable<NetworkObjectReference> m_SecondaryItem
            = new NetworkVariable<NetworkObjectReference>(default, default, NetworkVariableWritePermission.Owner);
    public enum Slot {
        PRIMARY, SECONDARY
    };
    public void SetActiveAnimation(int animation_index){
        m_ActiveAnimation.Value = animation_index;
    }
    [ServerRpc]
    public void UpdatePosServerRpc(PlayerPos p) {
        m_PlayerPos.Value = p;
    }

    private CharacterController m_Controller;
    private float m_MovementSpeed = 5f;
    private bool m_IsGrounded = false;
    public Transform groundCheck;
    public Transform dropPoint;

    // Places where items are attached
    public Transform PrimaryItemDisplay;  // left hand
    public Transform SecondaryItemDisplay;  // back

    public LayerMask groundLayer;
    private Animator m_PlayerAnimator;
    public const float GRAVITY = -10f;  //in case of zero gravity this need to change
    private PersistentPlayer m_LocalPlayer;
    private Vector3 m_Velocity;

    public TMP_Text nameText;

    public void Start() {
        m_Controller = GetComponent<CharacterController>();
        m_PlayerAnimator = GetComponent<Animator>();

        foreach (var player in FindObjectsOfType<PersistentPlayer>()) {
            if (player.OwnerClientId == OwnerClientId) {
                m_LocalPlayer = player;
                break;
            }
        }
        name = nameText.text = m_LocalPlayer.PlayerName;
    }

    void Update() {
        m_PlayerAnimator.SetInteger("active_animation", m_ActiveAnimation.Value);
        if (IsClient) {
            if (IsOwner) {
                ProcessInput();
            } else {
                UpdatePos();
            }
            UpdateNameTag();
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        m_PrimaryItem.OnValueChanged += OnPrimaryItemChanged;
        m_SecondaryItem.OnValueChanged += OnSecondaryItemChanged;
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
        //m_PlayerAnimator.SetFloat("speed", 0.1f);
        PerformGroundCheck();
        if (m_IsGrounded && m_Velocity.y < 0) {
            m_Velocity.y = -2f;
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
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)){
            //jumpingjacks
            m_ActiveAnimation.Value = 5;
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
            } else if (!HasInventorySpace(Slot.SECONDARY)) {
                m_ActiveAnimation.Value = 2;
                DropItem(Slot.SECONDARY);
            }
        }

        UpdatePosServerRpc(p);
    }
    void UpdatePos() {
        //m_PlayerAnimator.SetFloat("speed", 0.1f);
        transform.position = m_PlayerPos.Value.Position;
        transform.rotation = m_PlayerPos.Value.Rotation;
    }

    private void ShowInInventory(Transform hand, NetworkObject item) {
        MeshRenderer itemRend = item.GetComponentInChildren<MeshRenderer>();
        MeshRenderer handRend = hand.GetComponent<MeshRenderer>();
        handRend.materials = itemRend.materials;
        handRend.GetComponent<MeshFilter>().mesh = itemRend.GetComponent<MeshFilter>().mesh;
        itemRend.enabled = false;

        // Make overall world scale of object in hand match the item's scale.
        handRend.transform.localScale = Vector3.Scale(transform.localScale, item.transform.lossyScale);

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

    private NetworkObject GetInventoryItem(Slot slot) {
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

        Cup cup = o.GetComponentInChildren<Cup>();
        cup.DropServerRpc(dropPoint.position);

        if (slot == Slot.PRIMARY) {
            m_PrimaryItem.Value = new NetworkObjectReference();
        } else {
            m_SecondaryItem.Value = new NetworkObjectReference();
        }
    }
}
