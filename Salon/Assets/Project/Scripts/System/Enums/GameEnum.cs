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
    Shop
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
    ArmSwing,
    CheckWatch,
    ChugDrink,
    CrossChest,
    DespairPose,
    DrinkThrow,
    FootTap,
    LegShake,
    Sippin,
    ShootingArrow,
    TwistDance,
    DropKick,
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
