using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerNetwork : NetworkBehaviour
{


    [SerializeField] private Transform spawnedObjectPrefab;
    private Transform spawnedObjectTransform;

    private NetworkVariable<myCustomData> randomNumber = new NetworkVariable<myCustomData>(
        new myCustomData {
            _int = 50,
            _bool = false,
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);



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


    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += (myCustomData previousValue, myCustomData newValue) =>
        {
            Debug.Log(OwnerClientId + "; randomNumber:" + newValue._int + "; " + newValue._bool + "; "+ newValue.message);
        };

    }
    // Update is called once per frame
    void Update()
    {
        
        if (!IsOwner) return;
        if(Input.GetKeyDown(KeyCode.T))
        {
            spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
            spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
            TestServerRpc("ServerRPC working");
            TestClientRpc();
            randomNumber.Value = new myCustomData {  _int = Random.Range(0,100), _bool = false, message = "ABCDEFG"};
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            spawnedObjectTransform.GetComponent<NetworkObject>().Despawn(true);
            Destroy(spawnedObjectTransform.gameObject);
        }

        Vector3 MoveDir = new Vector3(0,0,0);
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
        float moveSpeed = 3f;
        transform.position += MoveDir * moveSpeed * Time.deltaTime;
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
}
