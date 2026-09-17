using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SchoolERP.DataAccess;

namespace SchoolERP.Tests;

public class DataAccessTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value))).Build();

    [Fact]
    public void Each_module_gets_its_own_database_on_the_shared_server()
    {
        var config = Config(("ConnectionStrings:SchoolDb", "Server=db.example;Port=4000;Uid=app;Pwd=secret;SslMode=VerifyFull;"));

        var student = new MySqlConnectionStringBuilder(DataAccessServiceCollectionExtensions.ConnectionStringFor(config, "student"));
        var fee = new MySqlConnectionStringBuilder(DataAccessServiceCollectionExtensions.ConnectionStringFor(config, "fee"));

        Assert.Equal("student", student.Database);
        Assert.Equal("fee", fee.Database);
        Assert.Equal("db.example", student.Server);
        Assert.Equal("secret", fee.Password);
    }

    [Fact]
    public void An_explicit_module_connection_string_wins()
    {
        var config = Config(
            ("ConnectionStrings:SchoolDb", "Server=shared;Uid=app;Pwd=x;"),
            ("ConnectionStrings:ReportingDb", "Server=reporting-replica;Database=reporting;Uid=ro;Pwd=y;"));

        Assert.Equal("Server=reporting-replica;Database=reporting;Uid=ro;Pwd=y;",
            DataAccessServiceCollectionExtensions.ConnectionStringFor(config, "reporting"));
    }

    [Fact]
    public void All_twelve_module_databases_are_registered()
    {
        var databases = DataAccessServiceCollectionExtensions.Modules.Select(m => m.Database).ToList();

        Assert.Equal(12, databases.Distinct().Count());
        Assert.Contains("identity", databases);
        Assert.Contains("reporting", databases);
    }
}
