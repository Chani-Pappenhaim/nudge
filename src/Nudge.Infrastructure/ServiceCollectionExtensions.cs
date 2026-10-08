using Microsoft.Extensions.DependencyInjection;
using Nudge.Core.Abstractions;
using Nudge.Core.Models;
using Nudge.Infrastructure.Audio;
using Nudge.Infrastructure.Persistence;
using Nudge.Infrastructure.Startup;

namespace Nudge.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers file storage, sounds and startup integration for Windows.</summary>
    public static IServiceCollection AddNudgeInfrastructure(
        this IServiceCollection services, string dataDirectory, string executablePath)
    {
        services.AddSingleton<IReminderRepository>(_ => new JsonReminderRepository(
            new JsonFileStore<ReminderRecord>(Path.Combine(dataDirectory, "reminders.json"))));
        services.AddSingleton<IHistoryRepository>(_ => new JsonHistoryRepository(
            new JsonFileStore<HistoryEntry>(Path.Combine(dataDirectory, "history.json"))));
        services.AddSingleton<IDebtRepository>(_ => new JsonDebtRepository(
            new JsonFileStore<DebtRecord>(Path.Combine(dataDirectory, "debts.json"))));
        services.AddSingleton<ISoundPlayer, WindowsSoundPlayer>();
        services.AddSingleton<IStartupManager>(_ => new RegistryStartupManager(executablePath));
        return services;
    }
}
