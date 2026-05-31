using System.Text.Json;
using Wokki.Application.Common;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.SwapPost;
using Wokki.Application.Mappings.SwapPosts;
using Wokki.Application.Notifications;
using Wokki.Application.Scheduling;
using Wokki.Application.Services.Organization.Implementations;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.SwapPost.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using SwapPostEntity = Wokki.Domain.Entities.SwapPost;
using ScheduleEntity = Wokki.Domain.Entities.Schedule;
using ShiftAssignmentEntity = Wokki.Domain.Entities.ShiftAssignment;
using ShiftDefinitionEntity = Wokki.Domain.Entities.ShiftDefinition;
using EmployeeEntity = Wokki.Domain.Entities.Employee;

namespace Wokki.Application.Services.SwapPost.Implementations;

public sealed class SwapPostService(
    IUnitOfWork unitOfWork,
    INotificationService notifications,
    IPasswordHasher passwordHasher,
    IOrganizationScopeService organizationScope,
    SwapPostPolicyValidator policyValidator) : ISwapPostService
{
    public async Task<ApiResponse<SwapPostResponse>> CreateAsync(
        CreateSwapPostRequest request,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var employeeResult = await RequireParticipantEmployeeAsync(userId, role, cancellationToken);
        if (employeeResult.Error is not null)
            return ApiResponse<SwapPostResponse>.FailureResponse(employeeResult.Error);

        var author = employeeResult.Employee!;
        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(request.AuthorAssignmentId, cancellationToken: cancellationToken);
        if (assignment is null || assignment.EmployeeId != author.Id)
            return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.AssignmentNotFound);

        var schedule = await unitOfWork.Schedules.GetByIdAsync(assignment.ScheduleId, cancellationToken: cancellationToken);
        if (schedule is null || !organizationScope.IsSameOrganization(schedule.OrganizationId))
            return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.ScheduleNotDraft);

        var scopeError = await ValidateEmployeeScheduleScopeAsync(author, schedule, cancellationToken);
        if (scopeError is not null)
            return ApiResponse<SwapPostResponse>.FailureResponse(scopeError);

        if (await unitOfWork.SwapPosts.HasPendingForAuthorAssignmentAsync(assignment.Id, cancellationToken: cancellationToken))
            return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.OpenExists);

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.Schedule.DepartmentNotFound);

        var entity = SwapPostMapper.ToEntity(request, author, assignment, schedule, department);
        await unitOfWork.SwapPosts.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<SwapPostResponse>.SuccessResponse(
            await MapResponseAsync(entity, author.Id, role, cancellationToken),
            AppMessages.SwapPost.Created);
    }

    public async Task<ApiResponse<PagedResponse<SwapPostResponse>>> ListFeedAsync(
        Guid scheduleId,
        Guid userId,
        string role,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var employeeResult = await RequireParticipantEmployeeAsync(userId, role, cancellationToken);
        if (employeeResult.Error is not null)
            return ApiResponse<PagedResponse<SwapPostResponse>>.FailureResponse(employeeResult.Error);

        var viewer = employeeResult.Employee!;
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null || !organizationScope.IsSameOrganization(schedule.OrganizationId))
            return ApiResponse<PagedResponse<SwapPostResponse>>.FailureResponse(AppMessages.Schedule.NotFound);

        var scopeError = await ValidateEmployeeScheduleScopeAsync(viewer, schedule, cancellationToken);
        if (scopeError is not null)
            return ApiResponse<PagedResponse<SwapPostResponse>>.FailureResponse(scopeError);

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return ApiResponse<PagedResponse<SwapPostResponse>>.FailureResponse(AppMessages.Schedule.DepartmentNotFound);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, total) = await unitOfWork.SwapPosts.ListFeedAsync(
            scheduleId,
            schedule.DepartmentId,
            department.LocationId,
            page,
            pageSize,
            cancellationToken);

        var responses = new List<SwapPostResponse>(items.Count);
        foreach (var post in items)
        {
            if (!await IsPostStillValidAsync(post, cancellationToken))
                continue;

            responses.Add(await MapResponseAsync(post, viewer.Id, role, cancellationToken));
        }

        return ApiResponse<PagedResponse<SwapPostResponse>>.SuccessPagedResponse(
            responses,
            page,
            pageSize,
            total,
            AppMessages.SwapPost.Listed);
    }

    public async Task<ApiResponse<PagedResponse<SwapPostResponse>>> ListMineAsync(
        Guid userId,
        Guid? scheduleId,
        SwapPostStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var employeeResult = await RequireParticipantEmployeeAsync(userId, RoleConstants.User, cancellationToken);
        if (employeeResult.Error is not null)
            return ApiResponse<PagedResponse<SwapPostResponse>>.FailureResponse(employeeResult.Error);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, total) = await unitOfWork.SwapPosts.ListByEmployeeAsync(
            employeeResult.Employee!.Id,
            scheduleId,
            status,
            page,
            pageSize,
            cancellationToken);

        var responses = new List<SwapPostResponse>(items.Count);
        foreach (var post in items)
            responses.Add(await MapResponseAsync(post, employeeResult.Employee!.Id, RoleConstants.User, cancellationToken));

        return ApiResponse<PagedResponse<SwapPostResponse>>.SuccessPagedResponse(
            responses,
            page,
            pageSize,
            total,
            AppMessages.SwapPost.Listed);
    }

    public async Task<ApiResponse<SwapPostResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var post = await unitOfWork.SwapPosts.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (post is null || !organizationScope.IsSameOrganization(post.OrganizationId))
            return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.NotFound);

        if (!await CanViewPostAsync(post, userId, role, cancellationToken))
            return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.Forbidden);

        Guid? viewerEmployeeId = null;
        if (role == RoleConstants.User)
        {
            var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
            viewerEmployeeId = employee?.Id;
        }

        return ApiResponse<SwapPostResponse>.SuccessResponse(
            await MapResponseAsync(post, viewerEmployeeId, role, cancellationToken),
            AppMessages.SwapPost.Found);
    }

    public async Task<ApiResponse<SwapPostResponse>> AcceptAsync(
        Guid id,
        AcceptSwapPostRequest request,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default) =>
        await ExecuteAcceptInternalAsync(id, request, userId, role, commit: true, cancellationToken);

    public async Task<ApiResponse<SwapPostAcceptPreviewResponse>> PreviewAcceptAsync(
        Guid id,
        AcceptSwapPostRequest request,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAcceptInternalAsync(id, request, userId, role, commit: false, cancellationToken);
        if (!result.Success)
        {
            return ApiResponse<SwapPostAcceptPreviewResponse>.SuccessResponse(
                new SwapPostAcceptPreviewResponse(false, result.Message.Code, result.Message.Text),
                AppMessages.SwapPost.PreviewValid);
        }

        return ApiResponse<SwapPostAcceptPreviewResponse>.SuccessResponse(
            new SwapPostAcceptPreviewResponse(true, null, null),
            AppMessages.SwapPost.PreviewValid);
    }

    public async Task<ApiResponse<SwapPostResponse>> CancelAsync(
        Guid id,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var employeeResult = await RequireParticipantEmployeeAsync(userId, role, cancellationToken);
        if (employeeResult.Error is not null)
            return ApiResponse<SwapPostResponse>.FailureResponse(employeeResult.Error);

        var post = await unitOfWork.SwapPosts.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (post is null || !organizationScope.IsSameOrganization(post.OrganizationId))
            return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.NotFound);

        if (post.AuthorEmployeeId != employeeResult.Employee!.Id)
            return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.Forbidden);

        if (post.Status != SwapPostStatus.Pending)
            return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.AlreadyTaken);

        var schedule = await unitOfWork.Schedules.GetByIdAsync(post.ScheduleId, cancellationToken: cancellationToken);
        if (schedule is null || schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.ScheduleNotDraft);

        post.Status = SwapPostStatus.Cancelled;
        post.UpdatedAt = DateTime.UtcNow;
        unitOfWork.SwapPosts.Update(post);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<SwapPostResponse>.SuccessResponse(
            await MapResponseAsync(post, employeeResult.Employee!.Id, role, cancellationToken),
            AppMessages.SwapPost.Cancelled);
    }

    public async Task<ApiResponse<PagedResponse<SwapPostAuditResponse>>> ListAuditAsync(
        Guid? scheduleId,
        Guid? locationId,
        Guid? departmentId,
        DateOnly? weekStartDate,
        Guid userId,
        string role,
        IReadOnlySet<Guid>? managedLocationIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (role is not (RoleConstants.Admin or RoleConstants.Manager))
            return ApiResponse<PagedResponse<SwapPostAuditResponse>>.FailureResponse(AppMessages.Auth.Forbidden);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var locationFilter = ResolveModerationLocationFilter(role, locationId, managedLocationIds);
        if (locationFilter is { Count: 0 })
            return ApiResponse<PagedResponse<SwapPostAuditResponse>>.FailureResponse(AppMessages.Auth.Forbidden);

        var organizationId = organizationScope.RequireOrganizationId();
        var (items, total) = await unitOfWork.SwapPosts.ListAuditAsync(
            organizationId,
            locationFilter,
            scheduleId,
            departmentId,
            weekStartDate,
            page,
            pageSize,
            cancellationToken);

        var responses = new List<SwapPostAuditResponse>(items.Count);
        foreach (var post in items)
            responses.Add(await MapAuditResponseAsync(post, cancellationToken));

        return ApiResponse<PagedResponse<SwapPostAuditResponse>>.SuccessPagedResponse(
            responses,
            page,
            pageSize,
            total,
            AppMessages.SwapPost.AuditListed);
    }

    public async Task<ApiResponse<PagedResponse<SwapPostResponse>>> ListAdminFeedAsync(
        Guid? locationId,
        Guid? departmentId,
        DateOnly? weekStartDate,
        Guid userId,
        string role,
        IReadOnlySet<Guid>? managedLocationIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (role is not (RoleConstants.Admin or RoleConstants.Manager))
            return ApiResponse<PagedResponse<SwapPostResponse>>.FailureResponse(AppMessages.Auth.Forbidden);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var locationFilter = ResolveModerationLocationFilter(role, locationId, managedLocationIds);
        if (locationFilter is { Count: 0 })
            return ApiResponse<PagedResponse<SwapPostResponse>>.FailureResponse(AppMessages.Auth.Forbidden);

        var organizationId = organizationScope.RequireOrganizationId();
        var (items, total) = await unitOfWork.SwapPosts.ListAdminFeedAsync(
            organizationId,
            locationFilter,
            departmentId,
            weekStartDate,
            page,
            pageSize,
            cancellationToken);

        var responses = new List<SwapPostResponse>(items.Count);
        foreach (var post in items)
        {
            if (!await IsPostStillValidAsync(post, cancellationToken))
                continue;

            responses.Add(await MapResponseAsync(post, null, role, cancellationToken, includeDepartment: true));
        }

        return ApiResponse<PagedResponse<SwapPostResponse>>.SuccessPagedResponse(
            responses,
            page,
            pageSize,
            total,
            AppMessages.SwapPost.Listed);
    }

    private static HashSet<Guid>? ResolveModerationLocationFilter(
        string role,
        Guid? locationId,
        IReadOnlySet<Guid>? managedLocationIds)
    {
        if (role == RoleConstants.Manager)
            return managedLocationIds is null ? [] : [.. managedLocationIds];

        return locationId.HasValue ? [locationId.Value] : null;
    }

    private async Task<ApiResponse<SwapPostResponse>> ExecuteAcceptInternalAsync(
        Guid id,
        AcceptSwapPostRequest request,
        Guid userId,
        string role,
        bool commit,
        CancellationToken cancellationToken)
    {
        var accepterResult = await RequireParticipantEmployeeAsync(userId, role, cancellationToken);
        if (accepterResult.Error is not null)
            return ApiResponse<SwapPostResponse>.FailureResponse(accepterResult.Error);

        var accepter = accepterResult.Employee!;
        SwapPostEntity? post = null;
        ScheduleEntity? schedule = null;
        ShiftAssignmentEntity? authorAssignment = null;
        ShiftAssignmentEntity? acceptorAssignment = null;
        ShiftDefinitionEntity? offeredShift = null;
        ShiftDefinitionEntity? acceptorShift = null;
        EmployeeEntity? author = null;
        OrganizationSchedulingSolverPolicy solverPolicy = null!;
        IReadOnlyList<ShiftAssignmentEntity> scheduleAssignments = [];
        Dictionary<Guid, ShiftDefinitionEntity> shiftsById = [];

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            post = await unitOfWork.SwapPosts.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (post is null || !organizationScope.IsSameOrganization(post.OrganizationId))
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.NotFound);
            }

            if (post.AuthorEmployeeId == accepter.Id)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.SelfAccept);
            }

            schedule = await unitOfWork.Schedules.GetByIdForUpdateAsync(post.ScheduleId, cancellationToken);
            if (schedule is null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.Schedule.NotFound);
            }

            if (schedule.Status != ScheduleStatus.Draft)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.ScheduleNotDraft);
            }

            post = await unitOfWork.SwapPosts.GetByIdForUpdateAsync(id, cancellationToken);
            if (post is null || post.Status != SwapPostStatus.Pending)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.AlreadyTaken);
            }

            var scopeError = await ValidateEmployeeScheduleScopeAsync(accepter, schedule, cancellationToken);
            if (scopeError is not null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<SwapPostResponse>.FailureResponse(scopeError);
            }

            authorAssignment = await unitOfWork.ShiftAssignments.GetByIdAsync(
                post.AuthorAssignmentId,
                track: commit,
                cancellationToken: cancellationToken);
            if (authorAssignment is null || authorAssignment.EmployeeId != post.AuthorEmployeeId)
            {
                post.Status = SwapPostStatus.Expired;
                post.UpdatedAt = DateTime.UtcNow;
                unitOfWork.SwapPosts.Update(post);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.StaleAssignment);
            }

            author = await unitOfWork.Employees.GetByIdAsync(post.AuthorEmployeeId, cancellationToken: cancellationToken);
            if (author is null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.StaleAssignment);
            }

            offeredShift = await unitOfWork.ShiftDefinitions.GetByIdAsync(
                authorAssignment.ShiftDefinitionId,
                cancellationToken: cancellationToken);
            if (offeredShift is null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.AssignmentNotFound);
            }

            scheduleAssignments = await unitOfWork.ShiftAssignments.ListByScheduleAsync(schedule.Id, cancellationToken);
            shiftsById = await LoadShiftsByIdAsync(scheduleAssignments, cancellationToken);

            var orgPolicy = await unitOfWork.OrganizationSchedulingPolicies.GetByOrganizationIdAsync(
                post.OrganizationId,
                cancellationToken: cancellationToken);
            solverPolicy = OrganizationSchedulingSolverPolicy.FromOrgPolicy(orgPolicy);

            if (post.Type == SwapPostType.Cover)
            {
                if (request.AcceptorAssignmentId.HasValue)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.CoverAcceptorAssignmentNotAllowed);
                }

                if (await unitOfWork.ShiftAssignments.ExistsAsync(
                        schedule.Id,
                        authorAssignment.ShiftDefinitionId,
                        accepter.Id,
                        authorAssignment.Date,
                        cancellationToken))
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.CoverNoOpenSlot);
                }

                var afterCover = CloneAssignment(authorAssignment);
                afterCover.EmployeeId = accepter.Id;
                var coverError = await policyValidator.ValidatePostSwapStateAsync(
                    accepter,
                    afterCover,
                    offeredShift,
                    schedule,
                    scheduleAssignments,
                    shiftsById,
                    new HashSet<Guid> { authorAssignment.Id },
                    solverPolicy,
                    cancellationToken);
                if (coverError is not null)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<SwapPostResponse>.FailureResponse(MapCoverAcceptError(coverError));
                }

                if (!commit)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<SwapPostResponse>.SuccessResponse(
                        await MapResponseAsync(post, accepter.Id, role, cancellationToken),
                        AppMessages.SwapPost.PreviewValid);
                }

                authorAssignment!.EmployeeId = accepter.Id;
                unitOfWork.ShiftAssignments.Update(authorAssignment);
            }
            else
            {
                if (!request.AcceptorAssignmentId.HasValue)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.AcceptorAssignmentRequired);
                }

                acceptorAssignment = await unitOfWork.ShiftAssignments.GetByIdAsync(
                    request.AcceptorAssignmentId.Value,
                    track: commit,
                    cancellationToken: cancellationToken);
                if (acceptorAssignment is null
                    || acceptorAssignment.EmployeeId != accepter.Id
                    || acceptorAssignment.ScheduleId != schedule.Id)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.AssignmentNotFound);
                }

                if (await unitOfWork.SwapPosts.HasPendingForAuthorAssignmentAsync(
                        acceptorAssignment.Id,
                        excludePostId: post.Id,
                        cancellationToken: cancellationToken))
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.AcceptorAssignmentHasOpenPost);
                }

                acceptorShift = await unitOfWork.ShiftDefinitions.GetByIdAsync(
                    acceptorAssignment.ShiftDefinitionId,
                    cancellationToken: cancellationToken);
                if (acceptorShift is null)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.SwapPost.AssignmentNotFound);
                }

                var accepterReceiving = CloneAssignment(authorAssignment);
                accepterReceiving.EmployeeId = accepter.Id;
                var authorReceiving = CloneAssignment(acceptorAssignment);
                authorReceiving.EmployeeId = author.Id;

                var authorError = await policyValidator.ValidatePostSwapStateAsync(
                    author,
                    authorReceiving,
                    acceptorShift,
                    schedule,
                    scheduleAssignments,
                    shiftsById,
                    new HashSet<Guid> { authorAssignment.Id, acceptorAssignment.Id },
                    solverPolicy,
                    cancellationToken);
                if (authorError is not null)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<SwapPostResponse>.FailureResponse(authorError);
                }

                var accepterError = await policyValidator.ValidatePostSwapStateAsync(
                    accepter,
                    accepterReceiving,
                    offeredShift,
                    schedule,
                    scheduleAssignments,
                    shiftsById,
                    new HashSet<Guid> { authorAssignment.Id, acceptorAssignment.Id },
                    solverPolicy,
                    cancellationToken);
                if (accepterError is not null)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<SwapPostResponse>.FailureResponse(accepterError);
                }

                if (!commit)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<SwapPostResponse>.SuccessResponse(
                        await MapResponseAsync(post, accepter.Id, role, cancellationToken),
                        AppMessages.SwapPost.PreviewValid);
                }

                var holdEmployeeId = await SwapHoldProvisioner.EnsureForOrganizationAsync(
                    unitOfWork,
                    passwordHasher,
                    post.OrganizationId,
                    schedule.DepartmentId,
                    cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                await unitOfWork.ShiftAssignments.SwapEmployeeIdsAsync(
                    authorAssignment.Id,
                    acceptorAssignment.Id,
                    holdEmployeeId,
                    cancellationToken);
            }

            if (!commit)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<SwapPostResponse>.SuccessResponse(
                    await MapResponseAsync(post, accepter.Id, role, cancellationToken),
                    AppMessages.SwapPost.PreviewValid);
            }

            post.AcceptedByEmployeeId = accepter.Id;
            post.AcceptorAssignmentId = acceptorAssignment?.Id;
            post.Status = SwapPostStatus.Completed;
            post.CompletedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;
            unitOfWork.SwapPosts.Update(post);

            await WriteAuditLogAsync(post, author!, accepter, authorAssignment!, acceptorAssignment, offeredShift!, acceptorShift, userId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await NotifyCompletedSafeAsync(post!, author!, accepter, authorAssignment!, acceptorAssignment, offeredShift!, acceptorShift, schedule!, cancellationToken);

        return ApiResponse<SwapPostResponse>.SuccessResponse(
            await MapResponseAsync(post!, accepter.Id, role, cancellationToken),
            AppMessages.SwapPost.Accepted);
    }

    private async Task WriteAuditLogAsync(
        SwapPostEntity post,
        EmployeeEntity author,
        EmployeeEntity accepter,
        ShiftAssignmentEntity authorAssignment,
        ShiftAssignmentEntity? acceptorAssignment,
        ShiftDefinitionEntity offeredShift,
        ShiftDefinitionEntity? acceptorShift,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var beforeJson = JsonSerializer.Serialize(new
        {
            authorEmployeeId = author.Id,
            accepterEmployeeId = (Guid?)null,
            authorAssignmentId = authorAssignment.Id,
            acceptorAssignmentId = (Guid?)null
        });
        var afterJson = JsonSerializer.Serialize(new
        {
            authorEmployeeId = author.Id,
            accepterEmployeeId = accepter.Id,
            authorAssignmentId = authorAssignment.Id,
            acceptorAssignmentId = acceptorAssignment?.Id,
            type = post.Type.ToString(),
            offered = new
            {
                date = authorAssignment.Date,
                shiftDefinitionId = offeredShift.Id,
                shiftName = offeredShift.Name,
                startTime = offeredShift.StartTime,
                endTime = offeredShift.EndTime,
                employeeIdBefore = author.Id,
                employeeIdAfter = post.Type == SwapPostType.Cover ? accepter.Id : acceptorAssignment?.EmployeeId
            },
            accepted = acceptorAssignment is null || acceptorShift is null
                ? null
                : new
                {
                    date = acceptorAssignment.Date,
                    shiftDefinitionId = acceptorShift.Id,
                    shiftName = acceptorShift.Name,
                    startTime = acceptorShift.StartTime,
                    endTime = acceptorShift.EndTime,
                    employeeIdBefore = accepter.Id,
                    employeeIdAfter = author.Id
                }
        });

        await unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = post.OrganizationId,
            ActorUserId = actorUserId,
            Action = "swap_post.completed",
            EntityType = nameof(SwapPostEntity),
            EntityId = post.Id,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task NotifyCompletedSafeAsync(
        SwapPostEntity post,
        EmployeeEntity author,
        EmployeeEntity accepter,
        ShiftAssignmentEntity authorAssignment,
        ShiftAssignmentEntity? acceptorAssignment,
        ShiftDefinitionEntity offeredShift,
        ShiftDefinitionEntity? acceptorShift,
        ScheduleEntity schedule,
        CancellationToken cancellationToken)
    {
        try
        {
            var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
            var location = department is null
                ? null
                : await unitOfWork.Locations.GetByIdAsync(department.LocationId, cancellationToken: cancellationToken);

            var payload = new SwapPostCompletedNotificationPayload(
                post.Type,
                schedule.WeekStartDate,
                location?.Name,
                department?.Name,
                BuildShiftLine(authorAssignment, offeredShift),
                acceptorAssignment is not null && acceptorShift is not null
                    ? BuildShiftLine(acceptorAssignment, acceptorShift)
                    : null);

            await NotifyEmployeeSafeAsync(author.Id, payload with { RecipientFirstName = author.FirstName }, cancellationToken);
            await NotifyEmployeeSafeAsync(accepter.Id, payload with { RecipientFirstName = accepter.FirstName }, cancellationToken);
        }
        catch
        {
            // BR-037: notifications must not roll back core transaction
        }
    }

    private async Task NotifyEmployeeSafeAsync(
        Guid employeeId,
        SwapPostCompletedNotificationPayload payload,
        CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken: cancellationToken);
        if (employee is null)
            return;

        await notifications.SendAsync(employee.UserId, "swap_post.completed", payload, cancellationToken);
    }

    private static SwapPostCompletedShiftLine BuildShiftLine(
        ShiftAssignmentEntity assignment,
        ShiftDefinitionEntity shift) =>
        new(assignment.Date, shift.Name, shift.StartTime, shift.EndTime);

    private async Task<bool> IsPostStillValidAsync(SwapPostEntity post, CancellationToken cancellationToken)
    {
        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(
            post.AuthorAssignmentId,
            cancellationToken: cancellationToken);
        if (assignment is null || assignment.EmployeeId != post.AuthorEmployeeId)
        {
            await MarkExpiredAsync(post, commit: true, cancellationToken);
            return false;
        }

        return true;
    }

    private async Task MarkExpiredAsync(SwapPostEntity post, bool commit, CancellationToken cancellationToken)
    {
        if (!commit)
            return;

        var tracked = await unitOfWork.SwapPosts.GetByIdAsync(post.Id, track: true, cancellationToken: cancellationToken);
        if (tracked is null || tracked.Status != SwapPostStatus.Pending)
            return;

        tracked.Status = SwapPostStatus.Expired;
        tracked.UpdatedAt = DateTime.UtcNow;
        unitOfWork.SwapPosts.Update(tracked);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<(EmployeeEntity? Employee, AppMessage? Error)> RequireParticipantEmployeeAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        if (role is RoleConstants.Admin or RoleConstants.Manager)
            return (null, AppMessages.SwapPost.ManagerNotAllowed);

        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null || employee.TerminatedAt is not null)
            return (null, AppMessages.SwapPost.NoEmployeeProfile);

        return (employee, null);
    }

    private async Task<AppMessage?> ValidateEmployeeScheduleScopeAsync(
        EmployeeEntity employee,
        ScheduleEntity schedule,
        CancellationToken cancellationToken)
    {
        if (!organizationScope.IsSameOrganization(employee.OrganizationId))
            return AppMessages.SwapPost.ScopeMismatch;

        if (!await unitOfWork.Employees.IsMemberOfDepartmentAsync(employee.Id, schedule.DepartmentId, cancellationToken))
            return AppMessages.SwapPost.ScopeMismatch;

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return AppMessages.Schedule.DepartmentNotFound;

        var membership = await unitOfWork.LocationMemberships.GetActiveByEmployeeAsync(employee.Id, cancellationToken: cancellationToken);
        if (membership?.LocationId != department.LocationId)
            return AppMessages.SwapPost.ScopeMismatch;

        return null;
    }

    private async Task<bool> CanViewPostAsync(
        SwapPostEntity post,
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        if (role is RoleConstants.Admin or RoleConstants.Manager)
            return true;

        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return false;

        if (post.AuthorEmployeeId == employee.Id || post.AcceptedByEmployeeId == employee.Id)
            return true;

        if (post.Status != SwapPostStatus.Pending)
            return false;

        var schedule = await unitOfWork.Schedules.GetByIdAsync(post.ScheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return false;

        return await ValidateEmployeeScheduleScopeAsync(employee, schedule, cancellationToken) is null;
    }

    private async Task<SwapPostResponse> MapResponseAsync(
        SwapPostEntity post,
        Guid? viewerEmployeeId,
        string role,
        CancellationToken cancellationToken,
        bool includeDepartment = false)
    {
        var author = await unitOfWork.Employees.GetByIdAsync(post.AuthorEmployeeId, cancellationToken: cancellationToken);
        var authorAssignment = await unitOfWork.ShiftAssignments.GetByIdAsync(post.AuthorAssignmentId, cancellationToken: cancellationToken);
        ShiftDefinitionEntity? offeredShift = null;
        if (authorAssignment is not null)
            offeredShift = await unitOfWork.ShiftDefinitions.GetByIdAsync(authorAssignment.ShiftDefinitionId, cancellationToken: cancellationToken);

        SwapPostShiftDto? acceptedShift = null;
        SwapPostAuthorDto? acceptedBy = null;
        if (post.AcceptedByEmployeeId is Guid acceptedEmployeeId)
        {
            var acceptedEmployee = await unitOfWork.Employees.GetByIdAsync(acceptedEmployeeId, cancellationToken: cancellationToken);
            if (acceptedEmployee is not null)
                acceptedBy = new SwapPostAuthorDto(acceptedEmployee.Id, FormatDisplayName(acceptedEmployee));

            if (post.AcceptorAssignmentId is Guid acceptorAssignmentId)
            {
                var acceptorAssignment = await unitOfWork.ShiftAssignments.GetByIdAsync(acceptorAssignmentId, cancellationToken: cancellationToken);
                if (acceptorAssignment is not null)
                {
                    var acceptorShiftDef = await unitOfWork.ShiftDefinitions.GetByIdAsync(
                        acceptorAssignment.ShiftDefinitionId,
                        cancellationToken: cancellationToken);
                    if (acceptorShiftDef is not null)
                        acceptedShift = MapShiftDto(acceptorAssignment, acceptorShiftDef);
                }
            }
        }

        var isMine = viewerEmployeeId == post.AuthorEmployeeId;
        var canCancel = isMine && post.Status == SwapPostStatus.Pending && role == RoleConstants.User;
        var canAccept = role == RoleConstants.User
                        && post.Status == SwapPostStatus.Pending
                        && viewerEmployeeId.HasValue
                        && viewerEmployeeId != post.AuthorEmployeeId;

        Guid? departmentId = null;
        string? departmentName = null;
        if (includeDepartment)
        {
            departmentId = post.DepartmentId;
            var department = await unitOfWork.Departments.GetByIdAsync(post.DepartmentId, cancellationToken: cancellationToken);
            departmentName = department?.Name;
        }

        return new SwapPostResponse(
            post.Id,
            post.ScheduleId,
            post.Type,
            post.Status,
            new SwapPostAuthorDto(post.AuthorEmployeeId, FormatDisplayName(author)),
            authorAssignment is not null && offeredShift is not null
                ? MapShiftDto(authorAssignment, offeredShift)
                : new SwapPostShiftDto(post.AuthorAssignmentId, default, Guid.Empty, string.Empty, default, default),
            acceptedShift,
            acceptedBy,
            post.Note,
            post.CreatedAt,
            post.CompletedAt,
            canAccept,
            canCancel,
            isMine,
            departmentId,
            departmentName);
    }

    private async Task<SwapPostAuditResponse> MapAuditResponseAsync(
        SwapPostEntity post,
        CancellationToken cancellationToken)
    {
        var author = await unitOfWork.Employees.GetByIdAsync(post.AuthorEmployeeId, cancellationToken: cancellationToken);
        EmployeeEntity? accepter = null;
        if (post.AcceptedByEmployeeId is Guid acceptedId)
            accepter = await unitOfWork.Employees.GetByIdAsync(acceptedId, cancellationToken: cancellationToken);

        var authorAssignment = await unitOfWork.ShiftAssignments.GetByIdAsync(post.AuthorAssignmentId, cancellationToken: cancellationToken);
        ShiftDefinitionEntity? offeredShift = null;
        if (authorAssignment is not null)
            offeredShift = await unitOfWork.ShiftDefinitions.GetByIdAsync(authorAssignment.ShiftDefinitionId, cancellationToken: cancellationToken);

        SwapPostShiftDto? acceptedShift = null;
        if (post.AcceptorAssignmentId is Guid acceptorAssignmentId)
        {
            var acceptorAssignment = await unitOfWork.ShiftAssignments.GetByIdAsync(acceptorAssignmentId, cancellationToken: cancellationToken);
            if (acceptorAssignment is not null)
            {
                var acceptorShiftDef = await unitOfWork.ShiftDefinitions.GetByIdAsync(
                    acceptorAssignment.ShiftDefinitionId,
                    cancellationToken: cancellationToken);
                if (acceptorShiftDef is not null)
                    acceptedShift = MapShiftDto(acceptorAssignment, acceptorShiftDef);
            }
        }

        var department = await unitOfWork.Departments.GetByIdAsync(post.DepartmentId, cancellationToken: cancellationToken);

        return new SwapPostAuditResponse(
            post.Id,
            post.Type,
            post.CompletedAt ?? post.UpdatedAt,
            new SwapPostAuthorDto(post.AuthorEmployeeId, FormatDisplayName(author)),
            accepter is null ? null : new SwapPostAuthorDto(accepter.Id, FormatDisplayName(accepter)),
            authorAssignment is not null && offeredShift is not null
                ? MapShiftDto(authorAssignment, offeredShift)
                : new SwapPostShiftDto(post.AuthorAssignmentId, default, Guid.Empty, string.Empty, default, default),
            acceptedShift,
            post.ScheduleId,
            post.LocationId,
            post.DepartmentId,
            department?.Name);
    }

    private static SwapPostShiftDto MapShiftDto(ShiftAssignmentEntity assignment, ShiftDefinitionEntity shift) =>
        new(assignment.Id, assignment.Date, shift.Id, shift.Name, shift.StartTime, shift.EndTime);

    private static string FormatDisplayName(EmployeeEntity? employee) =>
        employee is null
            ? "Unknown"
            : $"{employee.FirstName} {employee.LastName}".Trim();

    private static ShiftAssignmentEntity CloneAssignment(ShiftAssignmentEntity source) =>
        new()
        {
            Id = source.Id,
            OrganizationId = source.OrganizationId,
            ScheduleId = source.ScheduleId,
            ShiftDefinitionId = source.ShiftDefinitionId,
            EmployeeId = source.EmployeeId,
            Date = source.Date,
            Note = source.Note,
            CreatedAt = source.CreatedAt
        };

    private async Task<Dictionary<Guid, ShiftDefinitionEntity>> LoadShiftsByIdAsync(
        IReadOnlyList<ShiftAssignmentEntity> assignments,
        CancellationToken cancellationToken)
    {
        var shiftIds = assignments.Select(a => a.ShiftDefinitionId).Distinct().ToList();
        var result = new Dictionary<Guid, ShiftDefinitionEntity>();
        foreach (var shiftId in shiftIds)
        {
            var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(shiftId, cancellationToken: cancellationToken);
            if (shift is not null)
                result[shiftId] = shift;
        }

        return result;
    }

    private static AppMessage MapCoverAcceptError(AppMessage error) =>
        error == AppMessages.SwapPost.PolicyOverlap
        || error == AppMessages.SwapPost.PolicyRestConflict
        || error == AppMessages.SwapPost.PolicyDailyCap
        || error == AppMessages.SwapPost.PolicyWeeklyCap
            ? AppMessages.SwapPost.CoverNoOpenSlot
            : error;
}
