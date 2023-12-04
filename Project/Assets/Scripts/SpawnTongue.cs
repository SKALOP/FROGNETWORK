using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnTongue : NetworkBehaviour
{
  //  public Transform tongueTransform;
   // public Transform tonguePrefab;
   //     public PlayerNetwork pn;
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
    //    pn = this.gameObject.GetComponent<PlayerNetwork>();
    //    tonguePrefab = pn.TonguePrefab;
    }

    /*
    [ServerRpc]
    private void InitializeServerRpc(ulong clientid)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientid }
            }
        };
        List<ulong> spawned = new();
        UpdateClientInitializedClientRpc(spawned.ToArray(), clientRpcParams);
    }
    [ClientRpc]
    private void UpdateClientInitializedClientRpc(ulong[] player, ClientRpcParams clientRpcParams)
    {

    }
    [ServerRpc]
    private void SpawnTongueServerRpc(ulong clientid)
    {
        tongueTransform = Instantiate(tonguePrefab);
        // tongueTransform.transform.SetParent(this.gameObject.transform);
        tongueTransform.GetComponent<NetworkObject>().SpawnWithOwnership(clientid);

    }
    */
}
