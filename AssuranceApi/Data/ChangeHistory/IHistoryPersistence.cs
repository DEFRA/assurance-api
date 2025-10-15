using AssuranceApi.Project.Models;

namespace AssuranceApi.Data.ChangeHistory
{
    /// <summary>
    /// Defines methods for persisting and retrieving history entries for a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the item for which history is being managed.</typeparam>
    public interface IHistoryPersistence<T>
    {
        /// <summary>
        /// Creates a new history entry.
        /// </summary>
        /// <param name="history">The history entry to create.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
        Task<bool> CreateAsync(History<T> history);

        /// <summary>
        /// Retrieves the history entries for a specific item.
        /// </summary>
        /// <param name="id">The ID of the item.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of history entries.</returns>
        Task<IEnumerable<History<T>>> GetHistoryAsync(string id);
    }
}
