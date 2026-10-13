using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;

namespace Capstone.Core.Models;

public class Game : BaseTrackableGame
{
    private OwnershipKind  ownershipKind { get; set; }
    private ICollection<DLC>? DLCs { get; set; } = new List<DLC>();
}