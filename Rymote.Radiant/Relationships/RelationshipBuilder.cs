using System.Reflection;

namespace Rymote.Radiant.Relationships;

public static class RelationshipExtensions
{
    public static async Task<T> WithAsync<T, TRelated>(this Task<T> modelTask, Func<T, Task<TRelated>> relationshipLoader) 
        where T : class 
        where TRelated : class
    {
        T model = await modelTask;
        if (model == null)
            throw new ArgumentNullException(nameof(modelTask), "Model cannot be null");

        TRelated related = await relationshipLoader(model);
        
        PropertyInfo? property = typeof(T).GetProperties()
            .FirstOrDefault(p => p.PropertyType == typeof(TRelated) || 
                               (p.PropertyType.IsGenericType && 
                                p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && 
                                p.PropertyType.GetGenericArguments()[0] == typeof(TRelated)));
        
        if (property != null)
        {
            property.SetValue(model, related);
        }
        
        return model;
    }

    public static async Task<IEnumerable<T>> WithAsync<T, TRelated>(this Task<IEnumerable<T>> modelsTask, Func<IEnumerable<T>, Task<Dictionary<object, TRelated>>> relationshipLoader) 
        where T : class 
        where TRelated : class
    {
        IEnumerable<T> models = await modelsTask;
        if (!models.Any())
            return models;

        Dictionary<object, TRelated> relatedMap = await relationshipLoader(models);
        
        foreach (T model in models)
        {
            object? foreignKey = GetForeignKeyValue<T, TRelated>(model);
            if (foreignKey != null && relatedMap.TryGetValue(foreignKey, out TRelated? related))
            {
                PropertyInfo? property = typeof(T).GetProperties()
                    .FirstOrDefault(p => p.PropertyType == typeof(TRelated) || 
                                       (p.PropertyType.IsGenericType && 
                                        p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && 
                                        p.PropertyType.GetGenericArguments()[0] == typeof(TRelated)));
                
                if (property != null && related != null)
                {
                    property.SetValue(model, related);
                }
            }
        }
        
        return models;
    }

    private static object? GetForeignKeyValue<T, TRelated>(T model)
        where T : class
        where TRelated : class
    {
        if (model == null)
            return null;

        string relatedTypeName = typeof(TRelated).Name;
        string foreignKeyName = $"{relatedTypeName}Id";
        
        PropertyInfo? foreignKeyProperty = typeof(T).GetProperty(foreignKeyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        return foreignKeyProperty?.GetValue(model);
    }
}
