using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour {
    [SerializeField]
    private float speed = 0.2f;
    void FixedUpdate() {
        transform.Rotate(Vector3.forward, Time.fixedDeltaTime * speed);
    }
}
