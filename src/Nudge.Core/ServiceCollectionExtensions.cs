using Microsoft.Extensions.DependencyInjection;
using Nudge.Core.Services;

namespace Nudge.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the core reminder use cases.</summary>
    public static IServiceCollection AddNudgeCore(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ReminderService>();
        return services;
    }
}
