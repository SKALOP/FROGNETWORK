using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverbtn;
    [SerializeField] private Button hostbtn;
    [SerializeField] private Button clientbtn;

    private void Awake()
    {
        serverbtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });
        hostbtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });
        clientbtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });
    }
}
