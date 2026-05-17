using System;
using System.Data;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Smart.Configuration;

public interface ISmartModelConfiguration
{
    ISmartModelConfiguration UseAdapter(IDatabaseAdapter adapter);
    ISmartModelConfiguration UseConnection(IDbConnection connection);
    ISmartModelConfiguration RegisterModel<TModel>() where TModel : class;
    ISmartModelConfiguration RegisterModel(Type modelType);
    void Build();
}
