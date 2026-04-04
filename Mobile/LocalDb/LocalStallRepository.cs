using SQLite;

namespace Mobile.LocalDb;

/// <summary>
/// Contract cho repository truy cập bảng Stalls trong SQLite local.
/// </summary>
public interface ILocalStallRepository
{
    /// <summary>Lấy toàn bộ gian hàng đã lưu trong SQLite.</summary>
    Task<List<LocalStall>> GetAllAsync();

    /// <summary>Lấy một gian hàng theo StallId (string Guid).</summary>
    Task<LocalStall?> GetByIdAsync(string stallId);

    /// <summary>
    /// Upsert (Insert hoặc Replace) một batch gian hàng trong một transaction.
    /// Dùng sau khi sync từ API để cập nhật toàn bộ dữ liệu local.
    /// </summary>
    Task UpsertBatchAsync(IEnumerable<LocalStall> stalls);

    /// <summary>
    /// Cập nhật đường dẫn file audio đã download về máy cho một gian hàng.
    /// Được gọi sau khi AudioCacheService download xong file mp3.
    /// </summary>
    Task UpdateLocalAudioPathAsync(string stallId, string localPath);

    /// <summary>Kiểm tra SQLite có dữ liệu hay chưa (dùng để quyết định có cần sync ngay không).</summary>
    Task<bool> HasDataAsync();
}

/// <summary>
/// Implementation của ILocalStallRepository — truy cập SQLite local qua sqlite-net-pcl.
///
/// Dùng lazy initialization với double-check locking để đảm bảo DB chỉ được mở
/// một lần duy nhất, thread-safe, ngay cả khi nhiều coroutine gọi đồng thời.
/// </summary>
public class LocalStallRepository : ILocalStallRepository
{
    // Connection SQLite — null cho đến lần đầu GetDbAsync() được gọi (lazy init)
    private SQLiteAsyncConnection? _db;

    // SemaphoreSlim(1,1): mutex async — đảm bảo chỉ 1 coroutine khởi tạo DB tại một thời điểm
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Trả về connection đã sẵn sàng — khởi tạo DB lần đầu nếu chưa có.
    /// Double-check locking pattern:
    ///   - Check 1 (ngoài lock): tránh acquire semaphore không cần thiết khi đã init
    ///   - Check 2 (trong lock): tránh race condition nếu 2 coroutine vượt qua check 1 cùng lúc
    /// </summary>
    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null) return _db; // Check 1: fast path, không cần lock

        await _initLock.WaitAsync(); // Chờ lấy lock (chỉ 1 coroutine được vào)
        try
        {
            if (_db is not null) return _db; // Check 2: đề phòng coroutine khác đã init xong

            // Đường dẫn file DB trong thư mục data riêng của app (không bị user xóa dễ dàng)
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "stalls.db3");

            _db = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite |   // Cho phép đọc và ghi
                SQLiteOpenFlags.Create |      // Tạo file nếu chưa tồn tại
                SQLiteOpenFlags.SharedCache); // Cho phép nhiều connection dùng chung cache

            // Tạo bảng Stalls nếu chưa có (idempotent — an toàn khi gọi nhiều lần)
            await _db.CreateTableAsync<LocalStall>();
            return _db;
        }
        finally
        {
            _initLock.Release(); // Luôn giải phóng lock dù thành công hay lỗi
        }
    }

    /// <summary>Lấy toàn bộ gian hàng trong bảng Stalls.</summary>
    public async Task<List<LocalStall>> GetAllAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<LocalStall>().ToListAsync();
    }

    /// <summary>Tìm gian hàng theo StallId (so sánh string Guid).</summary>
    public async Task<LocalStall?> GetByIdAsync(string stallId)
    {
        var db = await GetDbAsync();
        return await db.Table<LocalStall>().FirstOrDefaultAsync(s => s.StallId == stallId);
    }

    /// <summary>
    /// Upsert toàn bộ batch trong một transaction duy nhất.
    /// InsertOrReplace: nếu StallId đã tồn tại → ghi đè, chưa có → thêm mới.
    /// Transaction đảm bảo tính nhất quán: hoặc tất cả thành công, hoặc không có gì thay đổi.
    /// </summary>
    public async Task UpsertBatchAsync(IEnumerable<LocalStall> stalls)
    {
        var db = await GetDbAsync();
        var list = stalls.ToList(); // Materialize trước để tránh enumerate nhiều lần trong transaction
        await db.RunInTransactionAsync(conn =>
        {
            foreach (var s in list)
                conn.InsertOrReplace(s); // Sync bên trong transaction (RunInTransactionAsync yêu cầu sync)
        });
    }

    /// <summary>
    /// Cập nhật LocalAudioPath cho một gian hàng sau khi download audio xong.
    /// Chỉ update field LocalAudioPath, không đụng đến các field khác.
    /// </summary>
    public async Task UpdateLocalAudioPathAsync(string stallId, string localPath)
    {
        var db = await GetDbAsync();
        var row = await db.Table<LocalStall>().FirstOrDefaultAsync(s => s.StallId == stallId);
        if (row is null) return; // Gian hàng không tồn tại trong local DB — bỏ qua

        row.LocalAudioPath = localPath;
        await db.UpdateAsync(row); // UPDATE Stalls SET LocalAudioPath = ? WHERE StallId = ?
    }

    /// <summary>
    /// Kiểm tra nhanh bảng Stalls có bản ghi nào không.
    /// Dùng trong StallService để quyết định có cần gọi API ngay khi offline hay không.
    /// </summary>
    public async Task<bool> HasDataAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<LocalStall>().CountAsync() > 0;
    }
}
