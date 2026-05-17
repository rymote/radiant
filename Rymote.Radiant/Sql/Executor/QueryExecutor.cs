using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Rymote.Radiant.Smart.Mapping;

namespace Rymote.Radiant.Sql.Executor;

public sealed class QueryExecutor
{
    private readonly IDbConnection databaseConnection;
    private readonly IDbTransaction? activeTransaction;

    public QueryExecutor(IDbConnection databaseConnection, IDbTransaction? activeTransaction = null)
    {
        this.databaseConnection = databaseConnection;
        this.activeTransaction = activeTransaction;
    }

    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        if (TryGetSourceGeneratedMapper<TResult>(out Func<DbDataReader, TResult>? generatedMapper))
            return await ExecuteWithSourceGeneratedMapperAsync(queryCommand, generatedMapper!, cancellationToken);

        return await databaseConnection.QueryAsync<TResult>(BuildCommand(queryCommand, cancellationToken));
    }

    public async Task<TResult> QuerySingleAsync<TResult>(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        if (TryGetSourceGeneratedMapper<TResult>(out Func<DbDataReader, TResult>? generatedMapper))
        {
            List<TResult> rows = await ExecuteWithSourceGeneratedMapperAsync(queryCommand, generatedMapper!, cancellationToken);
            if (rows.Count == 0) throw new InvalidOperationException("Sequence contains no elements.");
            if (rows.Count > 1) throw new InvalidOperationException("Sequence contains more than one element.");
            return rows[0];
        }

        return await databaseConnection.QuerySingleAsync<TResult>(BuildCommand(queryCommand, cancellationToken));
    }

    public async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        if (TryGetSourceGeneratedMapper<TResult>(out Func<DbDataReader, TResult>? generatedMapper))
        {
            List<TResult> rows = await ExecuteWithSourceGeneratedMapperAsync(queryCommand, generatedMapper!, cancellationToken);
            if (rows.Count == 0) return default;
            if (rows.Count > 1) throw new InvalidOperationException("Sequence contains more than one element.");
            return rows[0];
        }

        return await databaseConnection.QuerySingleOrDefaultAsync<TResult?>(BuildCommand(queryCommand, cancellationToken));
    }

    public async Task<int> ExecuteAsync(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        return await databaseConnection.ExecuteAsync(BuildCommand(queryCommand, cancellationToken));
    }

    private CommandDefinition BuildCommand(QueryCommand queryCommand, CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            commandText: queryCommand.SqlText,
            parameters: queryCommand.Parameters,
            transaction: activeTransaction,
            cancellationToken: cancellationToken);
    }

    private static bool TryGetSourceGeneratedMapper<TResult>(out Func<DbDataReader, TResult>? mapper)
    {
        mapper = null;
        if (typeof(TResult).IsValueType || typeof(TResult) == typeof(string)) return false;
        if (!typeof(TResult).IsClass) return false;

        if (!SourceGeneratedMapperRegistry.TryGet(typeof(TResult), out Func<DbDataReader, object>? rawMapper))
            return false;

        mapper = reader => (TResult)rawMapper!(reader);
        return true;
    }

    private async Task<List<TResult>> ExecuteWithSourceGeneratedMapperAsync<TResult>(
        QueryCommand queryCommand,
        Func<DbDataReader, TResult> generatedMapper,
        CancellationToken cancellationToken)
    {
        if (databaseConnection is not DbConnection modernConnection)
            throw new InvalidOperationException(
                "Source-generated result mapping requires a DbConnection-derived connection. The configured IDbConnection does not derive from DbConnection.");

        await using DbCommand command = modernConnection.CreateCommand();
        command.CommandText = queryCommand.SqlText;
        if (activeTransaction is DbTransaction modernTransaction)
            command.Transaction = modernTransaction;

        foreach (Rymote.Radiant.Adapters.QueryParameter parameter in queryCommand.OrderedParameters)
        {
            DbParameter dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            if (parameter.Type.HasValue) dbParameter.DbType = parameter.Type.Value;
            command.Parameters.Add(dbParameter);
        }

        List<TResult> rows = new List<TResult>();
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(generatedMapper(reader));
        return rows;
    }
}
