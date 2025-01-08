using System;
using System.Collections.Generic;

[System.Serializable]
public class RoomData
{
    public Dictionary<string, MessageData> Messages { get; set; }
    public Dictionary<string, PlayerData> Players { get; set; }
    public int UserCount;
    public bool isFull;

    public RoomData()
    {
        Messages = new Dictionary<string, MessageData>
        {
            { "welcome", new MessageData("system", "Welcome to the room!", DateTimeOffset.UtcNow.ToUnixTimeSeconds()) }
        };
        Players = null;
    }
}

[System.Serializable]
public class MessageData
{
    public string SenderId { get; set; }
    public string Content { get; set; }
    public long Timestamp { get; set; }

    public MessageData(string senderId, string content, long timestamp)
    {
        SenderId = senderId;
        Content = content;
        Timestamp = timestamp;
    }
}

[System.Serializable]
public class PlayerData
{
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public bool IsOnline { get; set; }

    public PlayerData(string playerId, string playerName, bool isOnline)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        IsOnline = isOnline;
    }
}