namespace PaunixGuard.Core.Events;

public interface IEventHistoryStore
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task AddAsync(GuardEvent guardEvent, CancellationToken cancellationToken);

    Task UpdateAsync(GuardEvent guardEvent, CancellationToken cancellationToken);

    Task<GuardEvent?> GetLatestAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<GuardEvent>> GetAllAsync(int limit = 100, CancellationToken cancellationToken = default);
}

