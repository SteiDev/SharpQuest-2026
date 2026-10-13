namespace Capstone.Core.Models;

public enum GameStatus {
    Backlog,
    Playing,
    Completed,
    Abandoned,
    OnHold
}

// Add more platforms
public enum Platform {
    PC,
    SteamDeck,
    PlayStation5,
    XboxSeriesX,
    NintendoSwitch
}

public enum OwnershipKind {
    Physical,
    Digital,
    Subscription
}