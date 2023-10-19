using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Collections;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using System.Threading.Tasks;

public class TestRelay : MonoBehaviour
{

    //creates the relay variable so other scripts (like lobby) can access it
    public static TestRelay Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
    private async void Start()
    {

    }


  
    //Gets allocation (data from host about IP and port, creates key to send data through this IP and code for the connection)
    //also defines maximum connections allowed on this relay (Not including host)
    //get the join code for multiplayer from the allocation creation
    //starts hosting the game
    //sends all ip/port data to the unity transport system which runs the netcode, so that all players can get the same data
    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation alc = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alc.AllocationId);
            Debug.Log(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                 alc.RelayServer.IpV4,
                (ushort)alc.RelayServer.Port,
                alc.AllocationIdBytes,
                alc.Key,
                alc.ConnectionData

                );

            NetworkManager.Singleton.StartHost();

            return joinCode;
        }

        catch (RelayServiceException e)
        {
            Debug.Log(e);

            return null;
        }
    }

    //gets the allocation data by joining through the code
    //shares the IP /port data for the joining client to unityTransport
    //starts the connection as a joined client
    public async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation jalc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                jalc.RelayServer.IpV4,
                (ushort)jalc.RelayServer.Port,
                jalc.AllocationIdBytes,
                jalc.Key,
                jalc.ConnectionData,
                jalc.HostConnectionData
                );

            NetworkManager.Singleton.StartClient();
        }

        catch(RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
