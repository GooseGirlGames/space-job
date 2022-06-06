using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraSwap : MonoBehaviour {

    public const int INROOM_CAMERA_ON = 120;
    public const int CAMERA_ON = 100;
    public const int CAMERA_OFF = 0;

    public CinemachineVirtualCamera m_Camera;
    public bool InRoom;

    public static bool CurrentSwapInRoom = false;
    private static List<CameraSwap> m_Instances = new List<CameraSwap>();
    public static List<CameraSwap> Instances { get => m_Instances; }

    void Start() {
        if(m_Camera == null){
            m_Camera = GetComponent<CinemachineVirtualCamera>();
        }
        if(m_Camera == null){
            m_Camera = GetComponentInChildren<CinemachineVirtualCamera>();
        }

        // I *think* this is not needed anymore because UpdateLookAt is
        // called whenever an avatar spawns -- not sure tho :)
        if(!InRoom) {
            if (PlayerManager.Instance.LocalPlayer != null) {
                PlayerAvatar avatar = PlayerManager.Instance.LocalPlayer.Avatar;
                if (avatar != null) {
                    LookAtPlayer(avatar);
                } else {
                    PlayerManager.Instance.LocalPlayer.OnAvatarChanged += LookAtPlayer;
                }
            }
        }

        if (!m_Camera) {
            Debug.LogWarning("No camera found for CameraSwap " + name);
        }

        m_Instances.Add(this);
    }

    public void LookAtPlayer(PlayerAvatar avatar) {
        m_Camera.LookAt = avatar.CameraLookAt.transform;
    }

    public void SwitchTo() {
        if (InRoom) {
            m_Camera.Priority = INROOM_CAMERA_ON;
            foreach (var cam in CameraSwap.Instances) {
                if (cam.InRoom && cam != this) {
                    cam.m_Camera.Priority = CAMERA_OFF;
                }
            }
            m_Camera.Priority = INROOM_CAMERA_ON;
        } else {
            Solo();
        }
        PlayerManager.Instance.LocalPlayer.Avatar.HidePlayer(InRoom);
    }

    public void SwitchAway() {
        if (InRoom) {
            m_Camera.Priority = CAMERA_OFF;
            PlayerManager.Instance.LocalPlayer.Avatar.HidePlayer(false);
        }
    }

    public void Solo() {
        m_Camera.Priority = CAMERA_ON;
        foreach (var cam in CameraSwap.Instances) {
            if (cam != this) {
                cam.m_Camera.Priority = CAMERA_OFF;
            }
        }
        m_Camera.Priority = CAMERA_ON;  // i don't know either
    }


    public static void UpdateLookAt(PlayerAvatar avatar) {
        foreach (var camera in m_Instances) {
            if (!camera.InRoom) {
                camera.LookAtPlayer(avatar);
            }
        }
    }
}
