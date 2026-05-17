using PaunixGuard.Core.Events;

namespace PaunixGuard.App.Tests.Fakes;

internal sealed class FakeEventHistoryStore : IEventHistoryStore
{
    public List<GuardEvent> Events { get; } = [];

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task AddAsync(GuardEvent guardEvent, CancellationToken cancellationToken)
    {
        Events.Add(guardEvent);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(GuardEvent guardEvent, CancellationToken cancellationToken)
    {
        var index = Events.FindIndex(x => x.Id == guardEvent.Id);
        if (index >= 0)
        {
            Events[index] = guardEvent;
        }

        return Task.CompletedTask;
    }

    public Task<GuardEvent?> GetLatestAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Events.OrderByDescending(x => x.CreatedAt).FirstOrDefault());
    }

    public Task<IReadOnlyList<GuardEvent>> GetAllAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 1000);
        return Task.FromResult<IReadOnlyList<GuardEvent>>(
            Events.OrderByDescending(x => x.CreatedAt).Take(limit).ToList().AsReadOnly());
    }
}
