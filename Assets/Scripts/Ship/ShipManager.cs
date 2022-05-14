using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ShipManager : NetworkBehaviour {
    public List<Room> Rooms = new List<Room>();

    public const char HAS_POWER = '\0';

    private NetworkVariable<float> m_Oxygen = new NetworkVariable<float>(1f);
    private NetworkVariable<char> m_Power = new NetworkVariable<char>(HAS_POWER);
    public static ShipManager Instance;
    private NetworkVariable<Vector2> m_Position = new NetworkVariable<Vector2>(new Vector2(512f, 512f));
    private NetworkVariable<float> m_Rotation = new NetworkVariable<float>(0f);
    private NetworkVariable<float> m_Speed = new NetworkVariable<float>(0f);
    private Map m_Map;
    private NetworkVariable<Vector2> m_Destination = new NetworkVariable<Vector2>(new Vector2(84f, 155f));
    private float m_DistanceToWin;
    public static char[] ERROR_CODES = {'2', 'e', (char)0xba, (char)42, '\n'};
    public static string PowerSolutionCode(char error_code) {
        return Convert.ToByte((error_code >> 1) ^ 'a').ToString("x2").ToUpper()
                + Convert.ToByte(error_code | 'B').ToString("x2").ToUpper();
    }
    public static string ErrorCodeDisplay(char power) {
        return Convert.ToByte(power ^ 'L').ToString("x2").ToUpper()
                + Convert.ToByte(power).ToString("x2").ToUpper();
    }

    void Start() {
        m_Map = GetComponent<Map>();
        /*
        foreach(char error_code in ERROR_CODES) {
            Debug.Log(ErrorCodeDisplay(error_code) + " / " + PowerSolutionCode(error_code));
        }
        */
    }

    public Terminal PowerTerminal;

    public bool HasPower{
        get => m_Power.Value == HAS_POWER;
    }



    protected void OnPowerChange(char _, char power){
        bool hasPower = power == HAS_POWER;
        LightManager.Instance.SetBackup(!hasPower);
        LightManager.Instance.SetNormal(hasPower);

        if (!hasPower) {
            PowerTerminal.DisplayError("0x" + ErrorCodeDisplay(power));
        } else {
            PowerTerminal.DisplayError("Power:\n100%");
        }

    }
    public override void OnNetworkSpawn(){
        m_Power.OnValueChanged += OnPowerChange;
        OnPowerChange(HAS_POWER, HAS_POWER);
    }
    public override void OnNetworkDespawn(){
        m_Power.OnValueChanged -= OnPowerChange;
    }

    public float GetShipSpeed(){
        return m_Speed.Value;
    }
    public float GetShipAngle(){
        return m_Rotation.Value;
    }
    public Vector2 GetShipPosition (){
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

    [ServerRpc(RequireOwnership = false)]
    public void AccillerateSpeedServerRpc(float speed){
        m_Speed.Value = m_Speed.Value + speed;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AccillerateAngleServerRpc(float angle){
        m_Rotation.Value = m_Rotation.Value + angle;
    }

    public void TriggerPowerOutageEvent(){
        int error_idx = UnityEngine.Random.Range(0, ERROR_CODES.Length - 1);
        m_Power.Value = ERROR_CODES[error_idx];
        Rooms[0].RoomOxygen = 0;
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
        Rooms[0].RoomOxygen = 1;
    }
    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this);
        }
    }

    private void UpdatePosition(float delta_time){

        float angle = m_Rotation.Value*(Mathf.PI/180f);
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        m_Position.Value = m_Position.Value + (direction * m_Speed.Value * delta_time);

        MapState state = m_Map.GetState(m_Position.Value);
        if (state.ev == Event.POWER_OUTAGE) {
            TriggerPowerOutageEvent();
        }
    }


    private void CheckWinCondition(){
        m_DistanceToWin = (m_Destination.Value - m_Position.Value).magnitude; 
        if (m_DistanceToWin <= 5){
            Debug.Log("Hey you won");
        } 
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.P) && IsServer){
            if (HasPower) {
                TriggerPowerOutageEvent();
            } else {
                ResolvePowerOutageEvent();
            }
        }
        if (Input.GetKeyDown(KeyCode.O)) {
            foreach (Room room in Rooms) {
                Debug.Log(room.Name + ": " + room.RoomOxygen);
            }
        }
        
        CheckWinCondition();

        if (IsServer) {
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

            UpdatePosition(Time.deltaTime);
        }
    }
}
