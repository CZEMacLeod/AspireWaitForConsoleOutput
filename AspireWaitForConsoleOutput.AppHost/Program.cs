using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql");
var sqldb = sql.AddDatabase("sqldb");

// Example of waiting for a console app to output a specific message
// This could be an Executable of some sort - such as Node, Python, etc.
// We wait for some pre-requisite to be ready, then we wait for the console app to output a specific message
var console = builder.AddProject<Projects.ConsoleApp>("consoleapp")
    .WithReference(sqldb, "sqldb")
    .WaitFor(sqldb);

// webapp won't start until console has output the message "Ready Now..."
// Note that 'console' does not have to exit, it just has to output the message
builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(sqldb, "sqldb")
    .WaitFor(sqldb)
    .WaitForOutput(console, m => m == "Ready Now...");

builder.Build().Run();
