using System;
using System.Collections.Generic;

namespace Salon.Firebase.Database
{
    [System.Serializable]
    public class CommonChannelData
    {
        public Dictionary<string, MessageData> Messages { get; set; }
        public int UserCount;
        public bool isFull;

        public CommonChannelData()
        {
            Messages = new Dictionary<string, MessageData>
            {
                { "welcome", new MessageData("system", "Welcome to the room!", DateTimeOffset.UtcNow.ToUnixTimeSeconds()) }
            };
            UserCount = 0;
            isFull = false;
        }
    }

    [System.Serializable]
    public class ChannelData
    {
        public CommonChannelData CommonChannelData { get; set; }
        public Dictionary<string, GamePlayerData> Players { get; set; }

        public ChannelData()
        {
            CommonChannelData = new CommonChannelData();
            Players = new Dictionary<string, GamePlayerData>();
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
        public string UserId { get; set; }
        public long LastOnline { get; set; }
        public Dictionary<string, bool> Friends { get; set; }
        public Dictionary<GameType, UserStats> GameStats { get; set; }

        public UserData(string userId)
        {
            UserId = userId;
            LastOnline = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Friends = new Dictionary<string, bool>();
            GameStats = new Dictionary<GameType, UserStats>();
        }
    }
    [System.Serializable]
    public class UserMapping
    {
        public string userId { get; set; }
        public string userName { get; set; }

        public UserMapping(string userId, string userName)
        {
            this.userId = userId;
            this.userName = userName;
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
        public string DisplayName { get; set; }
        public bool IsReady { get; set; }
        public bool IsHost { get; set; }
        public GamePlayerState State { get; set; }
        public Dictionary<string, object> GameSpecificData { get; set; }
        public PositionData Position { get; set; }
        public GamePlayerData(string displayName, bool isHost = false)
        {
            DisplayName = displayName;
            IsReady = false;
            IsHost = isHost;
            State = GamePlayerState.Waiting;
            GameSpecificData = new Dictionary<string, object>();
            Position = new PositionData(0f, 0.5f, 0f);
        }
    }

    [System.Serializable]
    public class PositionData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public PositionData(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}