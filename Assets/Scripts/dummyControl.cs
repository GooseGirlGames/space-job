using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dummyControl : MonoBehaviour
{
    private CharacterController controller;
    public float movementSpeed = 5f;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        
    }

    // Update is called once per frame
    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontalInput, 0, verticalInput);

        if (Input.GetKey(KeyCode.LeftControl)){
            controller.Move(direction * Time.deltaTime * movementSpeed * 2.5f);
            //Debug.Log("Sprinting");
        }
        else{
            controller.Move(direction * Time.deltaTime * movementSpeed);
        }
    }
}
