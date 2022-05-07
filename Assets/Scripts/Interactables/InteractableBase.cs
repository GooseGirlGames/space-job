using Unity.Netcode;
using UnityEngine;

public abstract class InteractableBase : NetworkBehaviour {

    protected LayerMask m_HighlightedLayer;
    protected LayerMask m_DefaultLayer;
    protected MeshRenderer m_Renderer;
    protected InteractionRange m_InteractionRange;
    public bool NeedsPower = false;
    public int TriggeredAnimation = 2;
    protected bool m_IsInArea = false;

    // Called when item is held in hand and right mouse button pressed
    // Returns animation to play upon interaction
    public virtual int SelfInteraction(PlayerAvatar avatar) {
        return -1;
    }

    protected abstract void Interaction();

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

    protected void SetHighlight(bool highlighted) {
        m_Renderer.gameObject.layer = highlighted ? m_HighlightedLayer : m_DefaultLayer;
    }

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

    protected void OnMouseOver() {
        SetHighlight(m_IsInArea);
        if (m_IsInArea && Input.GetButtonDown("Fire1") && (ShipManager.Instance.HasPower || !NeedsPower)) {
            Interaction();
            PlayerManager.Instance.LocalPlayer.Avatar.SetActiveAnimation(TriggeredAnimation);
        }
    }
    protected void OnMouseExit() {
        SetHighlight(false);
    }

    public virtual void Update() {
    }

}
