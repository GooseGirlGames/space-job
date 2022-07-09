using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ShipManager : NetworkBehaviour {
    public static ShipManager Instance;
    private ShipSteering m_Steering;
    public ShipSteering Steering {
        get => m_Steering;
    }

    public const char HAS_POWER = '\0';

    public List<Room> Rooms = new List<Room>();

    private NetworkVariable<float> m_Oxygen = new NetworkVariable<float>(1f);
    private NetworkVariable<char> m_Power = new NetworkVariable<char>(HAS_POWER);
    private NetworkVariable<Vector2> m_Position = new NetworkVariable<Vector2>(new Vector2(512f, 512f));
    private NetworkVariable<float> m_Rotation = new NetworkVariable<float>(0f);
    private NetworkVariable<float> m_Speed = new NetworkVariable<float>(0f);
    private NetworkVariable<float> m_Odometer = new NetworkVariable<float>(0f);
    private NetworkVariable<bool> m_Won = new NetworkVariable<bool>(false);
    private float m_DistSinceLastBreadcrumb = 0;
    private Map m_Map;
    private NetworkVariable<Vector2> m_Destination = new NetworkVariable<Vector2>(new Vector2(84f, 155f));
    private float m_DistanceToWin;

    public PowerTerminal PowerTerminal;
    public Canvas WinCanvas;

    [SerializeField]
    private Material m_TransitionMaterial;
    private Color m_TransitionColorNormal;
    [SerializeField]
    private Color m_TransitionColorOutage;

    public static char[] ERROR_CODES = {'2', 'e', (char)0xba, (char)42, '\n'};
    [SerializeField]
    private AudioSource audioSourceLamps;
    [SerializeField]
    private AudioClip lampSound;

    private Plant[] plants;

    public static string PowerSolutionCode(char error_code) {
        return Convert.ToByte((error_code >> 1) ^ 'a').ToString("x2").ToUpper()
                + Convert.ToByte(error_code | 'B').ToString("x2").ToUpper();
    }
    public static string ErrorCodeDisplay(char power) {
        return Convert.ToByte(power ^ 'L').ToString("x2").ToUpper()
                + Convert.ToByte(power).ToString("x2").ToUpper();
    }

    public bool HasPower{
        get => m_Power.Value == HAS_POWER;
    }

    protected void OnPowerChange(char _, char power){
        bool hasPower = power == HAS_POWER;
        
        if(LightManager.Instance != null){
            LightManager.Instance.SetBackup(!hasPower);
            LightManager.Instance.SetNormal(hasPower);
        }
        

        if(PowerTerminal != null){   
            if (!hasPower) {
                PowerTerminal.DisplayError("0x" + ErrorCodeDisplay(power));
            } else {
                PowerTerminal.DisplayError("Power:\n100%");
            }
        } 

        if (m_TransitionMaterial != null) {
            var col = hasPower ? m_TransitionColorNormal : m_TransitionColorOutage;
            m_TransitionMaterial.SetColor("_WireColor", col);
        }

    }
    protected void OnWinChange(bool prev, bool current) {
        if (WinCanvas != null) {
            WinCanvas.enabled = current;
        }
    }
    public override void OnNetworkSpawn(){
        m_Power.OnValueChanged += OnPowerChange;
        m_Won.OnValueChanged += OnWinChange;
        OnPowerChange(HAS_POWER, HAS_POWER);
        OnWinChange(false, false);
    }
    public override void OnNetworkDespawn(){
        m_Power.OnValueChanged -= OnPowerChange;
        m_Won.OnValueChanged -= OnWinChange;
    }

    public float GetShipSpeed(){
        return m_Steering.GetSpeed();
    }
    public float GetTargetShipSpeed(){
        return m_Steering.GetTargetSpeed();
    }
    public float GetShipAngle(){
        return m_Rotation.Value;
    }
    public float GetShipAngleSpeed(){
        return m_Steering.GetAngularSpeed();
    }
    public Vector2 GetShipPosition(){
        return m_Position.Value;
    }

    public float GetDistantToWin(){
        return m_DistanceToWin;
    }

    public void SetGoal(Vector2 new_destination){
        m_Destination.Value = new_destination;
    }
    public Vector2 GetGoal(){
        return m_Destination.Value;
    }

    public void Rotate(float delta_angle) {
        m_Rotation.Value += delta_angle;
    }
    public void Move(Vector2 delta_pos_raw) {
        var new_pos = m_Position.Value + delta_pos_raw;
        new_pos.x = Mathf.Clamp(new_pos.x, Map.MIN, Map.MAX);
        new_pos.y = Mathf.Clamp(new_pos.y, Map.MIN, Map.MAX);

        var delta_pos = new_pos - m_Position.Value;
        m_Position.Value = new_pos;
        m_Odometer.Value += Vector3.Magnitude(delta_pos);
        m_DistSinceLastBreadcrumb += Vector3.Magnitude(delta_pos);

        if (m_DistSinceLastBreadcrumb > Mathf.Lerp(8, 64, GetShipSpeed()/ShipSteering.MAX_TRANSLATION_VELOCITY)) {
            DropBreadcrumbClientRpc(GetShipPosition());
            m_DistSinceLastBreadcrumb = 0;
        }
    }

    [ClientRpc]
    public void DropBreadcrumbClientRpc(Vector2 ship_pos) {
        m_Map.DropBreadcrumb(ship_pos);
    }

    public void TriggerPowerOutageEvent(){
        int error_idx = UnityEngine.Random.Range(0, ERROR_CODES.Length - 1);
        m_Power.Value = ERROR_CODES[error_idx];
    }
    public bool TryResolvePowerOutageEvent(string solution_attempt) {
        if (solution_attempt == PowerSolutionCode(m_Power.Value)) {
            ResolvePowerOutageEvent();
            return true;
        } else {
            return false;
        }
    }
    private void ResolvePowerOutageEvent() {
        m_Power.Value = HAS_POWER;
        audioSourceLamps.PlayOneShot(lampSound);
    }

    public void TriggerHullBreachEvent(EventParameters.HullBreachSize size) {
        Room room = Util.RandomChoice(Rooms);
        room.SpawnHullBreach(size);
    }

    public void TriggerFireEvent() {
        Room room = Util.RandomChoice(Rooms);
        room.SpawnFire();
    }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this);
        }

        m_Map = GetComponent<Map>();
        m_Steering = GetComponent<ShipSteering>();
        m_TransitionColorNormal = m_TransitionMaterial.GetColor("_WireColor");
    }

    public float GetPlantOxygen(){
        float oxygen = 0.0f;
        plants = FindObjectsOfType<Plant>();
        foreach (Plant p in plants){
            if(p.seedPlanted.Value){
                oxygen += 1.0f;
            }
        }
        Debug.Log(oxygen/10);
        return oxygen/10;
    }
    public override void OnDestroy() {
        base.OnDestroy();
        m_TransitionMaterial.SetColor("_WireColor", m_TransitionColorNormal);
    }

    private void Start() {
        if (IsServer) {
            Debug.Log("Lets Start Event Corutine");
            GetComponent<EventManager>().StartDiceRollCoroutine();
        }
    }

    private void UpdatePosition() {

        // float angle = m_Rotation.Value*(Mathf.PI/180f);
        // Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        // m_Position.Value = m_Position.Value + (direction * m_Speed.Value * delta_time);

        MapState state = m_Map.GetState(m_Position.Value); 
        
    }


    private void CheckWinCondition(){
        m_DistanceToWin = (m_Destination.Value - m_Position.Value).magnitude; 
        if (m_DistanceToWin <= 15){
            if (IsServer) {
                m_Won.Value = true;
            }
        } 
    }

    private void Update() {
#if !DISABLE_DEBUG_KEYS
        if(Input.GetKeyDown(KeyCode.P) && IsServer){
            if (HasPower) {
                TriggerPowerOutageEvent();
            } else {
                ResolvePowerOutageEvent();
            }
        }
        if(Input.GetKeyDown(KeyCode.U) && IsServer){
            TriggerHullBreachEvent(EventParameters.HullBreachSize.SMALL);
        }
        if(Input.GetKeyDown(KeyCode.F) && IsServer){
            TriggerFireEvent();
        }

        if (Input.GetKeyDown(KeyCode.O)) {
            Rooms[0].RoomOxygen = 0;
            foreach (Room room in Rooms) {
                Debug.Log(room.Name + ": " + room.RoomOxygen);
            }
        }
#endif
        
        CheckWinCondition();

        if (IsServer) {
#if !DISABLE_DEBUG_KEYS
            if (Input.GetKey(KeyCode.UpArrow)){
                m_Speed.Value += 0.1f;
            }
            if (Input.GetKey(KeyCode.DownArrow)){
                if(m_Speed.Value >= 0f){
                    m_Speed.Value -= 0.1f;
                }
            }
            if (Input.GetKey(KeyCode.LeftArrow)){
                m_Rotation.Value += 1f;
            }
            if (Input.GetKey(KeyCode.RightArrow)){
                m_Rotation.Value -= 1f;
            }
#endif

            UpdatePosition();
        }
    }

    [ServerRpc(RequireOwnership=false)]
    public void StartNewGameServerRpc() {
        m_Won.Value = false;
        Steering.ResetSteering();
        Vector2 ship_pos = Util.RandomVec2(256, 1024-256);
        Vector2 destination;
        do {
            destination = Util.RandomVec2(256, 1024-256);
        } while(Vector2.Distance(ship_pos, destination) < 32);
        SetGoal(destination);
        m_Position.Value = ship_pos;
        m_Rotation.Value = 0;
    }
}
