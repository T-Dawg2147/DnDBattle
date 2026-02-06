using System;
using System.Collections.Generic;

namespace DnDBattle.Models
{
    /// <summary>
    /// Defines the type of network packet for Phase 9 multiplayer.
    /// </summary>
    public enum PacketType
    {
        Handshake,
        FullSync,
        TokenMove,
        TokenUpdate,
        FogUpdate,
        ChatMessage,
        DiceRoll,
        EffectPlaced,
        InitiativeUpdate,
        PlayerJoined,
        PlayerLeft,
        AttackRequest,
        AttackResult,
        Disconnect
    }

    /// <summary>
    /// Defines fog update operations.
    /// </summary>
    public enum FogUpdateType
    {
        Reveal,
        Hide
    }

    /// <summary>
    /// Wrapper for all network messages exchanged between server and clients.
    /// </summary>
    public class NetworkPacket
    {
        public PacketType Type { get; set; }
        public string Payload { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Data for a token movement over the network.
    /// </summary>
    public class TokenMoveData
    {
        public Guid TokenId { get; set; }
        public int NewX { get; set; }
        public int NewY { get; set; }
    }

    /// <summary>
    /// Data for a chat message sent over the network.
    /// </summary>
    public class ChatMessageData
    {
        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Data for a dice roll performed over the network.
    /// </summary>
    public class DiceRollData
    {
        public string Username { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public int Result { get; set; }
        public List<int> Individual { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Data for a fog of war update sent over the network.
    /// </summary>
    public class FogUpdateData
    {
        public FogUpdateType UpdateType { get; set; }
        public List<CellCoord> Cells { get; set; } = new();
        public bool IsFullSync { get; set; }
        public byte[]? CompressedData { get; set; }
    }

    /// <summary>
    /// Represents a grid cell coordinate for fog updates.
    /// </summary>
    public class CellCoord
    {
        public int X { get; set; }
        public int Y { get; set; }

        public CellCoord() { }
        public CellCoord(int x, int y) { X = x; Y = y; }
    }

    /// <summary>
    /// Represents a connected player in a multiplayer session.
    /// </summary>
    public class PlayerInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public List<Guid> AssignedTokenIds { get; set; } = new();
        public bool IsConnected { get; set; } = true;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Data for an attack request from a player client.
    /// </summary>
    public class AttackRequestData
    {
        public Guid AttackerTokenId { get; set; }
        public Guid DefenderTokenId { get; set; }
        public string AttackName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data for an attack result sent back from the DM server.
    /// </summary>
    public class AttackResultData
    {
        public Guid AttackerTokenId { get; set; }
        public Guid DefenderTokenId { get; set; }
        public string AttackerName { get; set; } = string.Empty;
        public string DefenderName { get; set; } = string.Empty;
        public bool Hit { get; set; }
        public int Damage { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Metadata for cloud-saved encounters.
    /// </summary>
    public class EncounterMetadata
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
    }

    /// <summary>
    /// Stores a client-side movement prediction for lag compensation.
    /// </summary>
    public class MovementPrediction
    {
        public Guid TokenId { get; set; }
        public int OldX { get; set; }
        public int OldY { get; set; }
        public int PredictedX { get; set; }
        public int PredictedY { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
