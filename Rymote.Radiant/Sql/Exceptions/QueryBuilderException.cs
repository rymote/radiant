namespace Rymote.Radiant.Sql.Exceptions;

public abstract class QueryBuilderException : Exception
{
    protected QueryBuilderException(string message) : base(message) { }
    protected QueryBuilderException(string message, Exception innerException) : base(message, innerException) { }
}