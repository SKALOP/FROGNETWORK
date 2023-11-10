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
    //lobby custom values, including slots for what lobby players are in
    //timers for keeping the lobby awake
    //lobby code values for players to join
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float hbTimer;
    private float hbTimerMax = 15;
    private float pullTimer;
    private float pullTimerMax = 1.1f;
    private string code;
    private string hostName;
    //all the UI assets that need to be referenced for the menus
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
    public bool lockCheck = false;
    public Camera mainCam;
    public float timer = 5;
    public GameObject[] listOfPlayers;
    public GameObject[] playerCams;

    //checks for the four slots for players in the lobby
    public bool slot1, slot2, slot3, slot4 = false;
   

    //on game start, load functionality for buttons
    private void Awake()
    {
        //playerName.text = "P1";
        //plays code when the player is done editing their name
        playerName.onEndEdit.AddListener(delegate { lockInput(playerName); });
         CreateLobbyButton();
         StartGameButtonClicked();
        JoinButtonClicked();
        LeaveButtonClicked();
        KillButtonClicked();
    }
   
    // Start is called before the first frame update
    private async void Start()
    {
        //loads all the features using Unity Gaming Services
        //this includes lobby, relay, and netcode
        await UnityServices.InitializeAsync();

        //creates an anonymous account for the player to play on
        //if i want to make seperate accounts later, this will need to change
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed In " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    //check for any changes in the lobby, as well as keep it running
    private void Update()
    {
       
        if (lockCheck)
        {
            timer -= Time.deltaTime;
            playerCams = GameObject.FindGameObjectsWithTag("CAMERA");
        }
        
        if(timer < 0)
        {
            listOfPlayers = GameObject.FindGameObjectsWithTag("Player");
            float aliveCount = listOfPlayers.Length;
            foreach (GameObject op in listOfPlayers)
            {

                if (op.GetComponentInChildren<PlayerNetwork>().deadValue.Value)
                {
                    aliveCount -= 1;
                }
                if (aliveCount == 1)
                {
                    Debug.Log("Shutting Down");
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
                    mainCam.enabled = true;
                    mainCam.gameObject.SetActive(true);
                    foreach(GameObject pc in playerCams)
                    {
                        pc.gameObject.SetActive(false);
                    }
                     NetworkManager.Singleton.Shutdown();
                }
            }
        }
       
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }
    //if players are waiting in a lobby, it will be shutdown after a time by default
    //this function refreshes the lobby timer 
    private async void HandleLobbyHeartbeat()
    {
        if(hostLobby == null)
        {
            return;
        }
            hbTimer -= Time.deltaTime; 
            if (hbTimer < 0f)
            {
                hbTimer = hbTimerMax;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        
    }

    //handles all updates that occur to the lobby
    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby == null)
        {
            return;
        }
            pullTimer -= Time.deltaTime; ;
            if (pullTimer < 0f)
            {
                pullTimer = pullTimerMax;
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
            //sends data to all connected members for who is in the lobby
            PrintPlayers(joinedLobby);
            //checks if the game has started yet to send data to client players
            //gets rid of unnecessary UI when game starts
            //connects players to the relay server when the game starts
            bool gameIsStarted = joinedLobby.Data["StartGame"].Value != "0";
                if (gameIsStarted)
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
            //checks if this player is the first player in the lobby, if so, gives them the lobby code to share
            bool hostCheck = playerName.text == joinedLobby.Players[0].Data["PlayerName"].Value;
                if (hostCheck)
                {
                    codeText.text = "Lobby Code: " + joinedLobby.LobbyCode;
                    codeText.gameObject.SetActive(true);
                }

            }
        
    }

    //function for the host to create a lobby
    //sets up the variables for the lobby, such as max size for players, whether the lobby needs a code, and custom data that is game specific
    //map and gamemode will probably not be changed for the sake of this project, but could lead to different games with further work on this
    //gets the lobby code and writes the lobby code to the UI so the host can let players join their lobby
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
                IsLocked = lockCheck,

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

    //not being used right now, if I add public lobbies in the future, this would be used to refresh the list so players can see lobbies that are joinable.
    //it sorts the ones with available slots to join first
    //could be sorted for different game modes in the future
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

    //function that uses a string code to join a lobby with a certain string
    //sends data about joining player to the lobby
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

    //prints all players to the debug to make sure things are working
    //goes through every player and looks at their order in the list
    //sends them to the UI to show lobby members which players are in the lobby
    //deletes names after people leave
    private void PrintPlayers(Lobby l)
    {
      //  float aliveCount = l.Players.Count;
       // listOfPlayers = GameObject.FindGameObjectsWithTag("Player");
       // foreach (GameObject op in listOfPlayers) {

        //    if (op.GetComponentInChildren<PlayerNetwork>().deadValue.Value)
         //   {
          //      aliveCount -= 1;
          //  }
           // if (aliveCount == 1)
          //  {
           //     Debug.Log("Shutting Down");
               // NetworkManager.Singleton.Shutdown();
          //  }
      //  }
        
        Debug.Log("Players In Lobby " + l.Name + " " + l.Data["GameMode"].Value + " " + l.Data["Map"].Value);
        foreach (Player p in l.Players)
        {

            Debug.Log(p.Id + " " + p.Data["PlayerName"].Value);
 
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
        }
    }

    //sends data about variables associated to a player
    //right now, this is just their custom names
    //could be things like level, chosen character, etc in the future
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

    //if more gamemodes are added, this function will update the lobby to know which gamemode is in play
    private async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                IsLocked = lockCheck,
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

    //if players want to update player data while in a lobby this is how to do it.
    //not implemented right now, as UI sends people out of the lobby to retype their name at the start
    //could be used for other player data in the future (see above GetPlayers() examples)
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

    //if a client wants to leave the lobby, this function removes them from the lobby data
    //if they are the last player in the lobby, then it also kills the lobby
    private async void LeaveLobby()
    {
        try
        {
            if(joinedLobby.Players.Count ==1)
            {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
            }
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
           
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
        joinedLobby = null;
    }

    //not in use yet, doesn't work as intended
    //function will kick the 2nd joined player
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

    //not in use yet, if host wants to no longer be host, the function will shift ownership to the 2nd joined player
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


    //changes the gamemode to the relay server code so other players can connect to it
    //starts the server so players can connect online
    //sends the message to the relay script to execute the function to start the game and gets a code for the relay server
    public async void StartGame()
    {
        try
        {
           lockCheck = true;
            UpdateLobbyGameMode("Elimination");
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
    public void CreateLobbyButton()
    {
        //plays a function when a player creates a lobby
        //makes lobby UI appear
        //takes awake irrelevant UI
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
    }
    public void StartGameButtonClicked()
    {
        //plays a function when a player starts the game
        //gets rid of some UI only the lobby host sees
        StartGameButton.onClick.AddListener(() =>
        {
            StartGame();
            codeText.gameObject.SetActive(false);
            killButton.gameObject.SetActive(false);
        });
    }
    public void JoinButtonClicked()
    {
        //reads a code the player inputs for joining a lobby
        //plays a function using that code
        //makes lobby UI appear
        //takes away irrelevant UI
        JoinButton.onClick.AddListener(() =>
        {
            code = inf.text;
            JoinLobbyByCode(code);
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
            lockCheck = true;

        });
    }
    public void LeaveButtonClicked()
    {
        //allows clients joining the lobby to leave
        //takes them back to the name selection screen
        leaveButton.onClick.AddListener(() =>
        {
            lobbyBack.gameObject.SetActive(false);
            lobbyText.gameObject.SetActive(false);
            leaveButton.gameObject.SetActive(false);
            codeText.gameObject.SetActive(false);
            playerName.gameObject.SetActive(true);
            lobbyMember1.gameObject.SetActive(false);
            lobbyMember2.gameObject.SetActive(false);
            lobbyMember3.gameObject.SetActive(false);
            lobbyMember4.gameObject.SetActive(false);
            LeaveLobby();

        });
    }
    public void KillButtonClicked()
    {
        //only visible to hosts
        //deletes the whole lobby if they leave
        //takes them back to the name selection screen
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
            LeaveLobby();

        });
    }
    //function that checks if the player has a name.
    //if so, let them join or create a lobby
    void lockInput(TMP_InputField field)
    {
        if (field.text.Length > 0)
        {
            playerName.gameObject.SetActive(false);
            lobbyButton.gameObject.SetActive(true);
            inf.gameObject.SetActive(true);
            JoinButton.gameObject.SetActive(true);
            //enable lobby and join, disable everything else
        }
    }
}


