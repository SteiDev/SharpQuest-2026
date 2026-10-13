using Capstone.Core.Models.Interfaces;

namespace Capstone.Core.Models;

public class DLC : BaseTrackableGame
{
    private Game partOf { get; set; } = new Game();
}