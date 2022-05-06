using UnityEngine;
using Cinemachine;


public class StarParticles : MonoBehaviour
{
    // float sensitivity = 0.25f; UNUSED -- TODO remove?
    public CinemachineVirtualCamera cam;
    private ParticleSystem.Particle[] stars;
    private Vector3 moveMapShip;
    public GameObject smol_ship;
    private GameObject ship;
    private MapControl shipOutOfMap;
    private bool shipOut = false;
    public int maxStars = 100;
    public float starSize = 1.0f;

    public float speed = 0.0f;
    public float maxSpeed = 100.0f;

    public float starDistance = 10.0f;

    private float starDistanceSqr;
    public float clippingDist = 1.0f;
    private float clippingDistSqr;
    private Vector3 moveStar;
    public bool driving;

    void Start() {
        ship = GameObject.FindGameObjectWithTag("Map");
        shipOutOfMap = ship.GetComponent<MapControl>();

        moveMapShip = smol_ship.transform.position;
        //cam = Camera.main;
        driving = false;
        starDistanceSqr = starDistance * starDistance;
        clippingDistSqr = clippingDist * clippingDist;
        createStars();
    }
    private void createStars(){
        stars = new ParticleSystem.Particle[maxStars];

        for(int i = 0; i < maxStars; i++){
            //current particle position around camera (sphere) * distace (inside sphere) * transofrm ( keeps particle around camera)
            stars[i].position = Random.insideUnitSphere * starDistance + cam.transform.position;
#pragma warning disable CS0618
            stars[i].color = new Color(1,1,1,1);
            stars[i].size = starSize;
#pragma warning restore CS0618
        }
    }
    void Update() {
        shipOut = shipOutOfMap.outOfMap;

        for (int i = 0; i < maxStars; i++){
            if(speed > 0){
                moveStar = stars[i].position - cam.transform.forward + cam.transform.position * speed * Time.deltaTime;
                stars[i].position = moveStar ;   
                transform.position = moveStar; 

                moveMapShip += Vector3.Scale(cam.transform.forward,new Vector3(0.0f,0.0f,-1.0f)) * speed*0.000001f * Time.deltaTime;
                smol_ship.transform.position = moveMapShip;
            }
            else{
                speed = 0;
                moveStar = Random.insideUnitSphere * starDistance + cam.transform.position;
            } 

            if(driving){
                if(Input.GetKey(KeyCode.W)){
                    if(speed < maxSpeed){
                        speed += 0.01f * Time.deltaTime;
                    }
                    if(speed == maxSpeed){
                        speed = maxSpeed;
                    }
                    moveStar = stars[i].position - cam.transform.forward + cam.transform.position * speed * Time.deltaTime;
                    stars[i].position = moveStar ;   
                    transform.position = moveStar; 

                    moveMapShip += Vector3.Scale(cam.transform.forward,new Vector3(0.0f,0.0f,-1.0f)) * speed*0.000001f * Time.deltaTime;
                    smol_ship.transform.position = moveMapShip;

                        
                }
                if(Input.GetKey(KeyCode.A)){
                    moveStar = stars[i].position + cam.transform.right + cam.transform.position * speed * Time.deltaTime;
                    stars[i].position = moveStar;
                    transform.position = moveStar;

                    moveMapShip += Vector3.Scale(cam.transform.right,new Vector3(1.0f,0.0f,0.0f)) * speed*0.000001f * Time.deltaTime;
                    smol_ship.transform.position = moveMapShip;    
                }
                if(Input.GetKey(KeyCode.D)){
                    moveStar = stars[i].position - cam.transform.right + cam.transform.position * speed * Time.deltaTime;
                    stars[i].position = moveStar;
                    transform.position = moveStar;  

                    moveMapShip += Vector3.Scale(cam.transform.right,new Vector3(-1.0f,0.0f,0.0f)) * speed*0.000001f * Time.deltaTime;
                    smol_ship.transform.position = moveMapShip;  
                }
                if(Input.GetKey(KeyCode.S)){
                    if(speed > 0){
                        speed -= 0.01f * Time.deltaTime;
                    }
                }  
            } 
            if(shipOut){
                Debug.Log("StartPArt. Ship out");
                if(speed > 0){
                        speed = 10;
                }
            }
            if((moveStar - cam.transform.position).sqrMagnitude > starDistanceSqr){
                stars[i].position = Random.insideUnitSphere.normalized * starDistance + cam.transform.forward + cam.transform.position;
            }
            if((stars[i].position - cam.transform.position).sqrMagnitude <= clippingDistSqr){
                float visability = (stars[i].position - cam.transform.forward +  cam.transform.position).sqrMagnitude / clippingDistSqr; //clipping distance 100%
#pragma warning disable CS0618
                stars[i].color = new Color(1,1,1,visability);
                stars[i].size = starSize * visability;
#pragma warning restore CS0618
            } 
            
            
        }

        GetComponent<ParticleSystem>().SetParticles(stars, stars.Length);
    }
    void OnGUI() {
        GUILayout.Box("actual Speed: " + speed.ToString() );
        
    }
}
