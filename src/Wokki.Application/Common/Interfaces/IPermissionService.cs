namespace Wokki.Application.Common.Interfaces;

public interface IPermissionService
{
    Task<bool> AuthorizeAsync(string resource, string action, Guid? resourceId = null, CancellationToken cancellationToken = default);
}
