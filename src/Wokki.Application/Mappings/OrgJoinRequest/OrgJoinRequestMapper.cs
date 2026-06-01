using Wokki.Application.Dtos.OrgJoinRequest;
using OrgJoinRequestEntity = Wokki.Domain.Entities.OrgJoinRequest;

namespace Wokki.Application.Mappings.OrgJoinRequest;

public static class OrgJoinRequestMapper
{
    public static OrgJoinRequestResponse ToResponse(this OrgJoinRequestEntity request) =>
        new(
            request.Id,
            request.OrganizationId,
            request.Organization.Name,
            request.Status,
            request.SubmittedAt,
            request.ReviewedAt,
            request.RejectNote);

    public static PendingOrgJoinRequestResponse ToPendingResponse(this OrgJoinRequestEntity request) =>
        new(
            request.Id,
            request.UserId,
            request.User.Email,
            request.User.FirstName,
            request.User.LastName,
            request.User.Phone,
            request.SubmittedAt);
}
