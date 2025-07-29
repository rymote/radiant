namespace Rymote.Radiant.Sql.Exceptions;

public sealed class InvalidQueryParameterException : QueryBuilderException
{
    public string ParameterName { get; }
    public object? ParameterValue { get; }

    public InvalidQueryParameterException(string parameterName, object? parameterValue, string reason)
        : base($"Invalid parameter '{parameterName}': {reason}. Provided value: '{parameterValue}'")
    {
        ParameterName = parameterName;
        ParameterValue = parameterValue;
    }

    public InvalidQueryParameterException(string parameterName, string reason)
        : base($"Invalid parameter '{parameterName}': {reason}")
    {
        ParameterName = parameterName;
    }
}