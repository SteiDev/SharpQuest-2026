namespace Capstone.Core.Models;

public class GameLibrary
{
    private IEnumerable<GameRecord> gameRecords = new List<GameRecord>();

    IEnumerable<GameRecord> getGamesByStatus(GameStatus status);
}