using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.LocationMembership;

public sealed record LocationMembershipReviewDto(LocationMembershipStatus Status, string? Note);
