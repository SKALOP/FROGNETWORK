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


    public static TestRelay Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
    private async void Start()
    {
      //  await UnityServices.InitializeAsync();

     //   AuthenticationService.Instance.SignedIn += () => {
      //      Debug.Log("Signed In " + AuthenticationService.Instance.PlayerId);
      //  };
      // await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }


  
    
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
