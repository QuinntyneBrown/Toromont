using System.Reflection;
using FluentValidation;
using MediatR;
using IronvaleFleetHub.Api.Common;
using Xunit;

namespace IronvaleFleetHub.Api.IntegrationTests;

public sealed class ValidatorCoverageTests
{
    [Fact]
    public void AllCommands_MustHaveValidator_OrISkipValidation()
    {
        var assembly = typeof(Program).Assembly;

        var commandTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                || typeof(IRequest).IsAssignableFrom(t))
            .ToList();

        var validatorTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract)
            .Where(t => t.BaseType is { IsGenericType: true }
                && t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .Select(t => t.BaseType!.GetGenericArguments()[0])
            .ToHashSet();

        var missing = new List<string>();

        foreach (var commandType in commandTypes)
        {
            var hasValidator = validatorTypes.Contains(commandType);
            var hasSkip = typeof(ISkipValidation).IsAssignableFrom(commandType);

            if (!hasValidator && !hasSkip)
            {
                missing.Add(commandType.FullName ?? commandType.Name);
            }
        }

        Assert.True(missing.Count == 0,
            $"The following commands lack a validator and do not implement ISkipValidation:\n" +
            string.Join("\n", missing.OrderBy(m => m)));
    }
}
