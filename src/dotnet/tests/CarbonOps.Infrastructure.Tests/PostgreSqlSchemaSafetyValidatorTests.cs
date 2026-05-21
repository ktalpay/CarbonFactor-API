namespace CarbonOps.Infrastructure.Tests;

public sealed class PostgreSqlSchemaSafetyValidatorTests
{
    [Theory]
    [InlineData("drop table carbonops.carbon_factors;")]
    [InlineData("truncate table carbonops.carbon_factors;")]
    [InlineData("delete from carbonops.carbon_factors;")]
    [InlineData("alter table carbonops.carbon_factors drop column source;")]
    public void ValidateScriptsRejectsDestructiveTokens(string sql)
    {
        var validator = new PostgreSqlSchemaSafetyValidator();
        var scripts = new[] { new PostgreSqlSchemaScript("999_unsafe.sql", "Database/postgresql/999_unsafe.sql", sql) };

        Assert.Throws<InvalidOperationException>(() => validator.ValidateScripts(scripts));
    }

    [Fact]
    public void ValidateScriptsAllowsNonDestructiveSql()
    {
        var validator = new PostgreSqlSchemaSafetyValidator();
        var scripts = new[]
        {
            new PostgreSqlSchemaScript("001_safe.sql", "Database/postgresql/001_safe.sql", "create schema if not exists carbonops;")
        };

        validator.ValidateScripts(scripts);
    }
}
