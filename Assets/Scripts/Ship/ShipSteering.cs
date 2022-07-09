using Unity.Netcode;
using UnityEngine;
using System;

[RequireComponent(typeof(ShipManager), typeof(NetworkObject))]
public class ShipSteering : NetworkBehaviour {

    public static readonly float TRANSLATION_ACCELERATION = 5.0f;
    public static readonly float ROTATION_ACCELERATION = 2.0f;
    public static readonly float[] TARGET_VELOCITY_STEPS = {-5f, -1f, 0, 1f, 5f};
    public static readonly float MAX_TRANSLATION_VELOCITY = 20f;
    public static readonly float MAX_ABS_ANGULAR_VELOCITY = 30.0f;
    public static readonly float EPSILON = 0.05f;

    private AudioSource audioSource;
    [SerializeField]
    private AudioSource audioSourceLeft; 
    [SerializeField]
    private AudioSource audioSourceRight; 
    [SerializeField]
    private AudioSource audioSourceTerminal;
    [SerializeField]
    private AudioSource audioSourceSpeed; 
    [SerializeField]
    private AudioClip buttonSound;


    public enum Thruster {
        ROTATE_LEFT,
        ROTATE_RIGHT,
        TRANSLATE_LEFT,
        TRANSLATE_RIGHT,
        TRANSLATE_FORWARD,
        TRANSLATE_BACKWARD,
    }

    // z ignored, but using Vector3 for ez rotation~
    private NetworkVariable<Vector3> m_Velocity = new NetworkVariable<Vector3>();
    private NetworkVariable<int> m_TargetVelocityIdx = new NetworkVariable<int>(2);
    private NetworkVariable<float> m_AngularVelocity = new NetworkVariable<float>(0);
    private bool[] m_ThrusterStates;  // kept up-to-date server-side only
    // updated server-side to reflect m_ThrusterStates:
    private NetworkVariable<int> m_ThrusterStatesNetwork = new NetworkVariable<int>(0);


    void Awake() {
        m_ThrusterStates = new bool[Enum.GetValues(typeof(Thruster)).Length];
        audioSource = GetComponent<AudioSource>();
    }

    public bool GetThrusterState(Thruster t) {
        return (m_ThrusterStatesNetwork.Value & (1 << (int)t)) != 0;
    }

    [ServerRpc(RequireOwnership=false)]
    public void SetThrusterStateServerRpc(Thruster t, bool state) {
        m_ThrusterStates[(int)t] = state;
        if(GetThrusterState(Thruster.TRANSLATE_FORWARD) | 
            GetThrusterState(Thruster.TRANSLATE_BACKWARD) |
            GetThrusterState(Thruster.ROTATE_LEFT)|
            GetThrusterState(Thruster.ROTATE_RIGHT)){
            if(!audioSourceTerminal.isPlaying){
                audioSourceTerminal.PlayOneShot(buttonSound);
            } 
        }
        if (state) {
            m_ThrusterStatesNetwork.Value |= (1 << (int)t);
        } else {
            m_ThrusterStatesNetwork.Value &= ~(1 << (int)t);
        }
    }

    [ServerRpc(RequireOwnership=false)]
    public void ToggleThrusterStateServerRpc(Thruster t) {
        bool current = m_ThrusterStates[(int)t];
        SetThrusterStateServerRpc(t, !current);
    }
 
    [ServerRpc(RequireOwnership=false)]
    public void ChangeTargetVelocityServerRpc(bool up) {
        int delta_idx = up ? 1 : -1;
        m_TargetVelocityIdx.Value = Mathf.Clamp(m_TargetVelocityIdx.Value += delta_idx, 0, TARGET_VELOCITY_STEPS.Length - 1);
    }

    [ServerRpc(RequireOwnership=false)]
    public void SetTargetVelocityServerRpc(int velocity_idx) {
        m_TargetVelocityIdx.Value = Mathf.Clamp(velocity_idx, 0, TARGET_VELOCITY_STEPS.Length - 1);
    }

    public float GetTargetSpeed() {
        return TARGET_VELOCITY_STEPS[m_TargetVelocityIdx.Value];
    }

    public int GetTargetSpeedIndex() {
        return m_TargetVelocityIdx.Value;
    }

    public float GetSpeed() {
        return m_Velocity.Value.x;
    }

    public float GetAngularSpeed() {
        return m_AngularVelocity.Value;
    }

    private void ApplyAllActiveThrusters(float delta_time) {
        string active = "";
        foreach (Thruster thruster in Enum.GetValues(typeof(Thruster))) {
            if (GetThrusterState(thruster)) {
                ApplyThruster(thruster, delta_time);
                active += thruster + " ";
            }
        }
        //Debug.Log("Active Thrusters: " + active);
    }

