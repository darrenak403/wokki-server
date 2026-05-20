namespace Wokki.Domain.Enums;

public enum SwapStatus
{
    Pending = 0,
    PeerAccepted = 1,
    PeerDeclined = 2,
    ManagerApproved = 3,
    ManagerRejected = 4,
    Cancelled = 5
}
