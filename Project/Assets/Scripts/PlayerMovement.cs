using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] public Vector2 cameraInput;
    public float VertCamInput;
    public float HorzCamInput;
    public float HorzInput;
    public float VertInput;
    public float LastHorzInput;
    public float LastVertInput;
    bool grounded = true;
    bool jumpPause = true;
    bool tetherPause = true;
    public float hitStrength = 10;
    Rigidbody rb;
    public GameObject localTongue;
    public GameObject localTongueTransform;
    public Vector3 hitTarget;
    [SerializeField]
    private PlayerNetwork pn;
    int layerMask = 1 << 6;
    float moveSpeedAmp = 9;
    public GameObject[] cameras;
    // Start is called before the first frame update
    void Start()
    {
        pn = this.gameObject.GetComponent<PlayerNetwork>();
    }
    public void OnEnable()
    {
       pn = this.gameObject.GetComponent<PlayerNetwork>();
        rb = this.gameObject.GetComponent<Rigidbody>();
    }
    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        //if there is input, then register it in a variable
        if (HorzInput != 0 || VertInput != 0)
        {
            LastHorzInput = HorzInput;
            LastVertInput = VertInput;
            //the character needs to look in the direction of input
            //this needs to be relative to the camera, so that if A is hit, the player always looks left, no matter what left is in world space
            Vector3 lookAngle = HorzInput * pn.camDirF.normalized + -VertInput * pn.camDirR.normalized;
            this.transform.rotation = Quaternion.LookRotation(lookAngle, Vector3.up);
        }
        //get input for camera based on mouse movement
        cameraInput.x = Input.GetAxis("Mouse X");
        cameraInput.y = Input.GetAxis("Mouse Y");
        //if the mouse is moving, then the camera is receiving input
        //used for the earlier check to determine who's camera is who's
        if (cameraInput.x != 0 || cameraInput.y != 0)
        {
            pn.ch.inputReceived = true;
        }
        else
        {
            pn.ch.inputReceived = false;
        }
        cameraInput = cameraInput.normalized;
        VertCamInput = cameraInput.y;
        HorzCamInput = cameraInput.x;

        //simple player move functionality
        //W S for the z axis movement, 
        //A D for the x axis movement
        
        Vector3 MoveDir = new Vector3(0, 0, 0);
        if (Input.GetKey(KeyCode.W))
        {
            MoveDir.z = +1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            MoveDir.z = -1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            MoveDir.x = -1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            MoveDir.x = +1f;
        }

        VertInput = MoveDir.z;
        HorzInput = MoveDir.x;
        float moveSpeed = 3f;

        //player has more movement control in the air
        if (!grounded)
        {
            moveSpeed = moveSpeedAmp;
        }
        else
        {
            moveSpeed = 3f;
        }

        //make the player move, make sure movement direction is relative to the camera, so w always moves the player away from the camera
        this.transform.position += MoveDir.z * pn.camDirF.normalized * moveSpeed * Time.deltaTime + MoveDir.x * pn.camDirR.normalized * moveSpeed * Time.deltaTime;

        //*GROUND DETECTION*
        Debug.DrawRay(new Vector3(this.transform.position.x, this.transform.position.y + 1, this.transform.position.z), this.transform.TransformDirection(Vector3.down) * 2f, Color.cyan);
        //shoot a raycast below the player to determine if they are touching the ground or not
        if (Physics.Raycast(new Vector3(this.transform.position.x, this.transform.position.y + 1, this.transform.position.z), this.transform.TransformDirection(Vector3.down), 2f, layerMask) && jumpPause == true)
        {
            grounded = true;
            //if they are, and the player hits space, a jump is performed
            if (Input.GetKey(KeyCode.Space))
            {
                //launch them up, and horizontally in a direction of their choosing
                grounded = false;
                rb.velocity = Vector3.zero;
                Vector3 velocity = Vector3.zero;
                velocity.y = 10;
                rb.AddForce(velocity, ForceMode.Impulse);
                StartCoroutine(afterJump());
            }
        }

        //*GRAPPLE MECHANIC*
        //check for player input
        if (Input.GetKeyDown(KeyCode.E) && tetherPause == true)
        {
            //if there is input, then draw a ray cast from the center of the camera forward a certain distance
            grounded = false;
            RaycastHit hit = new RaycastHit();
            Vector3 pos = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            Ray ray = pn.ch.GetComponentInChildren<Camera>().ScreenPointToRay(pos);
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
            if (Physics.Raycast(ray, out hit, 30))
            {
                //check if it hits something tagged as an object that is grappleable
                if (hit.collider.gameObject.tag == "Stickable")
                {
                    //if it does, and the player is a client, then the server needs to spawn them an object
                    if (!IsServer)
                    {
                        pn.TestServerRpc(this.gameObject.GetComponent<NetworkObject>().NetworkObjectId, hit.point);
                    }
                    //if they are not, then the server spawns its own
                    else
                    {
                        pn.Test2ServerRpc(this.gameObject.GetComponent<NetworkObject>().NetworkObjectId, hit.point);
                    }

                    //register the player as being in the air, mark the vector of the hit location, and create a local version of the tongue
                    hitTarget = hit.point;
                    pn.localTongueTransform = Instantiate(localTongue);
                    grounded = false;
                    Vector3 hitDir = hit.point - pn.ch.transform.position;
                    rb.velocity = Vector3.zero;
                    //move player in the direction of the grappled object
                    rb.AddForce(hitDir.normalized * hitStrength, ForceMode.Impulse);
                    StartCoroutine(afterTether());
                }
            }
        }
        cameras = GameObject.FindGameObjectsWithTag("CAMERA");
        foreach (GameObject c in cameras)
        {
            //if this player is detecting player inputs for the camera, and on the camera's end, it's not receiving these inputs, then it's another player's camera
            if ((cameraInput.x != 0 || cameraInput.y != 0) && c.GetComponentInChildren<CameraHandler>().inputReceived == false)
            {
                //and it needs to be disabled locally
                c.SetActive(false);
            }
            else
            {
                //If this is the right camera, get it's orientation
                pn.camDirF = c.transform.forward;
                pn.camDirR = c.transform.right;
            }
        }
        }
  public void FixedUpdate()
    {
        float d = Time.fixedDeltaTime;
        pn.ch.FollowTarget(d);
        pn.ch.CamRotation(d, HorzCamInput, VertCamInput);
    }
    //cooldown for jumping
    IEnumerator afterJump()
    {
        jumpPause = false;
        yield return new WaitForSeconds(0.5f);
        jumpPause = true;

    }

    //cooldown for grappling
    IEnumerator afterTether()
    {
        tetherPause = false;
        yield return new WaitForSeconds(0.2f);
        tetherPause = true;

    }
}
