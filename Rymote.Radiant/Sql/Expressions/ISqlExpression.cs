using System.Text;

namespace Rymote.Radiant.Sql.Expressions;

public interface ISqlExpression
{
    void AppendTo(StringBuilder stringBuilder);
}