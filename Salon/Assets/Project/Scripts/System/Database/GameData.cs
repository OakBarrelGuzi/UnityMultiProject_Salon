using System;
using System.Collections.Generic;

namespace Salon.Firebase.Database
{
    [System.Serializable]
    public class RoomData
    {
        public Dictionary<string, MessageData> Messages { get; set; }
        public Dictionary<string, GamePlayerData> Players { get; set; }
        public int UserCount;
        public bool isFull;

        public RoomData()
        {
            Messages = new Dictionary<string, MessageData>
            {
                { "welcome", new MessageData("system", "Welcome to the room!", DateTimeOffset.UtcNow.ToUnixTimeSeconds()) }
            };
            Players = new Dictionary<string, GamePlayerData>();
            UserCount = 0;
            isFull = false;
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
    public class UserData
    {
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public long LastOnline { get; set; }
        public Dictionary<string, bool> Friends { get; set; }
        public Dictionary<GameType, UserStats> GameStats { get; set; }

        public UserData(string displayName, string email)
        {
            DisplayName = displayName;
            Email = email;
            LastOnline = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Friends = new Dictionary<string, bool>();
            GameStats = new Dictionary<GameType, UserStats>();
        }
    }

    [System.Serializable]
    public class UserStats
    {
        public int TotalGames { get; set; }
        public int TopScore { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public float Rank { get; set; }

        public UserStats()
        {
            TotalGames = 0;
            Wins = 0;
            Losses = 0;
            Rank = 0;
        }
    }

    [System.Serializable]
    public class GamePlayerData
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public bool IsReady { get; set; }
        public bool IsHost { get; set; }
        public GamePlayerState State { get; set; }
        public Dictionary<string, object> GameSpecificData { get; set; }

        public GamePlayerData(string userId, string displayName, bool isHost = false)
        {
            UserId = userId;
            DisplayName = displayName;
            IsReady = false;
            IsHost = isHost;
            State = GamePlayerState.Waiting;
            GameSpecificData = new Dictionary<string, object>();
        }
    }
}