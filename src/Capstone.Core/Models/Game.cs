using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;

namespace Capstone.Core.Models;

/// <summary>A game owned and tracked in the library.</summary>
public class Game : BaseTrackableGame
{
    public OwnershipKind OwnershipKind { get; init; } = OwnershipKind.Digital;
    public ICollection<DLC> DLCs { get; init; } = new List<DLC>();
}