using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using System.Threading.Tasks;

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
    [SerializeField] private TextMeshProUGUI lobbyMember1;
    [SerializeField] private TextMeshProUGUI lobbyMember2;
    [SerializeField] private TextMeshProUGUI lobbyMember3;
    [SerializeField] private TextMeshProUGUI lobbyMember4;
    [SerializeField] private TextMeshProUGUI lobbyText;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private TMP_InputField inf;
    [SerializeField] private TMP_InputField playerName;
    [SerializeField] private Button JoinButton;
    [SerializeField] private Image lobbyBack;
    [SerializeField] private Button joinAGameButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button killButton;
    [SerializeField] private Button StartGameButton;
    [SerializeField] private Button NameButton;
    public bool slot1 = false;
    public bool slot2 = false;
    public bool slot3 = false;
    public bool slot4 = false;
    private void Awake()
    {
        playerName.onEndEdit.AddListener(delegate { lockInput(playerName); });
        
       // joinAGameButton.onClick.AddListener(() =>
       // {
//
      //  });
        lobbyButton.onClick.AddListener(() =>
        {
            CreateLobby();
            lobbyBack.gameObject.SetActive(true);
            lobbyText.gameObject.SetActive(true);
            killButton.gameObject.SetActive(true);
            lobbyButton.gameObject.SetActive(false);
            inf.gameObject.SetActive(false);
            JoinButton.gameObject.SetActive(false);
            StartGameButton.gameObject.SetActive(true);
            lobbyMember1.gameObject.SetActive(true);
            lobbyMember2.gameObject.SetActive(true);
            lobbyMember3.gameObject.SetActive(true);
            lobbyMember4.gameObject.SetActive(true);
            codeText.gameObject.SetActive(true);
        });
        StartGameButton.onClick.AddListener(() =>
        {
            StartGame();
        });
        JoinButton.onClick.AddListener(() =>
        {
            Code = inf.text;
            JoinLobbyByCode(Code);
            lobbyBack.gameObject.SetActive(true);
            lobbyText.gameObject.SetActive(true);
            leaveButton.gameObject.SetActive(true);
            lobbyButton.gameObject.SetActive(false);
            inf.gameObject.SetActive(false);
            JoinButton.gameObject.SetActive(false);
            lobbyMember1.gameObject.SetActive(true);
            lobbyMember2.gameObject.SetActive(true);
            lobbyMember3.gameObject.SetActive(true);
            lobbyMember4.gameObject.SetActive(true);

        });
       // NameButton.onClick.AddListener(() =>
      //  {
        //    name = playerName.text;
        //    UpdatePlayerName(playerName.text);

      //  });
        leaveButton.onClick.AddListener(() =>
        {
            lobbyBack.gameObject.SetActive(false);
            lobbyText.gameObject.SetActive(false);
            leaveButton.gameObject.SetActive(false);
            playerName.gameObject.SetActive(true);
            lobbyMember1.gameObject.SetActive(false);
            lobbyMember2.gameObject.SetActive(false);
            lobbyMember3.gameObject.SetActive(false);
            lobbyMember4.gameObject.SetActive(false);
            LeaveLobby();

        });
        killButton.onClick.AddListener(() =>
        {
            lobbyBack.gameObject.SetActive(false);
            lobbyText.gameObject.SetActive(false);
            codeText.gameObject.SetActive(false);
            killButton.gameObject.SetActive(false);
            StartGameButton.gameObject.SetActive(false);
            playerName.gameObject.SetActive(true);
            lobbyMember1.gameObject.SetActive(false);
            lobbyMember2.gameObject.SetActive(false);
            lobbyMember3.gameObject.SetActive(false);
            lobbyMember4.gameObject.SetActive(false);
            DeleteLobby();

        });
    }
    void lockInput(TMP_InputField field)
    {
        if(field.text.Length > 0)
        {
            playerName.gameObject.SetActive(false);
            lobbyButton.gameObject.SetActive(true);
            inf.gameObject.SetActive(true);
            JoinButton.gameObject.SetActive(true);
            //enable lobby and join, disable everything else
        }
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
                
                PrintPlayers(joinedLobby);
                if (joinedLobby.Data["StartGame"].Value != "0")
                {
                    lobbyBack.gameObject.SetActive(false);
                    lobbyText.gameObject.SetActive(false);
                    leaveButton.gameObject.SetActive(false);
                    lobbyMember1.gameObject.SetActive(false);
                    lobbyMember2.gameObject.SetActive(false);
                    lobbyMember3.gameObject.SetActive(false);
                    lobbyMember4.gameObject.SetActive(false);
                    StartGameButton.gameObject.SetActive(false);
                    TestRelay.Instance.JoinRelay(joinedLobby.Data["StartGame"].Value);
                    joinedLobby = null;
                }
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
                {"Map", new DataObject(DataObject.VisibilityOptions.Public,"Pond1") },
                    {"StartGame", new DataObject(DataObject.VisibilityOptions.Member,"0") }
            }
            };
            Lobby l = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            hostLobby = l;
            joinedLobby = hostLobby;
            PrintPlayers(hostLobby);
            Debug.Log("lobby created: " + l.Name + " " + l.MaxPlayers + " " + l.Id + " " + l.LobbyCode);
            codeText.text = "Lobby Code: " + l.LobbyCode;
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

            //if(slot1 == false)
            // {
            //     Debug.Log("Putting a name in slot 1");
            //     lobbyMember1.text = p.Data["PlayerName"].Value;
            lobbyMember1.text = l.Players[0].Data["PlayerName"].Value;
            if(l.Players.Count == 2)
            {
                slot2 = true;
                lobbyMember2.text = l.Players[1].Data["PlayerName"].Value;
            }
            if (l.Players.Count == 3)
            {
                slot3 = true;
                lobbyMember3.text = l.Players[2].Data["PlayerName"].Value;
            }

            if (l.Players.Count == 4)
            {
                slot4 = true;
                lobbyMember4.text = l.Players[3].Data["PlayerName"].Value;
            }
            if(slot4 = true && l.Players.Count == 3)
            {
                lobbyMember4.text = null;
                slot4 = false;
            }
            if (slot3 = true && l.Players.Count == 2)
            {
                lobbyMember3.text = null;
                slot3 = false;
            }
            if (slot2 = true && l.Players.Count == 1)
            {
                lobbyMember2.text = null;
                slot2 = false;
            }

            //     slot1 = true;
            //}
            //  else if(slot2 == false)
            //   {
            //       Debug.Log("Putting a name in slot 2");
            //        lobbyMember2.text = p.Data["PlayerName"].Value;
            //      slot2 = true;
            //   }
            //   else if(slot3 == false)
            //   {
            //       Debug.Log("Putting a name in slot 3");
            //      lobbyMember3.text = p.Data["PlayerName"].Value;
            //       slot3 = true;
            //   }
            //   else if (slot4 == false)
            //   {
            //      Debug.Log("Putting a name in slot 4");
            //       lobbyMember4.text = p.Data["PlayerName"].Value;
            //      slot4 = true;
            //  }
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
    public async void StartGame()
    {
        try
        {
            string relayCode = await TestRelay.Instance.CreateRelay();
            Lobby l = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                {"StartGame", new DataObject(DataObject.VisibilityOptions.Member,relayCode) }
            }
            });
            joinedLobby = l;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}


