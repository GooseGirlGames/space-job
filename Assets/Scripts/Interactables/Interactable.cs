using UnityEngine;
using Unity.Netcode;

public abstract class Interactable<T> : InteractableBase where T : unmanaged {

    private LayerMask m_HighlightedLayer;
    private LayerMask m_DefaultLayer;
    private MeshRenderer m_Renderer;
    protected InteractionRange m_InteractionRange;
    public bool NeedsPower = false;
    public int TriggeredAnimation = 2;
    protected bool m_IsInArea = false;
    protected NetworkVariable<T> m_State = new NetworkVariable<T>();


    public virtual void Start() {
        m_HighlightedLayer = LayerMask.NameToLayer("Highlighted");
        m_Renderer = GetComponent<MeshRenderer>();
        if (!m_Renderer) {
            m_Renderer = GetComponentInChildren<MeshRenderer>();
        }
        if (!m_Renderer) {
            m_Renderer = GetComponentInParent<MeshRenderer>();
        }

        if (m_Renderer) {
            m_DefaultLayer = m_Renderer.gameObject.layer;
        } else {
            Debug.LogWarning("No renderer found for " + gameObject.name);
        }

        m_InteractionRange = GetComponent<InteractionRange>();
        if (!m_InteractionRange) {
            m_InteractionRange = GetComponentInChildren<InteractionRange>();
        }
        if (m_InteractionRange) {
            m_InteractionRange.OnRangeTriggerEnter += OnRangeTriggerEnter;
            m_InteractionRange.OnRangeTriggerExit += OnRangeTriggerExit;
        } else {
            Debug.LogWarning("No interactionrange found for " + gameObject.name);
        }
    }


    public override void OnNetworkSpawn(){
        m_State.OnValueChanged += OnStateChange;
    }
    public override void OnNetworkDespawn(){
        m_State.OnValueChanged -= OnStateChange;
    }

    private void SetHighlight(bool highlighted) {
        m_Renderer.gameObject.layer = highlighted ? m_HighlightedLayer : m_DefaultLayer;
    }

    //will be called automatically for every client when new value is given.
    public abstract void OnStateChange(T previous, T current);

    // Called by InteractionRange
    public void OnRangeTriggerEnter(Collider other) {
        PlayerAvatar player = other.GetComponent<PlayerAvatar>();
        if(player != null && player.IsOwner){
            m_IsInArea = true;
        }
    }

    // Called by InteractionRange
    public void OnRangeTriggerExit(Collider other) {
        PlayerAvatar player = other.GetComponent<PlayerAvatar>();
        if(player != null && player.IsOwner){
            m_IsInArea = false;
        }
    }

    private void OnMouseOver() {
        SetHighlight(m_IsInArea);
        if (m_IsInArea && Input.GetButtonDown("Fire1") && (ShipManager.Instance.HasPower || !NeedsPower)) {
            Interaction();
            PlayerManager.Instance.LocalPlayer.Avatar.SetActiveAnimation(TriggeredAnimation);
        }
    }
    private void OnMouseExit() {
        SetHighlight(false);
    }

    protected abstract void Interaction();

    public T Value{
        get => m_State.Value;
    }


    public virtual void Update(){  
        /*
        if(m_IsInArea && Input.GetKeyDown(KeyCode.E)){
            Interaction();
        }
        */
    }
}
