using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerNetwork : NetworkBehaviour
{


    [SerializeField] private Transform spawnedObjectPrefab;
    public Transform spawnedObjectTransform;
    Rigidbody rb;
    int layerMask = 1 << 6;
    bool jumpPause = true;
    public CameraHandler ch;
    [SerializeField] public Vector2 cameraInput;
    public float VertCamInput;
    public float HorzCamInput;
    public GameObject[] cameras;
    public GameObject[] players;
    public bool currentcam = false;
    public Vector3 camDirF;
    public Vector3 camDirR;
    public bool dead = false;
    public float HorzInput;
    public float VertInput;
    public float LastHorzInput;
    public float LastVertInput;
    // public GameObject cameraHANDLER;
    //tests for sending custom data
    //will need to elaborate on this when I start making the game mechanics
    private NetworkVariable<myCustomData> randomNumber = new NetworkVariable<myCustomData>(
        new myCustomData {
            _int = 50,
            _bool = false,
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    //allows the data to be sent online over the netcode
    public struct myCustomData : INetworkSerializable
    {
        public int _int;
        public bool _bool;
        public FixedString128Bytes message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref message);

        }
    }

    //not being used, a function that would allow the changing of variables that I will need for the frog mechanics
    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += (myCustomData previousValue, myCustomData newValue) =>
        {
            Debug.Log(OwnerClientId + "; randomNumber:" + newValue._int + "; " + newValue._bool + "; "+ newValue.message);
        };
        

    }
    public void OnEnable()
    {
        spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
        spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
        ch = spawnedObjectTransform.GetComponentInChildren<CameraHandler>();
        ch.targetTransform = this.gameObject.transform;
       

    }
    // Update is called once per frame
    //due to scene references and how the netcode package works, the game spawns a controllable player for each ID present that is the transform for this object.
    //T and Y buttons also allow for a gameobject aside from the player to be spawned and seen by both over the netcode
    //this will become useful for future game mechanics programming
    void Update()
    {
        if (!dead)
        {
            ch = spawnedObjectTransform.GetComponentInChildren<CameraHandler>();
            ch.targetTransform = this.gameObject.transform;
        }
       
        if (ch.targetTransform.GetComponentInChildren<PlayerNetwork>().dead)
        {
            ch.targetTransform = players[Random.Range(0, players.Length)].transform;
        }
        rb = this.GetComponent<Rigidbody>();
        cameras = GameObject.FindGameObjectsWithTag("CAMERA");
        foreach (GameObject c in cameras)
        {
            if ((cameraInput.x != 0 || cameraInput.y != 0) && c.GetComponentInChildren<CameraHandler>().inputReceived == false)
            {
                c.SetActive(false);
            }
            else
            {
                camDirF = c.transform.forward;
                camDirR = c.transform.right;
            }
            if (dead)
            {
                if (this.gameObject.transform == c.GetComponentInChildren<CameraHandler>().targetTransform)
                {
                    c.GetComponentInChildren<CameraHandler>().targetTransform = players[Random.Range(0, players.Length)].transform;
                }
            }

        }
        if (!IsOwner) return;
        //  cameraHANDLER.GetComponent<NetworkObject>().Spawn(true);

        if(Input.GetKeyDown(KeyCode.T))
        {

           
            TestServerRpc("ServerRPC working");
            TestClientRpc();
            randomNumber.Value = new myCustomData {  _int = Random.Range(0,100), _bool = false, message = "ABCDEFG"};
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            spawnedObjectTransform.GetComponent<NetworkObject>().Despawn(true);
            Destroy(spawnedObjectTransform.gameObject);
        }

        HandleMovement();
    }

    public void FixedUpdate()
    {
        if (HorzInput != 0 || VertInput != 0)
        {
            LastHorzInput = HorzInput;
            LastVertInput = VertInput;
            Vector3 lookAngle = HorzInput * camDirF.normalized + -VertInput * camDirR.normalized;
            this.transform.rotation = Quaternion.LookRotation(lookAngle, Vector3.up);
        }
        //otherwise use the current input for direction
        else
        {
           // Vector3 lookAngle = LastHorzInput * camDirF.normalized + -LastVertInput * camDirR.normalized;
           // this.transform.rotation = Quaternion.LookRotation(lookAngle, Vector3.up);
        }
        float d = Time.fixedDeltaTime;
        ch.FollowTarget(d);
        ch.CamRotation(d, HorzCamInput, VertCamInput);
    }

    [ServerRpc]
    private void TestServerRpc(string message)
    {
        Debug.Log("TestServerRPC" + message);
    }

    [ClientRpc]
    private void TestClientRpc()
    {
        Debug.Log("TestClientRPC");
    }
    public void HandleMovement()
    {
        cameraInput.x = Input.GetAxis("Mouse X");
        cameraInput.y = Input.GetAxis("Mouse Y");
        if(cameraInput.x != 0 || cameraInput.y != 0) 
        {
            ch.inputReceived = true;
        }
        else
        {
            ch.inputReceived = false;
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
       // MoveDir = camDir.normalized * MoveDir.x
        transform.position += MoveDir.z * camDirF.normalized * moveSpeed * Time.deltaTime + MoveDir.x * camDirR.normalized * moveSpeed * Time.deltaTime;
        Debug.DrawRay(new Vector3(this.transform.position.x, this.transform.position.y + 1, this.transform.position.z), this.transform.TransformDirection(Vector3.down) * 2f, Color.cyan);
        if (Physics.Raycast(new Vector3(this.transform.position.x, this.transform.position.y + 1, this.transform.position.z), this.transform.TransformDirection(Vector3.down), 2f, layerMask) && jumpPause == true)
        {
         
            if (Input.GetKey(KeyCode.Space))
            {
                rb.velocity = Vector3.zero;
                Vector3 velocity = MoveDir.z * camDirF.normalized * 3 + MoveDir.x * camDirR.normalized * 3;
                velocity.y = 10;
                rb.AddForce(velocity, ForceMode.Impulse);
                StartCoroutine(afterJump());
            }
        }
           
    }
    IEnumerator afterJump()
    {
        jumpPause = false;
        yield return new WaitForSeconds(0.5f);
        jumpPause = true;

    }
    void OnCollisionEnter(Collision c)
    {
        if(c.gameObject.tag == "Water")
        {
            dead = true;
            //cameras[Random.Range(0, cameras.Length)].SetActive(true);
            players = GameObject.FindGameObjectsWithTag("Player");
            this.transform.position = new Vector3(0,-100,0);
            ch.targetTransform = players[Random.Range(0,players.Length)].transform;
            // Destroy(this.gameObject);
        }
    }
}
