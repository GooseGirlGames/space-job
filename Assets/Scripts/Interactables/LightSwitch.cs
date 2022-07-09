using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LightSwitch : Interactable<bool> {
    public List<Light> Lights;

    [SerializeField]
    private AudioClip[] switchSounds;
    private AudioSource audioSource;

    private AudioClip GetRandomSwitchClip(){
        return switchSounds[UnityEngine.Random.Range(0, switchSounds.Length)];
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetServerRpc(bool value){
        m_State.Value = value;
    }
    protected override void Interaction(){
        SetLightConditions(!Value);
        SetServerRpc(!Value);
        AudioClip sound = GetRandomSwitchClip();
        audioSource.PlayOneShot(sound);
        Debug.Log("lightswitch");
        
    }
    
    public override void OnStateChange(bool previous, bool current){
        SetLightConditions(current);
    }

    void SetLightConditions(bool on){
        foreach(var light in Lights){
            light.gameObject.SetActive(on);
        }
        
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer){
            m_State.Value = true;
        }
    }
    private void Awake() {
        SetLightConditions(Value);
        audioSource = GetComponent<AudioSource>();
    }

    public override string FriendlyName() {
        return "Light Switch";
    }
}
