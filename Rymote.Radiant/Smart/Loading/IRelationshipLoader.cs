using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Rymote.Radiant.Smart.Loading;

public interface IRelationshipLoader
{
    Task LoadRelationshipAsync<TModel>(List<TModel> models, Expression<Func<TModel, object>> navigationProperty) 
        where TModel : class, new();
    
    Task LoadRelationshipAsync<TModel>(List<TModel> models, string navigationPath) 
        where TModel : class, new();
} 