    private void ApplyThruster(Thruster thruster, float delta_time) {
        switch(thruster) {
            case Thruster.TRANSLATE_FORWARD:
                m_Velocity.Value += new Vector3(1, 0, 0) * TRANSLATION_ACCELERATION * delta_time;
                break;
            case Thruster.TRANSLATE_BACKWARD:
                m_Velocity.Value -=  new Vector3(1, 0, 0) * TRANSLATION_ACCELERATION * delta_time;
                break;
            case Thruster.ROTATE_LEFT:
                m_AngularVelocity.Value += ROTATION_ACCELERATION * delta_time;
                break;
            case Thruster.ROTATE_RIGHT:
                m_AngularVelocity.Value -= ROTATION_ACCELERATION * delta_time;
                break;
            default:
                Debug.LogError("Thruster " + thruster + " not implemented yet");
                break;
        };

        // enforce max velocities
        if (Vector3.Magnitude(m_Velocity.Value) > MAX_TRANSLATION_VELOCITY) {
            m_Velocity.Value = Vector3.Normalize(m_Velocity.Value) * MAX_TRANSLATION_VELOCITY;
        }
        m_AngularVelocity.Value = Mathf.Clamp(m_AngularVelocity.Value, -MAX_ABS_ANGULAR_VELOCITY, +MAX_ABS_ANGULAR_VELOCITY);
    }

    void Update() {
        // Set Thruster States
        Debug_MoveWithKeys(Time.deltaTime);
        if (IsServer) {
            UpdateServerside();
        }
        if(GetThrusterState(Thruster.TRANSLATE_FORWARD) || GetThrusterState(Thruster.TRANSLATE_BACKWARD)){
            Debug.Log(audioSourceLeft.clip);
            audioSourceLeft.Play(0);
            audioSourceRight.Play(0);
            CameraBrain.Instance.SetNoiseParameters(0.1f);
        } else if (GetThrusterState(Thruster.ROTATE_LEFT) || GetThrusterState(Thruster.ROTATE_RIGHT)) {
            CameraBrain.Instance.SetNoiseParameters(0.05f);
        } else {
            CameraBrain.Instance.SetNoiseParameters(0.0f);
        }

        if(GetThrusterState(Thruster.ROTATE_LEFT)){
            if(audioSourceLeft.isPlaying) {
                Debug.Log("left thruster");
            }
            if(!audioSourceLeft.isPlaying) {
                audioSourceLeft.Play(0);
            }
        }
        if(GetThrusterState(Thruster.ROTATE_RIGHT)){
            Debug.Log("right thruster");
            if(!audioSourceRight.isPlaying) {
                audioSourceRight.Play(0);
            }
        }
        if(GetSpeed() > 0.0f){
            audioSourceSpeed.volume = 0.005f;
        } else audioSourceSpeed.volume = 0.0f;

    }

    // Server-side only
    void UpdateServerside() {
        float delta = Time.deltaTime;

        float target_x_velocity = TARGET_VELOCITY_STEPS[m_TargetVelocityIdx.Value];
        SetThrusterStateServerRpc(Thruster.TRANSLATE_FORWARD, m_Velocity.Value.x < target_x_velocity - EPSILON);
        SetThrusterStateServerRpc(Thruster.TRANSLATE_BACKWARD, m_Velocity.Value.x > target_x_velocity + EPSILON);

        // apply thrusters to set velocity
        ApplyAllActiveThrusters(delta);

        // rotate
        ShipManager.Instance.Rotate(m_AngularVelocity.Value * delta);

        // translate
        Quaternion rot = Quaternion.AngleAxis(ShipManager.Instance.GetShipAngle(), Vector3.forward);
        ShipManager.Instance.Move(rot * m_Velocity.Value * delta);
    }

    private void Debug_MoveWithKeys(float delta_time) {

#if !DISABLE_DEBUG_KEYS
        if (Input.GetKeyDown(KeyCode.I)) {
            ChangeTargetVelocityServerRpc(up: true);
        }
        if (Input.GetKeyDown(KeyCode.K)) {
            ChangeTargetVelocityServerRpc(up: false);
        }

        if (Input.GetKeyDown(KeyCode.J)) {
            SetThrusterStateServerRpc(Thruster.ROTATE_LEFT, true);
        }
        if (Input.GetKeyUp(KeyCode.J)) {
            SetThrusterStateServerRpc(Thruster.ROTATE_LEFT, false);
        }
        if (Input.GetKeyDown(KeyCode.L)) {
            SetThrusterStateServerRpc(Thruster.ROTATE_RIGHT, true);
        }
        if (Input.GetKeyUp(KeyCode.L)) {
            SetThrusterStateServerRpc(Thruster.ROTATE_RIGHT, false);
        }
#endif
    }

    public void ResetSteering() {
        m_Velocity.Value = new Vector3(0, 0, 0);
        m_AngularVelocity.Value = 0;
        m_TargetVelocityIdx.Value = 1;
        SetThrusterStateServerRpc(Thruster.ROTATE_LEFT, false);
        SetThrusterStateServerRpc(Thruster.ROTATE_RIGHT, false);
        SetThrusterStateServerRpc(Thruster.TRANSLATE_LEFT, false);
        SetThrusterStateServerRpc(Thruster.TRANSLATE_RIGHT, false);
        SetThrusterStateServerRpc(Thruster.TRANSLATE_FORWARD, false);
        SetThrusterStateServerRpc(Thruster.TRANSLATE_BACKWARD, false);
    }
}
