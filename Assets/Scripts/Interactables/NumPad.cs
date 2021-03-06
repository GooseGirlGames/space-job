using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public enum VendingMashineItem {
    METALPLATE,
    SEED,
    ENERGY_ONE,
    ENERGY_TWO,
    BALLS,
    BBG,
    SYRINGE
}

public class NumPad : TwoLevelInteractable
{
    public TMP_Text InputDisplay;
    public Transform DropPod;
    [SerializeField]
    public GameObject MetalPlate;
    public GameObject Seed;
    public GameObject EnergyOne;
    public GameObject EnergyTwo;
    public GameObject Balls;
    public GameObject Bbg;
    public GameObject Syringe;

    private int m_InCounter = 0;

    public override void Start() {
        base.Start();
        ClearInput();   
    }
    public void DisplayInputText(int input_number){
        if(m_InCounter == 1){
            ClearInput();
        }
        InputDisplay.text += input_number;
        m_InCounter ++;
    }
    public void ClearInput(){
        InputDisplay.text = ">";
        m_InCounter = 0;
    }
    public void CheckInputText(){
        Debug.Log("Here is Number:" + InputDisplay.text);
        if(InputDisplay.text == ">2"){
            Debug.Log("Here is some: MetalPlate");
            SpawnItemServerRpc(VendingMashineItem.METALPLATE);
        }
        else if(InputDisplay.text == ">3"){
            Debug.Log("Here is some: Energy1");
            SpawnItemServerRpc(VendingMashineItem.ENERGY_ONE);
        }
        else if(InputDisplay.text == ">4"){
            Debug.Log("Here is some: Energy2");
            SpawnItemServerRpc(VendingMashineItem.ENERGY_TWO);
        }
        else if(InputDisplay.text == ">1"){
            Debug.Log("Here is some: Seed");
            SpawnItemServerRpc(VendingMashineItem.SEED);
        }
        else if(InputDisplay.text == ">5"){
            Debug.Log("Here is some: Meetballs");
            SpawnItemServerRpc(VendingMashineItem.BALLS);
        }
        else if(InputDisplay.text == ">6"){
            Debug.Log("Here is some: BBG");
            SpawnItemServerRpc(VendingMashineItem.BBG);
        }
        else if(InputDisplay.text == ">7"){
            Debug.Log("Here is some: Syringe");
            SpawnItemServerRpc(VendingMashineItem.SYRINGE);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnItemServerRpc(VendingMashineItem item){
        Vector3 yeet = Vector3.Normalize(transform.forward + transform.up) * 50f;
        GameObject spawn_item = new GameObject();
        if(VendingMashineItem.METALPLATE == item){
            spawn_item = Instantiate(MetalPlate, DropPod.position, Quaternion.identity);
        }
        else if(VendingMashineItem.SEED == item){
            spawn_item = Instantiate(Seed, DropPod.position, Quaternion.identity);
        }
        else if(VendingMashineItem.ENERGY_ONE == item){
            spawn_item = Instantiate(EnergyOne, DropPod.position, Quaternion.identity);
        }
        else if(VendingMashineItem.ENERGY_TWO == item){
            spawn_item = Instantiate(EnergyTwo, DropPod.position, Quaternion.identity);
        }
        else if(VendingMashineItem.BALLS == item){
            spawn_item = Instantiate(Balls, DropPod.position, Quaternion.identity);
        }
        else if(VendingMashineItem.BBG == item){
            spawn_item = Instantiate(Bbg, DropPod.position, Quaternion.identity);
        }
        else if(VendingMashineItem.SYRINGE == item){
            spawn_item = Instantiate(Syringe, DropPod.position, Quaternion.identity);
        }
        spawn_item.GetComponent<Rigidbody>().AddForce(yeet);
        spawn_item.GetComponent<NetworkObject>().Spawn();
    }

    public override string FriendlyName() {
        return "Numpad";
    }
}
