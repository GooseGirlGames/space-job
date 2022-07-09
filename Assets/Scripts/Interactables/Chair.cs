using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chair : Interactable<bool>{
    public Transform TeleportPoint;
    [SerializeField]
    private AudioClip[] sitSounds;
    private AudioSource audioSource;

    void Awake() {
        audioSource = GetComponent<AudioSource>();    
    }

    public override string FriendlyName() {
        return "Chair";
    }

    private AudioClip GetRandomSitClip(){
        return sitSounds[UnityEngine.Random.Range(0, sitSounds.Length)];
    }
    public override void OnStateChange(bool previous, bool current){}
    protected override void Interaction(){
        PlayerManager.Instance.LocalPlayer.Avatar.Teleport(TeleportPoint);
        AudioClip sound = GetRandomSitClip();
        audioSource.PlayOneShot(sound);
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer){
            m_State.Value = true;
        }
    }
}
