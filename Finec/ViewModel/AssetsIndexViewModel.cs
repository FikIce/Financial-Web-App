using Finec.Models;
using System.Collections.Generic;

namespace Finec.ViewModels
{
    // A small helper class to hold data for the doughnut chart
    public class AssetAllocationItem
    {
        public string AssetType { get; set; }
        public decimal Value { get; set; }
    }

    // The main ViewModel for the entire Assets Index page
    public class AssetsIndexViewModel
    {
        // Card Metrics
        public decimal NetWorth { get; set; }
        public decimal TotalAssetBalance { get; set; }
        public decimal TotalAccountBalance { get; set; }
        public decimal NetWorthChangeAbsolute { get; set; }
        public double NetWorthChangePercentage { get; set; }

        // Data for the Net Worth Line Chart
        public List<string> NetWorthChartLabels { get; set; } = new List<string>();
        public List<decimal> NetWorthChartData { get; set; } = new List<decimal>();

        // Data for the Asset Allocation Doughnut Chart
        public List<AssetAllocationItem> AssetAllocations { get; set; } = new List<AssetAllocationItem>();
        public List<Account> Accounts { get; set; } = new List<Account>();
    }
}