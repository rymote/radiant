using System;
using System.Data;

namespace Rymote.Radiant.Smart.Configuration;

public interface ISmartModelConfiguration
{
    ISmartModelConfiguration UseConnection(IDbConnection connection);
    ISmartModelConfiguration RegisterModel<TModel>() where TModel : class;
    ISmartModelConfiguration RegisterModel(Type modelType);
    void Build();
}