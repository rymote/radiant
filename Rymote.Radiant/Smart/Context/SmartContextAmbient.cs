using System;
using System.Threading;

namespace Rymote.Radiant.Smart.Context;

public static class SmartContextAmbient
{
    private static readonly AsyncLocal<SmartContext?> currentContextSlot = new();

    public static SmartContext? CurrentOrNull => currentContextSlot.Value;

    public static SmartContext Current
        => currentContextSlot.Value
           ?? throw new InvalidOperationException(
               "No ambient SmartContext is set. Configure ASP.NET Core middleware via UseRadiantSmartContext() or wrap your call in SmartContextAmbient.Use(context).");

    public static IDisposable Use(SmartContext context)
    {
        SmartContext? previousContext = currentContextSlot.Value;
        currentContextSlot.Value = context;
        return new AmbientContextScope(previousContext);
    }

    private sealed class AmbientContextScope : IDisposable
    {
        private readonly SmartContext? previousContext;

        public AmbientContextScope(SmartContext? previousContext)
        {
            this.previousContext = previousContext;
        }

        public void Dispose() => currentContextSlot.Value = previousContext;
    }
}
