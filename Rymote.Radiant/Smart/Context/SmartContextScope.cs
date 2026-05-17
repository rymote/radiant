using System;

namespace Rymote.Radiant.Smart.Context;

public static class SmartContextScope
{
    public static IDisposable Begin(SmartContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        return SmartContextAmbient.Use(context);
    }
}
