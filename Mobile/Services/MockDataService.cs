using System.Collections.Generic;
using Mobile.Models;

namespace Mobile.Services
{
    public static class MockDataService
    {
        public static List<Booth> GetStalls()
        {
            return new List<Booth>
            {
                new Booth { Id = 1, Name = "Bánh Mì Ông 3", Description = "Bánh mì đặc sản Phố Ẩm Thực", Latitude = 10.762622, Longitude = 106.660172, Radius = 30, ImageUrl = "https://picsum.photos/200/120?random=11" },
                new Booth { Id = 2, Name = "Phở Bò Bình Dân", Description = "Phở thơm ngon", Latitude = 10.7628, Longitude = 106.6595, Radius = 25, ImageUrl = "https://picsum.photos/200/120?random=12" },
                new Booth { Id = 3, Name = "Kem Trái Cây", Description = "Kem mát lạnh", Latitude = 10.7625, Longitude = 106.6605, Radius = 20, ImageUrl = "https://picsum.photos/200/120?random=13" }
            };
        }
    }
}
