using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwap : MonoBehaviour
{
    private Camera activeCamera;
    public Camera nextCamera;
    private Collider activeArea;

    void Start()
    {
        activeArea = GetComponent<Collider>();
        activeCamera = GetComponentInChildren<Camera>();
    }

    private void OnTriggerExit(Collider other) {
        Debug.Log("mmmh");
        if(other.GetComponent<CharacterController>() != null)
            Debug.Log("charakter");
            activeCamera.enabled = false;
            nextCamera.enabled = true;
    }
}
