namespace Api.Application.Services;

public class GeoService : IGeoService
{
    private const double EarthRadiusMeters = 6_371_000;

    public double CalculateDistanceMeters(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        var lat1 = DegreeToRadian(latitude1);
        var lon1 = DegreeToRadian(longitude1);
        var lat2 = DegreeToRadian(latitude2);
        var lon2 = DegreeToRadian(longitude2);

        var dLat = lat2 - lat1;
        var dLon = lon2 - lon1;

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    public bool IsInsideRadius(double sourceLatitude, double sourceLongitude, double targetLatitude, double targetLongitude, double radiusMeters)
    {
        var distance = CalculateDistanceMeters(sourceLatitude, sourceLongitude, targetLatitude, targetLongitude);
        return distance <= radiusMeters;
    }

    private static double DegreeToRadian(double degree) => degree * Math.PI / 180.0;
}
