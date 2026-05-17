namespace Rymote.Radiant.Smart.Auditing;

public interface ICurrentUserAccessor
{
    object? GetCurrentUserId();
}
