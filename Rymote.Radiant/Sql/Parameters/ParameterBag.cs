using Dapper;

namespace Rymote.Radiant.Sql.Parameters;

public sealed class ParameterBag
{
    private readonly DynamicParameters dynamicParameters = new();
    private int parameterIndex;

    public string Add(object value)
    {
        string parameterName = "p" + parameterIndex++;
        dynamicParameters.Add(parameterName, value);
        return parameterName;
    }

    public DynamicParameters ToDynamicParameters() => dynamicParameters;
}
