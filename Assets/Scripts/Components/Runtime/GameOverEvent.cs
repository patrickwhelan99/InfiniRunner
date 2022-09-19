using System;
using Unity.Entities;

public enum GameOverReason { NONE, PLAYER_KILLED, PLAYER_FELL };

[Serializable]
public struct GameOverEvent : IComponentData
{
    public GameOverReason Value;
}
