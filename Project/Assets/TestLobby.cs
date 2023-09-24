using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;
using TMPro;


public class TestLobby : MonoBehaviour
{
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float hbTimer;
    private float hbTimerMax = 15;
    private float pullTimer;
    private float pullTimerMax = 1.1f;
    private string Code;
    [SerializeField] private TextMeshProUGUI codeText;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private TMP_InputField inf;
    [SerializeField] private TMP_InputField playerName;
    [SerializeField] private Button JoinButton;
    [SerializeField] private Button leaveButton;
    private void Awake()
    {

        lobbyButton.onClick.AddListener(() =>
        {
            CreateLobby();
        });
        JoinButton.onClick.AddListener(() =>
        {
            Code = inf.text;
            JoinLobbyByCode(Code);

        });
        leaveButton.onClick.AddListener(() =>
        {
           
            LeaveLobby();

        });
    }

    // Start is called before the first frame update
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed In " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }
    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            hbTimer -= Time.deltaTime; ;
            if (hbTimer < 0f)
            {
                hbTimer = hbTimerMax;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby != null)
        {
            pullTimer -= Time.deltaTime; ;
            if (pullTimer < 0f)
            {
                pullTimer = pullTimerMax;
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
            }
        }
    }


    private async void CreateLobby()
    {
        try
        {
            string lobbyName = "FrogLobby";
            int maxPlayers = 4;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = true,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
            {
                {"GameMode", new DataObject(DataObject.VisibilityOptions.Public,"Elimination") },
                {"Map", new DataObject(DataObject.VisibilityOptions.Public,"Pond1") }
            }
            };
            Lobby l = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            hostLobby = l;
            joinedLobby = hostLobby;
            PrintPlayers(hostLobby);
            Debug.Log("lobby created: " + l.Name + " " + l.MaxPlayers + " " + l.Id + " " + l.LobbyCode);
            codeText.text = l.LobbyCode;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

    private async void ListLobbies()
    {


        try
        {
            QueryLobbiesOptions qlb = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots,"0",QueryFilter.OpOptions.GT)
              //  new QueryFilter(QueryFilter.FieldOptions.S1, "Elimination", QueryFilter.OpOptions.EQ)
            },
                Order = new List<QueryOrder>
            {
                new QueryOrder(false,QueryOrder.FieldOptions.Created)
            }
            };
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            Debug.Log("lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby l in queryResponse.Results)
            {
                Debug.Log(l.Name + " " + l.MaxPlayers + " " + l.Data["GameMode"].Value);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

    private async void JoinLobbyByCode(string lobbyCode)
    {

        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            joinedLobby = lobby;
            PrintPlayers(joinedLobby);
            Debug.Log("Joined lobby w code " + lobbyCode);

        }

        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    private void PrintPlayers()
    {
        PrintPlayers(joinedLobby);
    }

    private void PrintPlayers(Lobby l)
    {
        Debug.Log("Players In Lobby " + l.Name + " " + l.Data["GameMode"].Value + " " + l.Data["Map"].Value);
        foreach (Player p in l.Players)
        {
            Debug.Log(p.Id + " " + p.Data["PlayerName"].Value);
        }
    }
    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,playerName.text) }
                    }
        };
    }

    private async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
        {
            { "GameMode", new DataObject(DataObject.VisibilityOptions.Public,gameMode)}
        }
            });
            joinedLobby = hostLobby;
            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void UpdatePlayerName(string newPlayerName)
    {
        try
        {
            playerName.text = newPlayerName;
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
        {
            {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName.text) }
        }
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    private async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
        
    }
    private async void KickPlayer()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[1].Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void MigrateLobbyHost()
    {
        try
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = joinedLobby.Players[1].Id
      
            });
            joinedLobby = hostLobby;
            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}


