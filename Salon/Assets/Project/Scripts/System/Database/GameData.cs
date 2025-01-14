using System;
using System.Collections.Generic;
using UnityEngine;

namespace Salon.Firebase.Database
{
    [Serializable]
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

    [Serializable]
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

    [Serializable]
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

    [Serializable]
    public class UserData
    {
        public long LastOnline { get; set; }
        public Dictionary<string, bool> Friends { get; set; }
        public Dictionary<GameType, UserStats> GameStats { get; set; }

        public UserData()
        {
            LastOnline = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Friends = new Dictionary<string, bool>();
            GameStats = new Dictionary<GameType, UserStats>();
        }
    }
    [Serializable]
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

    [Serializable]
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

    [Serializable]
    public class NetworkPositionData
    {
        public float PosX { get; set; }
        public float PosZ { get; set; }
        public float DirX { get; set; }
        public float DirZ { get; set; }
        public bool IsPositionUpdate { get; set; }

        private const float POSITION_THRESHOLD = 0.01f;
        private const float DIRECTION_THRESHOLD = 0.1f;

        public NetworkPositionData()
        {
            PosX = 0f;
            PosZ = 0f;
            DirX = 0f;
            DirZ = 1f;
            IsPositionUpdate = true;
        }

        public NetworkPositionData(Vector3 position, Vector3 direction, bool isPositionUpdate)
        {
            IsPositionUpdate = isPositionUpdate;

            if (isPositionUpdate)
            {
                PosX = position.x;
                PosZ = position.z;
            }

            Vector3 normalizedDir = direction.normalized;
            DirX = normalizedDir.x;
            DirZ = normalizedDir.z;
        }

        public Vector3? GetPosition()
        {
            if (!IsPositionUpdate)
                return null;

            return new Vector3(PosX, 0f, PosZ);
        }

        public Vector3 GetDirection()
        {
            Vector3 direction = new Vector3(DirX, 0f, DirZ);
            return direction.magnitude > 0.01f ? direction.normalized : Vector3.zero;
        }

        public bool HasSignificantChange(NetworkPositionData other)
        {
            if (other == null) return true;

            float posDiff = Vector2.Distance(
                new Vector2(PosX, PosZ),
                new Vector2(other.PosX, other.PosZ));

            float dirDiff = Vector2.Distance(
                new Vector2(DirX, DirZ).normalized,
                new Vector2(other.DirX, other.DirZ).normalized);

            return posDiff > POSITION_THRESHOLD || dirDiff > DIRECTION_THRESHOLD;
        }
    }

    [Serializable]
    public class GamePlayerData
    {
        public string DisplayName { get; set; }
        public bool IsReady { get; set; }
        public bool IsHost { get; set; }
        public GamePlayerState State { get; set; }
        public Dictionary<string, object> GameSpecificData { get; set; }
        public string Position { get; set; }

        public GamePlayerData(string displayName, bool isHost = false)
        {
            DisplayName = displayName;
            IsReady = false;
            IsHost = isHost;
            State = GamePlayerState.Waiting;
            GameSpecificData = new Dictionary<string, object>();
            Position = NetworkPositionCompressor.CompressVector3(Vector3.zero, Vector3.forward, false);
        }
    }
}