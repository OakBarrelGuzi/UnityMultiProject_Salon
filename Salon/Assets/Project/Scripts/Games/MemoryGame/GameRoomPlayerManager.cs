using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salon.Firebase.Database;
using Salon.Character;

public class GameRoomPlayerManager : MonoBehaviour
{
    public LocalPlayer localPlayer;
    public RemotePlayer remotePlayer;

    private void Start()
    {
        localPlayer = FindObjectOfType<LocalPlayer>();
        remotePlayer = FindObjectOfType<RemotePlayer>();
    }

}
