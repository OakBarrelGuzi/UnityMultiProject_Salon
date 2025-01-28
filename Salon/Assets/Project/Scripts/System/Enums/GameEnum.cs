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
    MemoryGame,
    Emoji,
    Animation,
    Inventory,
    Shop,
    DartRanking,
    Loading
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

}

public enum AnimType
{
    None,
    Beer,
    YmcaDance,
    SnakeHipHopDance,
    NorthernSoulSpinCombo,
    NorthernSoulFloorCombo,
    HipHopDancing2,
    HipHopDancing,
    DismissingGesture,
    DancingTwerk,
    CrosslegFreeze,
    Breakdance,
    CheckWatch,
    Sippin,
    DrinkThrow,
    ChugDrink,
    DropKick,
    TwistDance,
    ShootingArrow
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

[Serializable]
public enum ItemType
{
    Anim,
    Emoji,
}
