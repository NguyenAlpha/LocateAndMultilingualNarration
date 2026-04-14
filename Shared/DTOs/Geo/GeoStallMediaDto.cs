namespace Shared.DTOs.Geo;

public class GeoStallMediaDto
{
    public string Url { get; set; } = "";
    public string? Caption { get; set; }
    public bool HasCaption => !string.IsNullOrWhiteSpace(Caption);
}
