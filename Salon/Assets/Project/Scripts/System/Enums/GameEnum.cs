using System;

public enum PanelType
{
    None,
    SignIn,
    SignUp,
    Channel,
    Lobby,
    Option,
    Friends,
    Chat,
    Customize,
    MainDisplay,
    Dart,
    DartGame,
    PartyRoom,
    ShellGame,
    MemoryGame
}
public enum InviteStatus
{
    Pending,
    Accepted,
    Declined,
    Expired
}
public enum GamePlayerState
{
    Waiting,
    Ready,
    Playing,
    Away
}

public enum GameType
{
    None,
    ShellGame,
    DartGame,
    MemoryGame,
    HoldBeerGame

}

public enum AnimType
{
    None,
    Beer
}

[Serializable]
public enum InteractionType
{
    None,
    Shop,
    DartGame,
    ShellGame,
    CardGame
}

public enum SocketType
{
    None,
    Inventory,
    Activated,
}
