using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TaskApi.Infrastructure.Persistence;

public sealed class TaskApiDbContextFactory
    : IDesignTimeDbContextFactory<TaskApiDbContext>
{
    public TaskApiDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(
                Assembly.Load("TaskApi.Api"),
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration
            .GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection must be configured in User Secrets or environment variables for EF tooling.");
        }

        var options = new DbContextOptionsBuilder<TaskApiDbContext>();
        options.UseSqlServer(
            connectionString,
            sqlOptions => sqlOptions.MigrationsAssembly(
                typeof(TaskApiDbContext).Assembly.GetName().Name));

        return new TaskApiDbContext(options.Options);
    }
}
