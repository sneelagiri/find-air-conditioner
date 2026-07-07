using FindAirConditioners.Web.Core.Models;

namespace FindAirConditioners.Web.Core.Services;

public sealed class SeededAirConditionerCatalog
{
    public IReadOnlyCollection<AirConditionerListing> GetListings()
    {
        return
        [
            new("ExampleStore", "Daikin Perfera 3.5kW", 1499m, "https://example.com/daikin-perfera", Notes: "Quiet inverter unit"),
            new("ExampleStore", "Mitsubishi Electric Comfort 5.0kW", 1899m, "https://example.com/mitsubishi-comfort", Notes: "Strong cooling for larger rooms"),
            new("ExampleStore", "LG DualCool 2.5kW", 999m, "https://example.com/lg-dualcool", Notes: "Budget-friendly option")
        ];
    }
}
