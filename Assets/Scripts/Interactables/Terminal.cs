using TMPro;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class Terminal : TwoLevelInteractable {
    public TMP_Text Text;
    private string m_ErrorText;
    private string m_TextEntered = "";
    private bool m_LocalPlayerIsInteracting = false;
    private CameraSwap m_CameraSwap;
    public AudioClip pressSound;
    private AudioSource audioSource;

    void Awake() {
        audioSource = GetComponent<AudioSource>();    
    }

    public override void Start() {
        base.Start();
        m_CameraSwap = GetComponent<CameraSwap>();
        m_InteractionRange.OnRangeTriggerExit += SwitchAwayIfPlayer;
    }

    public static readonly Dictionary<KeyCode, string> KEYCODES = new Dictionary<KeyCode, string>{
        {KeyCode.Alpha0, "0"},
        {KeyCode.Alpha1, "1"},
        {KeyCode.Alpha2, "2"},
        {KeyCode.Alpha3, "3"},
        {KeyCode.Alpha4, "4"},
        {KeyCode.Alpha5, "5"},
        {KeyCode.Alpha6, "6"},
        {KeyCode.Alpha7, "7"},
        {KeyCode.Alpha8, "8"},
        {KeyCode.Alpha9, "9"},
        {KeyCode.A, "A"},
        {KeyCode.B, "B"},
        {KeyCode.C, "C"},
        {KeyCode.D, "D"},
        {KeyCode.E, "E"},
        {KeyCode.F, "F"},
    };

    protected override void Interaction() {
        m_LocalPlayerIsInteracting = !m_LocalPlayerIsInteracting;
        if (m_LocalPlayerIsInteracting) {
            m_CameraSwap.SwitchTo();
            if (!ShipManager.Instance.HasPower) {
                // HCI:  Only lock movement when we can interact with the terminal
                PlayerManager.Instance.LocalPlayer.Avatar.LockMovement(GetHashCode());
            }
        } else {
            PlayerManager.Instance.LocalPlayer.Avatar.ReleaseMovementLock(GetHashCode());
            m_CameraSwap.SwitchAway();
        }
        audioSource.PlayOneShot(pressSound);
    }

    public void SwitchAwayIfPlayer(Collider other) {
        if(PlayerManager.IsLocalPlayerAvatar(other)){
            m_CameraSwap.SwitchAway();
            m_LocalPlayerIsInteracting = false;
        }
    }

    public void DisplayError(string err) {
        m_ErrorText = err;
        UpdateText();
    }
    private void UpdateText() {
        Text.text = m_ErrorText;
        if (!ShipManager.Instance.HasPower) {
            Text.text += "\n>" + Value + "|";
        }
    }

    public override void Update() {
        base.Update();
        if (m_IsInArea && m_LocalPlayerIsInteracting && !ShipManager.Instance.HasPower) {
            if (Input.GetKeyDown(KeyCode.Return)) {
                SetEnteredTextServerRpc(m_TextEntered, true);
                m_TextEntered = "";
            }
            if (Input.GetKeyDown(KeyCode.Backspace)) {
                if (m_TextEntered.Length > 0) {
                    m_TextEntered = m_TextEntered.Remove(m_TextEntered.Length - 1, 1);
                    SetEnteredTextServerRpc(m_TextEntered, false);
                }
            }
            foreach(var kv in KEYCODES) {
                var keycode = kv.Key;
                if (Input.GetKeyDown(keycode)) {
                    m_TextEntered += kv.Value;
                    SetEnteredTextServerRpc(m_TextEntered, false);
                }
            }
        }
    }
}
