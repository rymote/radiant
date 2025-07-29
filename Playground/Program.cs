// See https://aka.ms/new-console-template for more information

using Rymote.Radiant.Sql;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Clauses.OrderBy;
using Rymote.Radiant.Sql.Expressions;

QueryCommand insertCommand = new InsertBuilder()
    .Into("orders", "sales")
    .Value("id", Guid.NewGuid())
    .Value("customer_id", Guid.Parse("18c8fa3f-7e65-473b-9d41-3b7cbbe43c48"))
    .Value("total_amount", 123.45m)
    .Build();

QueryCommand updateCommand = new UpdateBuilder()
    .Table("orders", "sales")
    .Set("status", "shipped")
    .Where("o.id", "=", Guid.Parse("d3b57932-4fd4-4e01-a8a7-019f7d4164e4"))
    .Build();

QueryCommand deleteCommand = new DeleteBuilder()
    .From("orders", "sales")
    .Where("o.status", "=", "cancelled")
    .Build();

QueryCommand selectCommand = new SelectBuilder()
    .Select(new RawSqlExpression("*"))
    .From("orders", "sales", "o")
    .Where("o.total_amount", ">", 100m)
    .OrderBy("o.created_at", SortDirection.Descending)
    .Limit(5)
    .Build();

PrintQueryCommand(insertCommand, "INSERT");
PrintQueryCommand(updateCommand, "UPDATE");
PrintQueryCommand(deleteCommand, "DELETE");
PrintQueryCommand(selectCommand, "SELECT");

static void PrintQueryCommand(QueryCommand queryCommand, string label)
{
    Console.WriteLine($"{label}:\n{queryCommand}");
    Console.WriteLine("Parameters:");
    foreach (string parameterName in queryCommand.Parameters.ParameterNames)
    {
        object? parameterValue = queryCommand.Parameters.Get<dynamic>(parameterName);
        Console.WriteLine($"  {parameterName} = {parameterValue}");
    }
    Console.WriteLine();
}