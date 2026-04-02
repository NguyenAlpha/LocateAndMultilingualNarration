using Microsoft.Extensions.Logging;
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
    private readonly ILogger<LocalStallRepository> _logger;

    public LocalStallRepository(ILogger<LocalStallRepository> logger)
    {
        _logger = logger;
    }

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null) return _db;

        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return _db;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "stalls.db3");
            _logger.LogInformation("[SQLite] Mở DB tại: {Path}", dbPath);

            _db = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache);

            try
            {
                var result = await _db.CreateTableAsync<LocalStall>();
                _logger.LogInformation("[SQLite] CreateTable result: {Result}", result);

                var cols = await _db.GetTableInfoAsync("Stalls");
                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("[SQLite] Schema Stalls: {Columns}",
                        string.Join(", ", cols.Select(c => c.Name)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SQLite] Lỗi khi CreateTable — DB có thể bị corrupt");
                throw;
            }

            return _db;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<LocalStall>> GetAllAsync()
    {
        try
        {
            var db = await GetDbAsync();
            var list = await db.Table<LocalStall>().ToListAsync();
            _logger.LogInformation("[SQLite] GetAllAsync: {Count} rows", list.Count);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SQLite] GetAllAsync thất bại");
            return [];
        }
    }

    public async Task<LocalStall?> GetByIdAsync(string stallId)
    {
        var db = await GetDbAsync();
        return await db.Table<LocalStall>().FirstOrDefaultAsync(s => s.StallId == stallId);
    }

    public async Task UpsertBatchAsync(IEnumerable<LocalStall> stalls)
    {
        try
        {
            var db = await GetDbAsync();
            var list = stalls.ToList();
            await db.RunInTransactionAsync(conn =>
            {
                foreach (var s in list)
                    conn.InsertOrReplace(s);
            });
            _logger.LogInformation("[SQLite] UpsertBatch: {Count} rows OK", list.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SQLite] UpsertBatch thất bại");
            throw;
        }
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
