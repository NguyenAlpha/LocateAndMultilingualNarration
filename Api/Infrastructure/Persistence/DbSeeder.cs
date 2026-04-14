using Api.Infrastructure.Persistence.Seeds;

namespace Api.Infrastructure.Persistence
{
    /// <summary>
    /// Orchestrator – gọi từng seeder theo đúng thứ tự phụ thuộc.
    /// Thêm table mới: tạo file Seeds/XxxSeeder.cs rồi gọi ở đây.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            await RoleSeeder.SeedAsync(db);                  // phải chạy trước UserSeeder
            await UserSeeder.SeedAsync(db);
            await BusinessOwnerSeeder.SeedAsync(db);         // 100 business owner mẫu
            await SubscriptionOrderSeeder.SeedAsync(db);     // orders + plan cho các business
            await StallSeeder.SeedAsync(db);                 // stalls quanh phố ẩm thực
        }
    }
}
