using Dapper;

namespace Rymote.Radiant.Sql;

public sealed class QueryCommand
{
    public string SqlText { get; }
    public DynamicParameters Parameters { get; }

    public QueryCommand(string sqlText, DynamicParameters parameters)
    {
        SqlText = sqlText;
        Parameters = parameters;
    }

    public override string ToString()
    {
        return SqlText;
    }
    
    public static implicit operator string(QueryCommand queryCommand)
    {
        return queryCommand.ToString();
    }
}
