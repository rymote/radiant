using System;
using Rymote.Radiant.Smart.Configuration;

namespace Rymote.Radiant.Adapters.SqlServer.DependencyInjection;

public static class SqlServerBuilderExtensions
{
    public static RadiantBuilder UseSqlServer(this RadiantBuilder builder, string connectionString)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required", nameof(connectionString));

        return builder.UseAdapter(_ => new SqlServerAdapter(connectionString));
    }
}
