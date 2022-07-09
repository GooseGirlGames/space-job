using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class Plant : Interactable<bool> {
    [SerializeField]
    private TMP_Text plantStatus;
    private NetworkObject startingSeed;
    [SerializeField]
    private AudioClip plantSound;
    private AudioSource audioSource;
    [SerializeField]
    protected GameObject[] plantList;
    [SerializeField]
    protected Transform seedPlantedTrans;
    [SerializeField]
    protected Transform seedNotPlantedTrans;
    private GameObject seed;
    private GameObject plant;
    private GameObject deadPlant;
    private GameObject dryPlant;
/*     private NetworkObject seed;
 */ public NetworkVariable<int> m_mesh = new NetworkVariable<int>(1);
    public NetworkVariable<bool> notPlanted = new NetworkVariable<bool>(true);
    public NetworkVariable<bool> grown = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> seedPlanted = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> watered = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> dry = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> dead = new NetworkVariable<bool>(false);

    public override string FriendlyName() {
        return "Plant";
    }

    void Awake() {
        seed = plantList[0];
        plant = plantList[1];
        dryPlant = plantList[2];
        deadPlant = plantList[3];
        seed.SetActive(false);
        plant.SetActive(false);
        dryPlant.SetActive(false);
        deadPlant.SetActive(false);
        audioSource = GetComponent<AudioSource>();    
    }
    public override void Update() {
        base.Update();
    }
    protected override void Interaction(){
        SetServerRpc(!Value);
        if (PlayerAvatar.IsHolding<Seed>() && FindObjectOfType<Seed>() != null) {
        
            startingSeed = PlayerManager.Instance.LocalPlayer.Avatar.GetInventoryItem(PlayerAvatar.Slot.PRIMARY); 
            PlantingServerRpc();
            /*  audioSource.PlayOneShot(plantSound); */
        }
        if (PlayerAvatar.IsHolding<WateringCan>()){
            if(seedPlanted.Value){
                WaterPlantServerRpc(true);
                Debug.Log("plant has been watered");   
            } 
        }
        if (startingSeed != null) {
            DespawnServerRpc();
        } 
        if (dead.Value) {
            PlantDeadServerRpc();
        }
        
    }

    public override void OnStateChange(bool previous, bool current) {
        if(seedPlanted.Value && (!notPlanted.Value) ){
            if (!grown.Value && watered.Value){
                GrowPlantServerRpc();
            }
        } 
        if (dry.Value && (!watered.Value)){
            PlantDyingServerRpc();
        }
        
        ChangePlantServerRpc();
        
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetServerRpc(bool value){
        m_State.Value = value;
    }

    IEnumerator WaitForPlantGrow(float maxtime){
        float growIn = UnityEngine.Random.Range(5, maxtime);
        Debug.Log("plant grows in " + growIn + "sec");
        yield return new WaitForSeconds(growIn);
        grown.Value = true;

    }
    IEnumerator TimeTillPlantDry(float maxtime){
        float dryIn = UnityEngine.Random.Range(120, maxtime);
        Debug.Log("plant ist dry in " + dryIn + "secs");
        yield return new WaitForSeconds(dryIn);
        watered.Value = false;
        dry.Value = true;
    }
    IEnumerator TimeTillPlantDead(float maxtime){
        float deadIn = UnityEngine.Random.Range(200, maxtime);
        Debug.Log("plant is dead in " + deadIn + " secs");
        yield return new WaitForSeconds(deadIn);
        dead.Value = true;
    }

   [ServerRpc(RequireOwnership = false)]
    public void DespawnServerRpc() {
        startingSeed.GetComponentInParent<NetworkObject>().Despawn(destroy: true);   
    }
    [ServerRpc(RequireOwnership = false)]
    public void GrowPlantServerRpc() {
        Debug.Log("growing seed");
        if (!dry.Value && watered.Value) {
            StartCoroutine(WaitForPlantGrow(10));
        } else StopCoroutine(WaitForPlantGrow(10));
        
    }
    [ServerRpc(RequireOwnership = false)]
    public void PlantingServerRpc() {
        seedPlanted.Value = true;
        notPlanted.Value = false;
        Debug.Log("Planting");
    }
    [ServerRpc(RequireOwnership = false)]
    public void WaterPlantServerRpc(bool wet) {
        watered.Value = wet;
        dry.Value = !wet;
        Debug.Log("plant watered");
        StartCoroutine(TimeTillPlantDry(200));
    }
    [ServerRpc(RequireOwnership = false)]
    public void PlantDyingServerRpc() {
        if (dry.Value && !watered.Value){
            StartCoroutine(TimeTillPlantDead(300));
        } else StopCoroutine(TimeTillPlantDead(300));
        
    }
    [ServerRpc(RequireOwnership = false)]
    public void PlantDeadServerRpc() {
        seedPlanted.Value = false;
        watered.Value = false;
        dry.Value = false;
        dead.Value = false;
        grown.Value = false;
        notPlanted.Value = true;
        StopCoroutine(TimeTillPlantDead(300));
        StopCoroutine(TimeTillPlantDry(200));
        StopCoroutine(WaitForPlantGrow(10));
    }
    [ServerRpc(RequireOwnership = false)]
    public void ChangePlantServerRpc() {
        if (dead.Value /* && grown.Value && dry.Value && !watered.Value */) {
            seed.SetActive(false);
            deadPlant.SetActive(true); 
            plant.SetActive(false);
            dryPlant.SetActive(false);
        } 
        if (seedPlanted.Value && !notPlanted.Value && !grown.Value) {
            seed.SetActive(true);
            deadPlant.SetActive(false);
            plant.SetActive(false);
            dryPlant.SetActive(false);
        }
        if (grown.Value && watered.Value && !dry.Value) {
            plant.SetActive(true);
            deadPlant.SetActive(false);
            dryPlant.SetActive(false);
            seed.SetActive(false);
        }
        if (dry.Value && (!watered.Value) && grown.Value && !dead.Value) {
            dryPlant.SetActive(true);
            plant.SetActive(false);
            deadPlant.SetActive(false);
            seed.SetActive(false);
        }
        if (notPlanted.Value){
            dryPlant.SetActive(false);
            plant.SetActive(false);
            deadPlant.SetActive(false);
            seed.SetActive(false);
            Debug.Log("not planted");
        }
    } 
    
    
    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();
        dry.OnValueChanged += OnStateChange;
        dead.OnValueChanged += OnStateChange;
        watered.OnValueChanged += OnStateChange;
        seedPlanted.OnValueChanged += OnStateChange;
        grown.OnValueChanged += OnStateChange;
        notPlanted.OnValueChanged += OnStateChange;
        OnStateChange(false, true);
    } 
    public override void OnNetworkDespawn(){
        dry.OnValueChanged -= OnStateChange;
        dead.OnValueChanged -= OnStateChange;
        watered.OnValueChanged -= OnStateChange;
        seedPlanted.OnValueChanged -= OnStateChange;
        grown.OnValueChanged -= OnStateChange;
        notPlanted.OnValueChanged -= OnStateChange;
    }
    private void OnGUI() {
        if (seedPlanted.Value && !notPlanted.Value) {
            plantStatus.text = "Please water your seed";
        }
        if (dry.Value && (!watered.Value)) {
            plantStatus.text = "Your plant is getting dry please water it soon";
        }
        if (!seedPlanted.Value && notPlanted.Value) {
            plantStatus.text = "Plant your seed";
        }
        if (grown.Value && watered.Value && !dry.Value) {
            plantStatus.text = "Your plant looks good";
        }
        if (dead.Value) {
            plantStatus.text = "plants gone unfortunatly";
        }
        if (seedPlanted.Value && watered.Value && !grown.Value && !notPlanted.Value){
            plantStatus.text = "Your plant is growing";
        }

    } 
}
