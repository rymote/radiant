using System;
using Rymote.Radiant.Smart.Configuration;

namespace Rymote.Radiant.Adapters.MySql.DependencyInjection;

public static class MySqlBuilderExtensions
{
    public static RadiantBuilder UseMySql(this RadiantBuilder builder, string connectionString)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required", nameof(connectionString));

        return builder.UseAdapter(_ => new MySqlAdapter(connectionString));
    }
}
