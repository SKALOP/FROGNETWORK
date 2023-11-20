using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerNetwork : NetworkBehaviour
{


    [SerializeField] private Transform spawnedObjectPrefab;
    public Transform spawnedObjectTransform;
    public GameObject localTongue;
    public GameObject localTongueTransform;
    [SerializeField] public GameObject TonguePrefab;
    [SerializeField] public GameObject ServerTonguePrefab;

    public GameObject tongueTransform;
    public TestLobby lt;
    public Vector3 hitTarget;
   // public SpawnTongue st;
    Rigidbody rb;
    int layerMask = 1 << 6;
    bool jumpPause = true;
    bool tetherPause = true;
    public CameraHandler ch;
    public TongueHandler th;
    [SerializeField] public Vector2 cameraInput;
    public float VertCamInput;
    public float HorzCamInput;
    public GameObject[] cameras;
    public GameObject[] players;
    public bool currentcam = false;
    bool speedAmpOn = false;
    public Vector3 camDirF;
    public Vector3 camDirR;
    public bool dead = false;
    public float HorzInput;
    public float VertInput;
    public float LastHorzInput;
    public float LastVertInput;
    public float hitStrength;
    float moveSpeedAmp = 9;
    bool grounded = true;
   public bool knockBackCD;
    float kbTimer = 1;
    public GameObject collidedObj;
    public Rigidbody rbAP;
    public int spawnLoc;
    public float startTimer = 5;
    // public GameObject cameraHANDLER;
    //tests for sending custom data
    //will need to elaborate on this when I start making the game mechanics
    private NetworkVariable<myCustomData> randomNumber = new NetworkVariable<myCustomData>(
        new myCustomData {
            _int = 50,
            _bool = false,
            dead = false,
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> deadValue = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> OnlineSpeedx = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> OnlineSpeedy = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> OnlineSpeedz = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    //allows the data to be sent online over the netcode
    public struct myCustomData : INetworkSerializable
    {
        public int _int;
        public bool _bool;
        public bool dead;
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
        deadValue.OnValueChanged += OnStateChanged;
        OnlineSpeedx.OnValueChanged += (float previous, float newValue) =>
        {

        };
        OnlineSpeedy.OnValueChanged += (float previous, float newValue) =>
        {

        };
        OnlineSpeedz.OnValueChanged += (float previous, float newValue) =>
        {

        };
        randomNumber.OnValueChanged += (myCustomData previousValue, myCustomData newValue) =>
        {
            Debug.Log(OwnerClientId + "; randomNumber:" + newValue._int + "; " + newValue._bool + "; "+ newValue.message);
        };
        

    }
    public void OnStateChanged(bool previous, bool current)
    {
        if (deadValue.Value == false)
        {
            ch = spawnedObjectTransform.GetComponentInChildren<CameraHandler>();
            ch.targetTransform = this.gameObject.transform;
        }

        if (ch.targetTransform.GetComponentInChildren<PlayerNetwork>().deadValue.Value == true)
        {
            ch.targetTransform = players[Random.Range(0, players.Length)].transform;
        }
    }
    public void OnEnable()
    {
     //   st = this.gameObject.GetComponent<SpawnTongue>();
       // randomNumber.Value = new myCustomData { _int = Random.Range(0, 100), _bool = false, dead = false, message = "ABCDEFG" };
        spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
        spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
        ch = spawnedObjectTransform.GetComponentInChildren<CameraHandler>();
        ch.targetTransform = this.gameObject.transform;

        spawnLoc = Random.Range(0, 3);
    }
    // Update is called once per frame
    //due to scene references and how the netcode package works, the game spawns a controllable player for each ID present that is the transform for this object.
    //T and Y buttons also allow for a gameobject aside from the player to be spawned and seen by both over the netcode
    //this will become useful for future game mechanics programming
    void Update()
    {
        rb = this.GetComponent<Rigidbody>();
        if (startTimer > 0)
        {
            Debug.Log("TICKING...");
            startTimer -= Time.deltaTime;
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("SPAWN");
            this.transform.position = spawns[spawnLoc].transform.position;
            GameObject[] Players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject p in Players)
            {
                Collider[] colliders;
                  if ((colliders = Physics.OverlapSphere(p.transform.position, 0.5f/* Radius */)).Length > 3)
                {
                   // foreach(var c in colliders)
                  //  {
                   //     Debug.Log(c);
                   // }
                   // Debug.Log("Moving... Too close" + colliders.Length);
                    spawnLoc = Random.Range(0, 3);
                }
               // if (p.transform.position == this.transform.position && p.gameObject != this.gameObject)
              //  {
               //     spawnLoc = Random.Range(0, 3);
               // }
            }
            rb.velocity = Vector3.zero;
        }
        try
        {

            // GameObject g = GameObject.Find("ServerTongue(Clone)");
            if (!IsServer)
            {
                tongueTransform = GameObject.Find("Tongue(Clone)");
                tongueTransform.gameObject.SetActive(false);
                //.Find("ServerTongue(Clone)").SetActive(false);
            }
        }
        catch
        {


        }
        try
        {
            if (th != null)
            {
                Debug.Log(hitTarget);
                th.updateTongue(hitTarget, this.gameObject.transform.position);
                // th.gameObject.SetActive(false);
                Destroy(tongueTransform.gameObject, 3);
                tongueTransform.GetComponent<NetworkObject>().NetworkHide(this.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
                localTongueTransform.GetComponent<TongueHandler>().updateTongue(hitTarget, this.gameObject.transform.position);
            }
        }
           
        
        catch{

        }
        if (knockBackCD)
        {
            
            kbTimer -= Time.deltaTime;
           if(kbTimer < 0)
            {
                knockBackCD = false;
            }
        }
       // tongueTransform = GameObject.Find("Tongue(Clone)");
       // tongueTransform.SetActive(false);
        if (localTongueTransform != null)
        {
            Debug.Log("DO THE THING");
           // th.gameObject.SetActive(false);
            localTongueTransform.GetComponent<TongueHandler>().updateTongue(hitTarget, this.gameObject.transform.position);
        }
        if (tongueTransform == null)
        {
            try
            {
                Destroy(localTongueTransform,3);
                tongueTransform.GetComponent<NetworkObject>().Despawn();
            }
            catch
            {

            }
        }
      
        // dead = randomNumber.dead;
        //  if (deadValue.Value == false)
        // {
        //      ch = spawnedObjectTransform.GetComponentInChildren<CameraHandler>();
        //      ch.targetTransform = this.gameObject.transform;
        //   }
        if (deadValue.Value == false)
        {
            ch = spawnedObjectTransform.GetComponentInChildren<CameraHandler>();
            ch.targetTransform = this.gameObject.transform;
        }
        if (ch.targetTransform.GetComponentInChildren<PlayerNetwork>().deadValue.Value == true)
        {
            ch.targetTransform = players[Random.Range(0, players.Length)].transform;
        }
        // if (ch.targetTransform.GetComponentInChildren<PlayerNetwork>().deadValue.Value == true)
        //   {
        //     ch.targetTransform = players[Random.Range(0, players.Length)].transform;
        //  }
        
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

           
           // TestServerRpc("ServerRPC working");
            //TestClientRpc();
            randomNumber.Value = new myCustomData {  _int = Random.Range(0,100), _bool = false, message = "ABCDEFG"};
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            spawnedObjectTransform.GetComponent<NetworkObject>().Despawn(true);
            Destroy(spawnedObjectTransform.gameObject);
        }
       // OnlineSpeedx.Value = rb.velocity.x;
       // OnlineSpeedy.Value = rb.velocity.y;
       // OnlineSpeedz.Value = rb.velocity.z;
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
    private void TestServerRpc(ulong clientId, Vector3 target)
    {
        
            tongueTransform = Instantiate(TonguePrefab);
        
       
           
        
           
        // tongueTransform.transform.SetParent(this.gameObject.transform);
        tongueTransform.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
        th = tongueTransform.GetComponentInChildren<TongueHandler>();
        // tongueTransform.GetComponent<NetworkObject>().NetworkHide(clientId);
      
        hitTarget = target;
        Debug.Log("TestServerRPC" + th);
    }

    [ServerRpc]
    private void Test2ServerRpc(ulong clientId, Vector3 target)
    {

        tongueTransform = Instantiate(ServerTonguePrefab);





        // tongueTransform.transform.SetParent(this.gameObject.transform);
        tongueTransform.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
        th = tongueTransform.GetComponentInChildren<TongueHandler>();
        // tongueTransform.GetComponent<NetworkObject>().NetworkHide(clientId);

        hitTarget = target;
        Debug.Log("TestServerRPC" + th);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CollisionServerRpc(Vector3 sender)
    {

        //  Collider[] colliders;
        //  if ((colliders = Physics.OverlapSphere(attacker, 0.2f /* Radius */)).Length == 1) 
        //  {
        //      foreach (var collider in colliders)
        //     {
        //          atk = collider.gameObject; 

        //   }
        //  }
        //  if ((colliders = Physics.OverlapSphere(receiver, 0.2f /* Radius */)).Length == 1)
        //  {
        //     foreach (var collider in colliders)
        //     {
        //          def = collider.gameObject;

        //     }
        //  }
        Debug.Log("Telling Host to Knockback themselves");
        Vector3 dir = sender - NetworkManager.LocalClient.PlayerObject.transform.position;
        //Debug.Log(collidedObj + "Hit this");
       // Debug.Log(dir + "BONLK");
       rbAP = NetworkManager.LocalClient.PlayerObject.GetComponent<Rigidbody>();
       //  rbAP = collidedObj.GetComponent<Rigidbody>();
       // rb.velocity = Vector3.zero;
       // rb.AddForce(dir.normalized * 10, ForceMode.Impulse);
        rbAP.AddForce(-dir.normalized * 10, ForceMode.Impulse);

    }
    [ClientRpc]
    private void CollisionClientRpc(Vector3 sender, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("telling client to knockback themselves");
          Vector3 dir = sender - NetworkManager.LocalClient.PlayerObject.transform.position;
       // Debug.Log(collidedObj + "Hit this");
       // Debug.Log(dir + "BONLK");
        rbAP = NetworkManager.LocalClient.PlayerObject.GetComponent<Rigidbody>();
        //  rbAP = collidedObj.GetComponent<Rigidbody>();
       // rb.velocity = Vector3.zero;
      //  rb.AddForce(dir.normalized * 10, ForceMode.Impulse);
        rbAP.AddForce(-dir.normalized * 10, ForceMode.Impulse);
    }

    [ClientRpc]
    private void TestClientRpc(ulong clientId, Vector3 target)
    {

        tongueTransform = Instantiate(TonguePrefab);
        // tongueTransform.transform.SetParent(this.gameObject.transform);
        tongueTransform.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
       // tongueTransform.gameObject.SetActive(false);
        th = tongueTransform.GetComponentInChildren<TongueHandler>();
        hitTarget = target;
        Debug.Log("TestServerRPC" + th);
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
       // MoveDir = camDir.normalized * MoveDir.x;
       if(!grounded)
        {
           moveSpeed = moveSpeedAmp;
        }
        else
        {
            moveSpeed = 3f;
        }
        transform.position += MoveDir.z * camDirF.normalized * moveSpeed * Time.deltaTime + MoveDir.x * camDirR.normalized * moveSpeed * Time.deltaTime;
        Debug.DrawRay(new Vector3(this.transform.position.x, this.transform.position.y + 1, this.transform.position.z), this.transform.TransformDirection(Vector3.down) * 2f, Color.cyan);
        if (Physics.Raycast(new Vector3(this.transform.position.x, this.transform.position.y + 1, this.transform.position.z), this.transform.TransformDirection(Vector3.down), 2f, layerMask) && jumpPause == true)
        {
            grounded = true;
            //rb.velocity = new Vector3(0, rb.velocity.y, 0);
            if (Input.GetKey(KeyCode.Space))
            {
                grounded = false;
                rb.velocity = Vector3.zero;
                Vector3 velocity = MoveDir.z * camDirF.normalized * 7 + MoveDir.x * camDirR.normalized * 7;
              //Vector3 velocity = Vector3.zero;
                velocity.y = 10;
                rb.AddForce(velocity, ForceMode.Impulse);
                StartCoroutine(afterJump());
            }
        }
        if (Input.GetKeyDown(KeyCode.E) && tetherPause == true)
        {
            //  rb.velocity = Vector3.zero;
            //  Camera cam = ch.
            grounded = false;
            RaycastHit hit = new RaycastHit();
            Vector3 pos = new Vector3(Screen.width / 2, Screen.height / 2,0);
            Ray ray = ch.GetComponentInChildren<Camera>().ScreenPointToRay(pos);
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
            if(Physics.Raycast(ray, out hit, 30))
            {
                if (hit.collider.gameObject.tag =="Stickable")
                {
                    if (!IsServer)
                    {
                        TestServerRpc(this.gameObject.GetComponent<NetworkObject>().NetworkObjectId, hit.point);
                    }
                    else
                    {
                        Test2ServerRpc(this.gameObject.GetComponent<NetworkObject>().NetworkObjectId, hit.point);
                    }
                    
                   // TestClientRpc(this.gameObject.GetComponent<NetworkObject>().NetworkObjectId,hit.point);
                    Debug.Log(hit.point + "HITPOINT");
                    hitTarget = hit.point;
                    localTongueTransform = Instantiate(localTongue);
                   // tongueTransform = Instantiate(TonguePrefab);
                   /////// tongueTransform.transform.SetParent(this.gameObject.transform);
                    //tongueTransform.GetComponent<NetworkObject>().Spawn(true);
                   // th = tongueTransform.GetComponentInChildren<TongueHandler>();
                    grounded = false;

                    Vector3 hitDir = hit.point - ch.transform.position;
                    rb.velocity = Vector3.zero;
                    rb.AddForce(hitDir.normalized * hitStrength, ForceMode.Impulse);
                    StartCoroutine(afterTether());
                   // StartCoroutine(SpeedAmp());
                }
            }
        }
    }
    IEnumerator afterJump()
    {
        jumpPause = false;
        yield return new WaitForSeconds(0.5f);
        jumpPause = true;

    }
    IEnumerator afterTether()
    {
        tetherPause = false;
        yield return new WaitForSeconds(0.2f);
        tetherPause = true;

    }
   // IEnumerator SpeedAmp()
   // {
    //    speedAmpOn = true;
        

        //    while(grounded) yield return null;
           
          //  speedAmpOn = false;
        

  //  }
    void OnCollisionEnter(Collision c)
    {
        if(c.gameObject.tag == "Water")
        {
            deadValue.Value = true;
            //dead = true;
            //cameras[Random.Range(0, cameras.Length)].SetActive(true);
            players = GameObject.FindGameObjectsWithTag("Player");
            this.transform.position = new Vector3(0,-100,0);
           // ch.targetTransform = players[Random.Range(0,players.Length)].transform;
            // Destroy(this.gameObject);
        }
        /*
        if (c.gameObject.tag == "Player" && knockBackCD == false)
        {
            CollisionServerRpc(this.gameObject.transform.position, c.gameObject.transform.position);
            float knockBackStrength = 5;
            float knockbackMultiplier;
            GameObject attackingPlayer = c.gameObject;
            Rigidbody rbAP = c.gameObject.GetComponent<Rigidbody>();
            Vector3 dir = this.transform.position - c.transform.position;
            ///  float xS = c.gameObject.GetComponentInChildren<PlayerNetwork>().OnlineSpeedx.Value;
            ///  float yS = c.gameObject.GetComponentInChildren<PlayerNetwork>().OnlineSpeedy.Value;
            /// float zS = c.gameObject.GetComponentInChildren<PlayerNetwork>().OnlineSpeedz.Value;
            //if(rbAP.velocity.magnitude > rb.velocity.magnitude)
            // {
            //     knockbackMultiplier = 1;
            // }
            // else
            // {
            //     knockbackMultiplier = -0.5f;
            // }
            // Vector3 SPEEEED = new Vector3(xS, 0, zS);
            //if (rbAP.velocity.magnitude > 0)
            // {
          //////  Debug.Log(dir + "BONLK");
         ///////   rb.velocity = Vector3.zero;
         ///////       rb.AddForce(dir.normalized * 10, ForceMode.Impulse);
           // }
           // else
           // {
            //    rb.velocity = rbAP.velocity * 100;
           // }
            Vector3 knockDir = new Vector3(rbAP.velocity.x,0,rbAP.velocity.z);
           // Debug.Log("Getting pushed at " + SPEEEED);
           // rb.velocity = Vector3.zero;
         
           // rb.AddForce(knockDir * knockBackStrength * knockbackMultiplier, ForceMode.Impulse);
            kbTimer = 1;
            knockBackCD = true;
        }
        */
    }
    void OnTriggerEnter(Collider c)
    {
        Debug.Log(c.gameObject.tag);

        if (c.gameObject.tag == "Player" || c.gameObject.tag == "Untagged" && knockBackCD == false)
        {

            float knockBackStrength = 5;
            float knockbackMultiplier;
            GameObject attackingPlayer = c.gameObject;
            rbAP = c.gameObject.GetComponent<Rigidbody>();
            Vector3 dir = this.transform.position - c.transform.position;
            Debug.Log(c.gameObject.tag + "THIS IS THE OTHER HITBOX");
           // collidedObj = c.gameObject;
            


            if (IsServer)
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { c.gameObject.GetComponent<NetworkObject>().NetworkObjectId }
                    }
                };
                CollisionClientRpc(dir, clientRpcParams);
            }
            if(!IsServer) {
                CollisionServerRpc(this.gameObject.transform.position);
            }




           // dir = this.transform.position - collidedObj.transform.position;
           // Debug.Log(collidedObj + "Hit this");
          //  Debug.Log(dir + "BONLK");
          //  rbAP = collidedObj.GetComponent<Rigidbody>();
          //  rb.velocity = Vector3.zero;
          //  rbAP.velocity = Vector3.zero;
          //  Debug.Log(rbAP);
           // rb.AddForce(dir.normalized * 10, ForceMode.Impulse);
           // rbAP.AddForce(-dir.normalized * 10, ForceMode.Impulse);
            ///  float xS = c.gameObject.GetComponentInChildren<PlayerNetwork>().OnlineSpeedx.Value;
            ///  float yS = c.gameObject.GetComponentInChildren<PlayerNetwork>().OnlineSpeedy.Value;
            /// float zS = c.gameObject.GetComponentInChildren<PlayerNetwork>().OnlineSpeedz.Value;
            //if(rbAP.velocity.magnitude > rb.velocity.magnitude)
            // {
            //     knockbackMultiplier = 1;
            // }
            // else
            // {
            //     knockbackMultiplier = -0.5f;
            // }
            // Vector3 SPEEEED = new Vector3(xS, 0, zS);
            //if (rbAP.velocity.magnitude > 0)
            // {
            Debug.Log(dir + "BONLK");
            // rb.velocity = Vector3.zero;
            //  rb.AddForce(dir.normalized * 10, ForceMode.Impulse);
            // rbAP.AddForce(-dir.normalized * 10, ForceMode.Impulse);
            // }
            // else
            // {
            //    rb.velocity = rbAP.velocity * 100;
            // }
            Vector3 knockDir = new Vector3(rbAP.velocity.x, 0, rbAP.velocity.z);
            // Debug.Log("Getting pushed at " + SPEEEED);
            // rb.velocity = Vector3.zero;

            // rb.AddForce(knockDir * knockBackStrength * knockbackMultiplier, ForceMode.Impulse);
            kbTimer = 1;
            knockBackCD = true;
        }
    }
}
