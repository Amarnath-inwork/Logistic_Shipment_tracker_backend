using System.Text.Json;

namespace Logistic_Shipment_tracker.Services
{
    public class NominatimDistanceService : IDistanceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NominatimDistanceService> _logger;
        private static readonly Dictionary<
            string,
            (double lat, double lon, DateTime cached)> _geocodeCache = new();
        private static readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(24);

        public class NominatimResult
        {
            public string lat { get; set; } = "";
            public string lon { get; set; } = "";
            public string display_name { get; set; } = "";
        }

        public NominatimDistanceService(
            HttpClient httpClient ,
            ILogger<NominatimDistanceService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                "LogisticTracker/1.0 (contact@logistictracker.com)"
            );
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<(double latitude , double longitude)> GeocodeAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                _logger.LogWarning("Empty address provided for geocoding");
                return (0, 0);
            }
            // Check cache first
            var cacheKey = address.ToLowerInvariant().Trim();
            if (_geocodeCache.ContainsKey(cacheKey))
            {
                var cached = _geocodeCache[cacheKey];
                if(DateTime.UtcNow - cached.cached < _cacheExpiry)
                {
                    _logger.LogDebug("Using cached coordinates for address: {Address}", address);
                    return (cached.lat, cached.lon);
                }
                else
                {
                    // remove expored cache entry
                    _geocodeCache.Remove(cacheKey);
                }
            }

            try
            {
                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://nominatim.openstreetmap.org/search?format=json&q={encodedAddress}&limit=1&addressdetails=1";


                _logger.LogDebug("Geocoding address: {Address}", address);

                // Add a small delay to respect rate limits (1 request per second max)
                await Task.Delay(1000);
                var response = await _httpClient.GetStringAsync(url);
                var results = JsonSerializer.Deserialize<NominatimResult[]>(response);

                if (results?.Length > 0)
                {
                    var result = results[0];
                    if (
                        double.TryParse(result.lat, out var lat)
                        && double.TryParse(result.lon, out var lon)
                    )
                    {
                        // Cache the result
                        _geocodeCache[cacheKey] = (lat, lon, DateTime.UtcNow);

                        _logger.LogDebug(
                            "Successfully geocoded address: {Address} -> ({Lat}, {Lon})",
                            address,
                            lat,
                            lon
                        );
                        return (lat, lon);
                    }
                }

                _logger.LogWarning("No results found for address: {Address}", address);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while geocoding address: {Address}", address);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout while geocoding address: {Address}", address);
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "JSON parsing error while geocoding address: {Address}",
                    address
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while geocoding address: {Address}",
                    address
                );
            }
            return (0, 0);
        }
        public async Task<double> CalculateDistanceAsync(
            string originAddress,
            string destinationAddress
        )
        {
            if (
                string.IsNullOrWhiteSpace(originAddress)
                || string.IsNullOrWhiteSpace(destinationAddress)
            )
            {
                _logger.LogWarning("Empty addresses provided for distance calculation");
                return 25; // Default fallback distance
            }

            try
            {
                _logger.LogDebug(
                    "Calculating distance between: {Origin} and {Destination}",
                    originAddress,
                    destinationAddress
                );

                var originCoords = await GeocodeAddressAsync(originAddress);
                var destCoords = await GeocodeAddressAsync(destinationAddress);

                if (originCoords.latitude == 0 || destCoords.latitude == 0)
                {
                    _logger.LogWarning(
                        "Failed to geocode one or both addresses. Origin: {Origin}, Destination: {Destination}",
                        originAddress,
                        destinationAddress
                    );
                    return 25; // Fallback distance
                }

                var distance = CalculateHaversineDistance(
                    originCoords.latitude,
                    originCoords.longitude,
                    destCoords.latitude,
                    destCoords.longitude
                );

                _logger.LogDebug(
                    "Calculated distance: {Distance} km between {Origin} and {Destination}",
                    distance,
                    originAddress,
                    destinationAddress
                );

                return distance;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error calculating distance between {Origin} and {Destination}",
                    originAddress,
                    destinationAddress
                );
                return 25; // Fallback distance
            }
        }

        /// <summary>
        /// Calculate the straight-line distance between two points using the Haversine formula
        /// </summary>
        private static double CalculateHaversineDistance(
            double lat1,
            double lon1,
            double lat2,
            double lon2
        )
        {
            const double R = 6371; // Earth's radius in kilometers

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1))
                    * Math.Cos(ToRadians(lat2))
                    * Math.Sin(dLon / 2)
                    * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = R * c;

            return Math.Round(distance, 2); // Round to 2 decimal places
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

        /// <summary>
        /// Clear the geocoding cache (useful for testing or memory management)
        /// </summary>
        public static void ClearCache()
        {
            _geocodeCache.Clear();
        }

        /// <summary>
        /// Get cache statistics for monitoring
        /// </summary>
        public static (int count, int expired) GetCacheStats()
        {
            var now = DateTime.UtcNow;
            var expired = _geocodeCache.Values.Count(c => now - c.cached >= _cacheExpiry);
            return (_geocodeCache.Count, expired);
        }
    }
}
