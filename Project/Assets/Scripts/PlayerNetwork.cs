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
    public CameraHandler ch;
    public TongueHandler th;
    [SerializeField] public Vector2 cameraInput;
        public GameObject[] cameras;
    public GameObject[] players;
    public bool currentcam = false;
    bool speedAmpOn = false;
    public Vector3 camDirF;
    public Vector3 camDirR;
    public bool dead = false;
   public bool knockBackCD;
    float kbTimer = 1;
    public GameObject collidedObj;
    public Rigidbody rbAP;
    public int spawnLoc;
    public float startTimer = 5;
    [SerializeField]
    private PlayerMovement pm;
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
        pm = this.gameObject.GetComponent<PlayerMovement>();
        //create cameras and focus them on their respective players
        spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
        spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
        ch = spawnedObjectTransform.GetComponentInChildren<CameraHandler>();
        ch.targetTransform = this.gameObject.transform;
        //pick a random spawn location
        spawnLoc = Random.Range(0, 3);
    }
    // Update is called once per frame
    //due to scene references and how the netcode package works, the game spawns a controllable player for each ID present that is the transform for this object.
    //T and Y buttons also allow for a gameobject aside from the player to be spawned and seen by both over the netcode
    //this will become useful for future game mechanics programming
    void Update()
    {
        this.gameObject.name = "Player" + spawnLoc;
        //delay before match starts, all players need to find a unique spawn first
        rb = this.GetComponent<Rigidbody>();
        if (startTimer > 0)
        {
            Debug.Log("TICKING...");
            startTimer -= Time.deltaTime;
            //find all possible spawns
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("SPAWN");
            //set the player's positions to one of the spawn points
            this.transform.position = spawns[spawnLoc].transform.position;
            //find all players
            GameObject[] Players = GameObject.FindGameObjectsWithTag("Player");
            //check if any other players are overlapping with this one (having the same spawn)
            foreach (GameObject p in Players)
            {
                Collider[] colliders;
                //if so, pick a new spawn point
                  if ((colliders = Physics.OverlapSphere(p.transform.position, 0.5f/* Radius */)).Length > 3)
                {
                    spawnLoc = Random.Range(0, 3);
                }
            }
            //make sure player doesn't move during this process
            rb.velocity = Vector3.zero;
        }

        try
        {
            //if this isn't the server, we dont' want to use the server's instance of the tongue, since it's laggy, so disable it if it exists
            if (!IsServer)
            {
                tongueTransform = GameObject.Find("Tongue(Clone)");
                tongueTransform.gameObject.SetActive(false);
               
            }
            if (IsServer)
            {
                localTongueTransform = GameObject.Find("LocalTongue(Clone)");
                localTongueTransform.gameObject.SetActive(false);
            }
        }
        catch
        {
            Debug.Log("There is no Tongue");
        }

        try
        {
            //make sure that tongues act and move properly, destroy them after 3 seconds, and make sure client's online tongues aren't seen by them
            if (th != null)
            {
                Debug.Log(hitTarget);
                th.updateTongue(pm.hitTarget, this.gameObject.transform.position);
                Destroy(tongueTransform.gameObject, 3);
                tongueTransform.GetComponent<NetworkObject>().NetworkHide(this.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
                localTongueTransform.GetComponent<TongueHandler>().updateTongue(pm.hitTarget, this.gameObject.transform.position);
            }
        }
        catch
        {
            Debug.Log("There is no Tongue or Local Tongue");
        }

        //timer for the knockback effect, so player's cant get launched around too much
        if (knockBackCD)
        {
            kbTimer -= Time.deltaTime;
           if(kbTimer < 0)
            {
                knockBackCD = false;
            }
        }

      try
        {

            localTongueTransform.GetComponent<TongueHandler>().updateTongue(pm.hitTarget, this.gameObject.transform.position);
        }
        catch
        {
            Debug.Log("No Local Tongue");
        }

        //if there is no online tongue, make sure to destroy the local tongue, and despawn the tongue online
        if (tongueTransform == null)
        {
            try
            {
                Destroy(localTongueTransform,3);
                tongueTransform.GetComponent<NetworkObject>().Despawn();
            }
            catch
            {
                Debug.Log("There is no Tongue or local Tongue");
            }
        }
      
        //if the player isn't dead, the camera should follow them
        if (deadValue.Value == false)
        {
            ch = spawnedObjectTransform.GetComponentInChildren<CameraHandler>();
            ch.targetTransform = this.gameObject.transform;
        }
        //if the player is dead, the camera should follow another player
        if (ch.targetTransform.GetComponentInChildren<PlayerNetwork>().deadValue.Value == true)
        {
            ch.targetTransform = players[Random.Range(0, players.Length)].transform;
        }
        
        //I need to make sure the cameras are looking at the correct player
        //to do this, I got every camera in the scene

        /*
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
        */
        if (!IsOwner) return;

        //test code used while testing and for reference
        /*
        if(Input.GetKeyDown(KeyCode.T))
        {
            randomNumber.Value = new myCustomData {  _int = Random.Range(0,100), _bool = false, message = "ABCDEFG"};
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            spawnedObjectTransform.GetComponent<NetworkObject>().Despawn(true);
            Destroy(spawnedObjectTransform.gameObject);
        }
        */

        //function for player movement and abilities
      //  pm.HandleMovement(ch);
    }


    //online gameObjects can't be created by clients, so they need to send a message to the server to spawn them when needed
    [ServerRpc]
    public void TestServerRpc(ulong clientId, Vector3 target)
    {
        tongueTransform = Instantiate(TonguePrefab);
        tongueTransform.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
        th = tongueTransform.GetComponentInChildren<TongueHandler>();
        hitTarget = target;
        Debug.Log("TestServerRPC" + th);
    }

    [ServerRpc]
    public void Test2ServerRpc(ulong clientId, Vector3 target)
    {
        tongueTransform = Instantiate(ServerTonguePrefab);
        tongueTransform.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
        th = tongueTransform.GetComponentInChildren<TongueHandler>();
        hitTarget = target;
        Debug.Log("TestServerRPC" + th);
    }

    //for some reason, it seems rigidbodies of other's cant be changed normally, and messages need to be sent to the object's owner for them to move objects
    //this is the message for server players
    [ServerRpc(RequireOwnership = false)]
    private void CollisionServerRpc(NetworkObjectReference sender, NetworkObjectReference target)
    {
        //gets the vector from the colliding player to this player, so it can be launched in the right way
        Debug.Log("Telling Host to Knockback themselves");
        if (!sender.TryGet(out NetworkObject networkObject1)){
            Debug.Log("error");
        }
            if (!target.TryGet(out NetworkObject networkObject2)){
            Debug.Log("error");
        }
        Vector3 dir = networkObject1.transform.position - networkObject2.transform.position;
       Rigidbody rb1 = networkObject1.GetComponent<Rigidbody>();
        Rigidbody rb2 = networkObject2.GetComponent<Rigidbody>();
        rb1.AddForce(dir.normalized * 10, ForceMode.Impulse);
        rb2.AddForce(-dir.normalized * 10, ForceMode.Impulse);
    }

    //for some reason, it seems rigidbodies of other's cant be changed normally, and messages need to be sent to the object's owner for them to move objects
    //this is the message for client players
    [ClientRpc]
    private void CollisionClientRpc(Vector3 sender, ClientRpcParams clientRpcParams = default)
    {
        //gets the vector from the colliding player to this player, so it can be launched in the right way
        Debug.Log("telling client to knockback themselves");
          Vector3 dir = sender - NetworkManager.LocalClient.PlayerObject.transform.position;
        rbAP = NetworkManager.LocalClient.PlayerObject.GetComponent<Rigidbody>();
        rbAP.AddForce(-dir.normalized * 10, ForceMode.Impulse);
    }

  

    //collision check for water, if the player falls into water, they are eliminated
    void OnCollisionEnter(Collision c)
    {
        if(c.gameObject.tag == "Water")
        {
            deadValue.Value = true;
            players = GameObject.FindGameObjectsWithTag("Player");
            this.transform.position = new Vector3(0,-100,0);
        }
    }

    //collision check for other players, which have a larger trigger hitbox for collisions with each other
    void OnTriggerEnter(Collider c)
    {
        Debug.Log(c.gameObject.tag);

        if (c.gameObject.tag == "Player" || c.gameObject.tag == "Untagged" && knockBackCD == false)
        {

            float knockBackStrength = 5;
            float knockbackMultiplier;
            //get the hit object, and it's rigidbody, get the vector from this player to that one
          //  GameObject attackingPlayer = c.gameObject;
         //   rbAP = c.gameObject.GetComponent<Rigidbody>();
            Vector3 dir = this.transform.position - c.transform.position;
            Debug.Log(c.gameObject.tag + "THIS IS THE OTHER HITBOX");
          //  Debug.Log(rb + "Is hitting "+ rbAP + "THIS IS it's rigidbody" + "With a force going" + dir);
            if (!IsServer)
            {
                rb.AddForce(dir.normalized * 10, ForceMode.Impulse);
                CollisionServerRpc(this.gameObject, c.gameObject);
            }
           // if (IsServer)
          //  {
                //rb.AddForce(dir.normalized * 10, ForceMode.Impulse);
          //  }
            // rb.AddForce(dir.normalized * 10, ForceMode.Impulse);
            //   rbAP.AddForce(-dir.normalized * 10, ForceMode.Impulse);
            /*
            //if this is the host, then send a message to the hit client to have it move it's rigidbody
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
            //if this is a client, tell the server to move it's rigidbody
            if(!IsServer) {
                CollisionServerRpc(this.gameObject, c.gameObject);
            }
            Vector3 knockDir = new Vector3(rbAP.velocity.x, 0, rbAP.velocity.z);
            kbTimer = 1;
            knockBackCD = true;
            */
        }
    }
}
