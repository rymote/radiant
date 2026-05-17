using System;
using Rymote.Radiant.Smart.Query;

namespace Rymote.Radiant.Smart.Configuration;

public abstract class GlobalQueryFilter
{
    public abstract Type MarkerInterface { get; }

    public abstract void Apply<TModel>(ISmartQuery<TModel> query, IServiceProvider serviceProvider)
        where TModel : class, new();
}

public sealed class DelegateGlobalQueryFilter : GlobalQueryFilter
{
    private readonly Action<object, IServiceProvider> applyDelegate;

    public DelegateGlobalQueryFilter(Type markerInterface, Action<object, IServiceProvider> applyDelegate)
    {
        MarkerInterface = markerInterface;
        this.applyDelegate = applyDelegate;
    }

    public override Type MarkerInterface { get; }

    public override void Apply<TModel>(ISmartQuery<TModel> query, IServiceProvider serviceProvider)
        => applyDelegate(query, serviceProvider);
}
