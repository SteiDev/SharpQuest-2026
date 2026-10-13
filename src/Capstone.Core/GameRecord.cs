namespace Capstone.Core.Models;

public class GameRecord : GenericRecord
{
    private IEnumerable<string> publishers { get; set; } = new List<string>();
    private IEnumerable<string> genres { get; set; } = new List<string>();
    private GameStatus status { get; set; }
    private int playHours { get; set; } = 0;
    private IEnumerable<GameRecord> relatedGames { get; set; } = new List<GameRecord>();
}

public class DLCRecord : GenericRecord
{
    private GameRecord partOf { get; set; } = new GameRecord();
}