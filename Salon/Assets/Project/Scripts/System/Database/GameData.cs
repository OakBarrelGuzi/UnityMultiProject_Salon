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
        public byte Data1 { get; set; }
        public byte Data2 { get; set; }
        public byte Data3 { get; set; }

        private const float MAP_OFFSET = 50f;
        private const float MAP_SIZE = 400f;
        private const float POSITION_SCALE = MAP_SIZE / 255f;

        public bool IsPositionUpdate
        {
            get => (Data1 & 0x80) != 0;
            private set => Data1 = (byte)((Data1 & 0x7F) | (value ? 0x80 : 0));
        }

        public NetworkPositionData()
        {
            Data1 = 0;
            Data2 = 0;
            Data3 = 0;
            IsPositionUpdate = true;
        }

        public NetworkPositionData(Vector3 position, Vector3 direction, bool isPositionUpdate)
        {
            IsPositionUpdate = isPositionUpdate;

            if (isPositionUpdate)
            {
                float normalizedX = (position.x + MAP_OFFSET) / MAP_SIZE;
                float normalizedZ = (position.z + MAP_OFFSET) / MAP_SIZE;

                Data2 = (byte)(Mathf.Clamp01(normalizedX) * 255);
                Data3 = (byte)(Mathf.Clamp01(normalizedZ) * 255);
                Data1 &= 0x80;
            }
            else
            {
                int dirX = Mathf.RoundToInt((direction.x * 0.5f + 0.5f) * 15);
                int dirZ = Mathf.RoundToInt((direction.z * 0.5f + 0.5f) * 7);
                Data1 = (byte)((Data1 & 0x80) | ((dirX << 3) & 0x78) | (dirZ & 0x07));
            }
        }

        public Vector3? GetPosition()
        {
            if (!IsPositionUpdate)
                return null;

            float x = (Data2 * POSITION_SCALE) - MAP_OFFSET;
            float z = (Data3 * POSITION_SCALE) - MAP_OFFSET;

            return new Vector3(x, 0f, z);
        }

        public Vector3 GetDirection()
        {
            if (IsPositionUpdate)
                return Vector3.forward;

            float x = ((Data1 >> 3) & 0x0F) / 15f * 2f - 1f;
            float z = (Data1 & 0x07) / 7f * 2f - 1f;

            return new Vector3(x, 0f, z).normalized;
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
        public NetworkPositionData Position { get; set; }

        public GamePlayerData(string displayName, bool isHost = false)
        {
            DisplayName = displayName;
            IsReady = false;
            IsHost = isHost;
            State = GamePlayerState.Waiting;
            GameSpecificData = new Dictionary<string, object>();
            Position = new NetworkPositionData();
        }
    }
}