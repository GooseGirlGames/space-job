using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnStars : MonoBehaviour
{
    public GameObject prefabStars;
    private GameObject PartSys_go;
    private GameObject newStars_go;



    void Start()
    {
        PartSys_go =  Instantiate(prefabStars, transform.position, Quaternion.identity) as GameObject;
    }


    IEnumerator Wait()
    {
        
        yield return new WaitForSeconds(2.0f);
    }   
    
    void createNewStars(){
        

        if(Vector3.Distance(PartSys_go.transform.position, transform.position) > 200){

            //newStars_go = Instantiate(prefabStars, transform.position /* + new Vector3(100f,100f,100f) */, Quaternion.identity) as GameObject;
            newStars_go = Instantiate(prefabStars, transform.forward , Quaternion.identity) as GameObject;
            StartCoroutine(Wait());
            Destroy(PartSys_go);
            PartSys_go = newStars_go; 
        } 
        if(PartSys_go == null){
            PartSys_go = newStars_go; 
        }
            

    }


    void Update()
    {
        createNewStars();
        
    }
}
