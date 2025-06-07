namespace Rymote.Radiant.Events;

public static class ModelEvents<T> where T : class
{
    public static event Func<T, Task>? Creating;
    public static event Func<T, Task>? Created;
    public static event Func<T, Task>? Updating;
    public static event Func<T, Task>? Updated;
    public static event Func<T, Task>? Deleting;
    public static event Func<T, Task>? Deleted;

    internal static async Task OnCreatingAsync(T model)
    {
        if (Creating != null)
            await Creating(model);
    }

    internal static async Task OnCreatedAsync(T model)
    {
        if (Created != null)
            await Created(model);
    }

    internal static async Task OnUpdatingAsync(T model)
    {
        if (Updating != null)
            await Updating(model);
    }

    internal static async Task OnUpdatedAsync(T model)
    {
        if (Updated != null)
            await Updated(model);
    }

    internal static async Task OnDeletingAsync(T model)
    {
        if (Deleting != null)
            await Deleting(model);
    }

    internal static async Task OnDeletedAsync(T model)
    {
        if (Deleted != null)
            await Deleted(model);
    }
}
