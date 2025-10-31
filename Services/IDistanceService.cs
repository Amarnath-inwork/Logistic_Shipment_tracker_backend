namespace Logistic_Shipment_tracker.Services
{
    public interface IDistanceService
    {
        Task<double> CalculateDistanceAsync(string originAddress, string destinationAddress);
        Task<(double latitude, double longitude)> GeocodeAddressAsync(string address);
    }
}
