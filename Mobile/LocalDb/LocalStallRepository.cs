using SQLite;

namespace Mobile.LocalDb;

public interface ILocalStallRepository
{
    Task<List<LocalStall>> GetAllAsync();
    Task<LocalStall?> GetByIdAsync(string stallId);
    Task UpsertBatchAsync(IEnumerable<LocalStall> stalls);
    Task UpdateLocalAudioPathAsync(string stallId, string localPath);
    Task<bool> HasDataAsync();
}

public class LocalStallRepository : ILocalStallRepository
{
    private SQLiteAsyncConnection? _db;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null) return _db;

        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return _db;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "stalls.db3");
            _db = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await _db.CreateTableAsync<LocalStall>();
            return _db;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<LocalStall>> GetAllAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<LocalStall>().ToListAsync();
    }

    public async Task<LocalStall?> GetByIdAsync(string stallId)
    {
        var db = await GetDbAsync();
        return await db.Table<LocalStall>().FirstOrDefaultAsync(s => s.StallId == stallId);
    }

    public async Task UpsertBatchAsync(IEnumerable<LocalStall> stalls)
    {
        var db = await GetDbAsync();
        var list = stalls.ToList();
        await db.RunInTransactionAsync(conn =>
        {
            foreach (var s in list)
                conn.InsertOrReplace(s);
        });
    }

    public async Task UpdateLocalAudioPathAsync(string stallId, string localPath)
    {
        var db = await GetDbAsync();
        var row = await db.Table<LocalStall>().FirstOrDefaultAsync(s => s.StallId == stallId);
        if (row is null) return;
        row.LocalAudioPath = localPath;
        await db.UpdateAsync(row);
    }

    public async Task<bool> HasDataAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<LocalStall>().CountAsync() > 0;
    }
}
