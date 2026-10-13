using System.Reflection;
using NetArchTest.Rules;
using Switchback.Application.Services;
using Switchback.Data;
using Switchback.Infrastructure.Persistence.Entities;
using Switchback.Shared.Contracts;

namespace Switchback.Tests;

/// <summary>
/// The persistence gate as a failing test (Slim Domain §4.6). The guidance keys the gate on namespaces; here
/// the unit is the assembly, so a type cannot opt in by declaring a namespace it does not belong to.
/// The console runner is left out on purpose: it is measurement tooling that must see both libraries' SQL.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly[] ApplicationAssemblies =
    [
        typeof(Program).Assembly,                 // Switchback.Web
        typeof(RecentOrdersService).Assembly,     // Switchback.Application.Services
        typeof(SwitchbackDbContext).Assembly,     // Switchback.Data
        typeof(RecentOrderRow).Assembly,          // Switchback.Infrastructure.Persistence.Entities
        typeof(RecentOrderResponse).Assembly,     // Switchback.Shared.Contracts
    ];

    // FunkyORM: only the entities (attributes) and the services (the gate).
    // EF Core: only Switchback.Data (the schema owner) and the services.
    public static TheoryData<string, string> ForbiddenReferences => new()
    {
        { "Switchback.Web", "Funcular.Data.Orm" },
        { "Switchback.Data", "Funcular.Data.Orm" },
        { "Switchback.Shared.Contracts", "Funcular.Data.Orm" },
        { "Switchback.Web", "Microsoft.EntityFrameworkCore" },
        { "Switchback.Infrastructure.Persistence.Entities", "Microsoft.EntityFrameworkCore" },
        { "Switchback.Shared.Contracts", "Microsoft.EntityFrameworkCore" },
    };

    [Theory]
    [MemberData(nameof(ForbiddenReferences))]
    public void Only_the_approved_assemblies_reference_a_data_access_library(string assemblyName, string library)
    {
        var assembly = ApplicationAssemblies.Single(a => a.GetName().Name == assemblyName);

        var result = Types.InAssembly(assembly)
            .ShouldNot().HaveDependencyOn(library)
            .GetResult();

        Assert.True(result.IsSuccessful, $"{assemblyName} references {library}: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Read_models_are_sealed()
    {
        var result = Types.InAssembly(typeof(RecentOrderRow).Assembly)
            .That().ImplementInterface(typeof(IReadModel))
            .Should().BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Entities_carry_columns_and_nothing_else()
    {
        var offenders =
            from type in typeof(RecentOrderRow).Assembly.GetTypes()
            where type.IsClass && type.IsPublic
            from property in type.GetProperties()
            where !IsColumnType(property.PropertyType)
            select $"{type.Name}.{property.Name} ({property.PropertyType.Name})";

        Assert.Empty(offenders);
    }

    // A column is a scalar. A class-typed property is a navigation property, which the pattern forbids.
    private static bool IsColumnType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(byte[]);
    }
}
