namespace Api.Application.Services;

public interface IGeoService
{
    double CalculateDistanceMeters(double latitude1, double longitude1, double latitude2, double longitude2);
    bool IsInsideRadius(double sourceLatitude, double sourceLongitude, double targetLatitude, double targetLongitude, double radiusMeters);
}
