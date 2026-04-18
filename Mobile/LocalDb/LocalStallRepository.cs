using Microsoft.Extensions.Logging;
using SQLite;

// Lớp này quản lý việc lưu trữ và truy xuất dữ liệu Stall trong SQLite cục bộ trên thiết bị, 
// bao gồm khởi tạo database, đọc dữ liệu, cập nhật và đồng bộ theo lô.
namespace Mobile.LocalDb;

// Kho lưu trữ dữ liệu cục bộ cho Stall, dùng SQLite để đọc/ghi trên thiết bị.
public interface ILocalStallRepository
{
    // Lấy toàn bộ dữ liệu Stall đã lưu trong máy.
    Task<List<LocalStall>> GetAllAsync();
    // Tìm một Stall theo mã định danh.
    Task<LocalStall?> GetByIdAsync(string stallId);
    // Thêm mới hoặc cập nhật hàng loạt dữ liệu Stall.
    Task UpsertBatchAsync(IEnumerable<LocalStall> stalls);
    // Cập nhật đường dẫn âm thanh cục bộ cho một Stall.
    Task UpdateLocalAudioPathAsync(string stallId, string localPath);
    // Kiểm tra bảng đã có dữ liệu hay chưa.
    Task<bool> HasDataAsync();
}

// Triển khai repository, quản lý kết nối SQLite bất đồng bộ và bảo vệ quá trình khởi tạo DB.
public class LocalStallRepository : ILocalStallRepository
{
    private SQLiteAsyncConnection? _db;
    private static readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly ILogger<LocalStallRepository> _logger;

    public LocalStallRepository(ILogger<LocalStallRepository> logger)
    {
        _logger = logger;
    }

    // Tạo kết nối SQLite một lần duy nhất và đảm bảo an toàn khi nhiều luồng gọi cùng lúc.
    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        // Nếu kết nối đã được tạo thì dùng lại ngay.
        if (_db is not null) return _db;

        // Khóa khởi tạo để tránh nhiều luồng tạo DB cùng lúc.
        await _initLock.WaitAsync();
        try
        {
            // Kiểm tra lại sau khi vào vùng khóa vì luồng khác có thể đã tạo xong.
            if (_db is not null) return _db;

            // Xác định đường dẫn file SQLite trong thư mục dữ liệu của ứng dụng.
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "stalls.db3");
            _logger.LogInformation("[SQLite] Mở DB tại: {Path}", dbPath);

            // Tạo kết nối bất đồng bộ với các cờ cho phép đọc/ghi và tạo mới nếu chưa có.
            _db = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache);

            try
            {
                // Tạo bảng tương ứng với model LocalStall nếu chưa tồn tại.
                var result = await _db.CreateTableAsync<LocalStall>();
                _logger.LogInformation("[SQLite] CreateTable result: {Result}", result);

                // Lấy thông tin schema để hỗ trợ kiểm tra cấu trúc bảng khi debug.
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

    // Đọc toàn bộ dữ liệu Stall từ bảng cục bộ.
    public async Task<List<LocalStall>> GetAllAsync()
    {
        try
        {
            // Lấy kết nối DB đã khởi tạo.
            var db = await GetDbAsync();
            // Đọc toàn bộ dữ liệu từ bảng LocalStall.
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

    // Truy vấn một bản ghi Stall theo StallId.
    public async Task<LocalStall?> GetByIdAsync(string stallId)
    {
        // Lấy DB rồi lọc theo khóa StallId.
        var db = await GetDbAsync();
        return await db.Table<LocalStall>().FirstOrDefaultAsync(s => s.StallId == stallId);
    }

    // Ghi hàng loạt dữ liệu vào SQLite — chỉ ghi những record thực sự thay đổi.
    public async Task UpsertBatchAsync(IEnumerable<LocalStall> stalls)
    {
        try
        {
            var db = await GetDbAsync();
            var incoming = stalls.ToList();

            // Load toàn bộ bản ghi hiện tại để so sánh — một lần đọc thay vì N lần query.
            var existing = (await db.Table<LocalStall>().ToListAsync())
                .ToDictionary(s => s.StallId);

            // Lọc chỉ giữ lại những record mới hoặc có dữ liệu thay đổi.
            var toWrite = incoming.Where(s =>
                !existing.TryGetValue(s.StallId, out var old) || HasChanged(old, s)
            ).ToList();

            if (toWrite.Count == 0)
            {
                _logger.LogDebug("[SQLite] UpsertBatch: không có thay đổi, bỏ qua");
                return;
            }

            // Giữ lại LocalAudioPath đã download — API không biết path local trên máy.
            foreach (var s in toWrite)
                if (existing.TryGetValue(s.StallId, out var old) && old.LocalAudioPath is not null)
                    s.LocalAudioPath = old.LocalAudioPath;

            await db.RunInTransactionAsync(conn =>
            {
                foreach (var s in toWrite)
                    conn.InsertOrReplace(s);
            });
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("[SQLite] UpsertBatch: {Written}/{Total} rows ghi", toWrite.Count, incoming.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SQLite] UpsertBatch thất bại");
            throw;
        }
    }

    // So sánh các trường đến từ API — bỏ qua LocalAudioPath vì đó là dữ liệu local.
    private static bool HasChanged(LocalStall old, LocalStall s) =>
        old.StallName        != s.StallName
        || old.Latitude      != s.Latitude
        || old.Longitude     != s.Longitude
        || old.RadiusMeters  != s.RadiusMeters
        || old.AudioUrl      != s.AudioUrl
        || old.LanguageCode  != s.LanguageCode
        || old.VoiceId       != s.VoiceId
        || old.NarrationContentId  != s.NarrationContentId
        || old.NarrationTitle      != s.NarrationTitle
        || old.NarrationDescription != s.NarrationDescription
        || old.NarrationScriptText != s.NarrationScriptText;

    // Cập nhật riêng đường dẫn file âm thanh cục bộ của một Stall.
    public async Task UpdateLocalAudioPathAsync(string stallId, string localPath)
    {
        // Tìm dòng dữ liệu tương ứng trong SQLite.
        var db = await GetDbAsync();
        var row = await db.Table<LocalStall>().FirstOrDefaultAsync(s => s.StallId == stallId);
        // Nếu không có bản ghi thì bỏ qua.
        if (row is null) return;

        // Gán lại đường dẫn file âm thanh mới rồi lưu xuống DB.
        row.LocalAudioPath = localPath;
        await db.UpdateAsync(row);
    }

    // Kiểm tra xem bảng Stalls đã có ít nhất một dòng dữ liệu chưa.
    public async Task<bool> HasDataAsync()
    {
        // Đếm số bản ghi, chỉ cần lớn hơn 0 là đã có dữ liệu.
        var db = await GetDbAsync();
        return await db.Table<LocalStall>().CountAsync() > 0;
    }
}
