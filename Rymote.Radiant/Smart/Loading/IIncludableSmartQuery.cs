using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Rymote.Radiant.Smart.Query;

namespace Rymote.Radiant.Smart.Loading;

public interface IIncludableSmartQuery<TRoot, TCurrent> : ISmartQuery<TRoot>
    where TRoot : class, new()
{
    IIncludableSmartQuery<TRoot, TNext> ThenInclude<TNext>(Expression<Func<TCurrent, TNext?>> navigation)
        where TNext : class;

    IIncludableSmartQuery<TRoot, TNext> ThenInclude<TNext>(Expression<Func<TCurrent, IEnumerable<TNext>>> navigation)
        where TNext : class;
}
