using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/**
  * Mjam, that's some good coffee~

  Feckn delicious shit, oh no..
  */

public class CoffeCup : DroppableInteractable{
    private Int32 m_CupMaterialPattern = ~0x42CAFE43;
    private static int m_CupNumber = 0;
    public List<Material> Materials = new List<Material>();
    private bool m_PickedUp = false;  // Server-Only
    [SerializeField]
    private AudioClip[] gulpSounds;

    public override void Awake() {
        base.Awake();
        int mat_idx = (m_CupMaterialPattern >> (m_CupNumber++ % 32)) & 1;
        GetComponent<MeshRenderer>().material = Materials[mat_idx];  
    }

    public override string FriendlyName() {
        return "Cup of Coffee";
    }

    private AudioClip GetRandomGulpClip(){
        return gulpSounds[UnityEngine.Random.Range(0, gulpSounds.Length)];
    }
    IEnumerator WaitForMouth(float waitTime){
        yield return new WaitForSeconds(waitTime);
        AudioClip sound = GetRandomGulpClip();
        audioSource.PlayOneShot(sound);
        Debug.Log("gulp");
    }
    public override PlayerAnimation SelfInteraction(PlayerAvatar avatar) {
        avatar.SpeedBoost();
        StartCoroutine(WaitForMouth(0.5f));
        return PlayerAnimation.DRINK;
        
    }

    [ServerRpc(RequireOwnership = false)]
    public override void SetHolderServerRpc(int holder_id) {
        base.SetHolderServerRpc(holder_id);
        m_PickedUp = true;

    }

    public bool IsPickedUp() {
        return m_PickedUp;
    }
}
