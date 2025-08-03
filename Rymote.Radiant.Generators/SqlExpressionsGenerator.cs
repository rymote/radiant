using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Rymote.Radiant.Generators;

[Generator]
public sealed class SqlExpressionsGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        string source = GenerateSqlExpressionHelpers();
        context.AddSource("SqlExpressionHelpers.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private string GenerateSqlExpressionHelpers()
    {
        StringBuilder sourceBuilder = new StringBuilder();

        sourceBuilder.AppendLine("#nullable enable");
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using Rymote.Radiant.Sql.Expressions;");
        sourceBuilder.AppendLine("using Rymote.Radiant.Sql.Builder;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("namespace Rymote.Radiant.Sql;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("public static class Sql");
        sourceBuilder.AppendLine("{");
        
        sourceBuilder.AppendLine("    public static class Column");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static ColumnExpression Named(string columnName, string? tableAlias = null) =>");
        sourceBuilder.AppendLine("            new ColumnExpression(columnName, tableAlias);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        sourceBuilder.AppendLine("    public static class Function");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static FunctionExpression Count(ISqlExpression? expression = null) =>");
        sourceBuilder.AppendLine("            new FunctionExpression(\"COUNT\", expression ?? new RawSqlExpression(\"*\"));");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FunctionExpression Sum(ISqlExpression expression) =>");
        sourceBuilder.AppendLine("            new FunctionExpression(\"SUM\", expression);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FunctionExpression Avg(ISqlExpression expression) =>");
        sourceBuilder.AppendLine("            new FunctionExpression(\"AVG\", expression);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FunctionExpression Min(ISqlExpression expression) =>");
        sourceBuilder.AppendLine("            new FunctionExpression(\"MIN\", expression);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FunctionExpression Max(ISqlExpression expression) =>");
        sourceBuilder.AppendLine("            new FunctionExpression(\"MAX\", expression);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FunctionExpression Coalesce(params ISqlExpression[] expressions) =>");
        sourceBuilder.AppendLine("            FunctionExpression.Coalesce(expressions);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FunctionExpression Upper(ISqlExpression expression) =>");
        sourceBuilder.AppendLine("            new FunctionExpression(\"UPPER\", expression);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FunctionExpression Lower(ISqlExpression expression) =>");
        sourceBuilder.AppendLine("            new FunctionExpression(\"LOWER\", expression);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FunctionExpression Length(ISqlExpression expression) =>");
        sourceBuilder.AppendLine("            new FunctionExpression(\"LENGTH\", expression);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FunctionExpression Trim(ISqlExpression expression) =>");
        sourceBuilder.AppendLine("            new FunctionExpression(\"TRIM\", expression);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        sourceBuilder.AppendLine("    public static class Date");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static DateTimeExpression CurrentDate() =>");
        sourceBuilder.AppendLine("            DateTimeExpression.CurrentDate();");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static DateTimeExpression CurrentTime() =>");
        sourceBuilder.AppendLine("            DateTimeExpression.CurrentTime();");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static DateTimeExpression CurrentTimestamp() =>");
        sourceBuilder.AppendLine("            DateTimeExpression.CurrentTimestamp();");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static DateTimeExpression Now() =>");
        sourceBuilder.AppendLine("            DateTimeExpression.Now();");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static DateTimeExpression DateTrunc(string unit, ISqlExpression expression) =>");
        sourceBuilder.AppendLine("            DateTimeExpression.DateTrunc(unit, expression);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static DateTimeExpression Extract(string unit, ISqlExpression expression) =>");
        sourceBuilder.AppendLine("            DateTimeExpression.Extract(unit, expression);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        sourceBuilder.AppendLine("    public static class Json");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static JsonExpression ExtractText(ISqlExpression column, string path) =>");
        sourceBuilder.AppendLine("            new JsonExpression(column, JsonOperator.ExtractText, path);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static JsonExpression ExtractJson(ISqlExpression column, string path) =>");
        sourceBuilder.AppendLine("            new JsonExpression(column, JsonOperator.ExtractJson, path);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static JsonExpression PathExists(ISqlExpression column, string path) =>");
        sourceBuilder.AppendLine("            new JsonExpression(column, JsonOperator.PathExists, path);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static JsonExpression Contains(ISqlExpression column, string jsonValue) =>");
        sourceBuilder.AppendLine("            new JsonExpression(column, JsonOperator.Contains, jsonValue);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        sourceBuilder.AppendLine("    public static class Jsonb");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static JsonbExpression Contains(ISqlExpression column, object value) =>");
        sourceBuilder.AppendLine("            new JsonbExpression(column, JsonbOperator.StrictContains, new LiteralExpression(System.Text.Json.JsonSerializer.Serialize(value)));");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static JsonbExpression PathExists(ISqlExpression column, string jsonPath) =>");
        sourceBuilder.AppendLine("            JsonbExpression.PathExists(column.ToString(), jsonPath);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static JsonbExpression HasKey(ISqlExpression column, string key) =>");
        sourceBuilder.AppendLine("            new JsonbExpression(column, JsonbOperator.HasKey, new LiteralExpression(key));");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static JsonbExpression HasAnyKeys(ISqlExpression column, params string[] keys) =>");
        sourceBuilder.AppendLine("            new JsonbExpression(column, JsonbOperator.HasAnyKey, new ArrayLiteralExpression(keys));");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static JsonbExpression HasAllKeys(ISqlExpression column, params string[] keys) =>");
        sourceBuilder.AppendLine("            new JsonbExpression(column, JsonbOperator.HasAllKeys, new ArrayLiteralExpression(keys));");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        sourceBuilder.AppendLine("    public static class Array");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static ArrayExpression Contains(ISqlExpression column, params object[] values) =>");
        sourceBuilder.AppendLine("            ArrayExpression.Contains(column.ToString(), values);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static ArrayExpression ContainedBy(ISqlExpression column, params object[] values) =>");
        sourceBuilder.AppendLine("            ArrayExpression.ContainedBy(column.ToString(), values);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static ArrayExpression Overlaps(ISqlExpression column, params object[] values) =>");
        sourceBuilder.AppendLine("            ArrayExpression.Overlap(column.ToString(), values);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static ArrayFunctionExpression Length(ISqlExpression array, int dimension = 1) =>");
        sourceBuilder.AppendLine("            ArrayFunctionExpression.ArrayLength(array, dimension);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static ArrayFunctionExpression Position(ISqlExpression array, ISqlExpression element) =>");
        sourceBuilder.AppendLine("            ArrayFunctionExpression.ArrayPosition(array, element);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        sourceBuilder.AppendLine("    public static class Vector");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static VectorExpression L2Distance(ISqlExpression column, float[] vector) =>");
        sourceBuilder.AppendLine("            new VectorExpression(column, VectorOperator.L2Distance, new VectorLiteralExpression(vector));");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static VectorExpression InnerProduct(ISqlExpression column, float[] vector) =>");
        sourceBuilder.AppendLine("            new VectorExpression(column, VectorOperator.InnerProduct, new VectorLiteralExpression(vector));");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static VectorExpression CosineDistance(ISqlExpression column, float[] vector) =>");
        sourceBuilder.AppendLine("            new VectorExpression(column, VectorOperator.CosineDistance, new VectorLiteralExpression(vector));");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static VectorExpression CosineSimilarity(ISqlExpression column, float[] vector) =>");
        sourceBuilder.AppendLine("            VectorExpression.CosineSimilarity(column.ToString(), vector);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        sourceBuilder.AppendLine("    public static class Text");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static FullTextExpression ToTsVector(string language, ISqlExpression text) =>");
        sourceBuilder.AppendLine("            FullTextExpression.ToTsVector(language, text);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FullTextExpression ToTsQuery(string language, string query) =>");
        sourceBuilder.AppendLine("            FullTextExpression.ToTsQuery(language, query);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FullTextExpression PlainToTsQuery(string language, string query) =>");
        sourceBuilder.AppendLine("            FullTextExpression.PlainToTsQuery(language, query);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FullTextExpression TsRank(ISqlExpression vector, ISqlExpression query) =>");
        sourceBuilder.AppendLine("            FullTextExpression.TsRank(vector, query);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static FullTextMatchExpression Match(ISqlExpression tsVector, ISqlExpression tsQuery) =>");
        sourceBuilder.AppendLine("            new FullTextMatchExpression(tsVector, tsQuery);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        sourceBuilder.AppendLine("    public static class Case");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static CaseExpression When(string column, string operatorSymbol, object value, object result) =>");
        sourceBuilder.AppendLine("            new CaseExpression().When(column, operatorSymbol, value, result);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        sourceBuilder.AppendLine("    public static class Raw");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static RawSqlExpression Sql(string sql) =>");
        sourceBuilder.AppendLine("            new RawSqlExpression(sql);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        sourceBuilder.AppendLine("    public static class Literal");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static LiteralExpression Value(object value) =>");
        sourceBuilder.AppendLine("            new LiteralExpression(value);");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("        public static LiteralExpression Null() =>");
        sourceBuilder.AppendLine("            new LiteralExpression(null);");
        sourceBuilder.AppendLine("    }");
        
        sourceBuilder.AppendLine("}");

        return sourceBuilder.ToString();
    }
} 