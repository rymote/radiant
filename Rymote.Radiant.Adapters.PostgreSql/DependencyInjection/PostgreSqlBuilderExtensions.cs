using System;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Rymote.Radiant.Smart.Configuration;

namespace Rymote.Radiant.Adapters.PostgreSql.DependencyInjection;

public static class PostgreSqlBuilderExtensions
{
    public static RadiantBuilder UsePostgreSql(
        this RadiantBuilder builder,
        string connectionString,
        Action<NpgsqlDataSourceBuilder>? configureDataSource = null)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required", nameof(connectionString));

        builder.Services.AddSingleton<NpgsqlDataSource>(_ =>
        {
            NpgsqlDataSourceBuilder dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            configureDataSource?.Invoke(dataSourceBuilder);
            return dataSourceBuilder.Build();
        });

        return builder.UseAdapter(serviceProvider =>
        {
            NpgsqlDataSource dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            return new PostgreSqlAdapter(dataSource);
        });
    }

    public static RadiantBuilder UsePostgreSql(this RadiantBuilder builder, NpgsqlDataSource dataSource)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (dataSource is null) throw new ArgumentNullException(nameof(dataSource));

        builder.Services.AddSingleton(dataSource);
        return builder.UseAdapter(_ => new PostgreSqlAdapter(dataSource));
    }
}
