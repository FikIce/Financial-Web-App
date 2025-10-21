using Finec.Data;
using Finec.Models;
using Finec.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Finec.Controllers
{
    [Authorize]
    public class AssetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssetsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Assets
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // --- 1. FETCH RAW DATA ---
            var assets = await _context.Assets
                                .Include(a => a.History)
                                .Where(a => a.UserId == currentUser.Id)
                                .ToListAsync();

            var accounts = await _context.Accounts
                                .Where(a => a.UserId == currentUser.Id)
                                .ToListAsync();

            // --- 2. CALCULATE CURRENT METRICS ---
            var totalAssetBalance = assets.Sum(a => a.CurrentValue);
            var totalAccountBalance = accounts.Sum(a => a.CurrentBalance);
            var currentNetWorth = totalAssetBalance + totalAccountBalance;

            // --- 3. CALCULATE HISTORICAL METRICS FOR GROWTH ---
            var oneMonthAgo = DateTime.Now.AddMonths(-1);

            decimal historicalAssetValue = 0;
            foreach (var asset in assets)
            {
                var latestRecord = asset.History
                                        .Where(h => h.DateRecorded <= oneMonthAgo)
                                        .OrderByDescending(h => h.DateRecorded)
                                        .FirstOrDefault();
                if (latestRecord != null) historicalAssetValue += latestRecord.Value;
            }

            decimal previousNetWorth = historicalAssetValue + totalAccountBalance;

            decimal netWorthChangeAbsolute = currentNetWorth - previousNetWorth;
            double netWorthChangePercentage = (previousNetWorth == 0 || currentNetWorth == 0) ? 0 : ((double)(currentNetWorth - previousNetWorth) / (double)previousNetWorth);

            // --- 4. PREPARE DATA FOR DOUGHNUT CHART (ASSET ALLOCATION) ---
            // THIS IS THE LOGIC THAT WAS MISSING FROM THE PREVIOUS SNIPPET
            var allocations = new List<AssetAllocationItem>();

            var assetGroups = assets.GroupBy(a => a.Type.ToString())
                                    .Select(group => new AssetAllocationItem
                                    {
                                        AssetType = group.Key,
                                        Value = group.Sum(a => a.CurrentValue)
                                    });
            allocations.AddRange(assetGroups);

            if (totalAccountBalance > 0)
            {
                allocations.Add(new AssetAllocationItem { AssetType = "Cash & Deposits", Value = totalAccountBalance });
            }

            // --- 5. PREPARE DATA FOR LINE CHART (NET WORTH HISTORY) ---
            var chartLabels = new List<string>();
            var chartData = new List<decimal>();
            var dateIterator = DateTime.Now.AddMonths(-5);

            while (dateIterator <= DateTime.Now)
            {
                chartLabels.Add(dateIterator.ToString("MMM yyyy"));

                decimal loopHistoricalAssetValue = 0;
                foreach (var asset in assets)
                {
                    var latestRecord = asset.History
                                            .Where(h => h.DateRecorded <= dateIterator)
                                            .OrderByDescending(h => h.DateRecorded)
                                            .FirstOrDefault();

                    if (latestRecord != null)
                    {
                        loopHistoricalAssetValue += latestRecord.Value;
                    }
                }
                chartData.Add(loopHistoricalAssetValue + totalAccountBalance);
                dateIterator = dateIterator.AddMonths(1);
            }

            // --- 6. ASSEMBLE THE FINAL VIEWMODEL ---
            var viewModel = new AssetsIndexViewModel
            {
                NetWorth = currentNetWorth,
                TotalAssetBalance = totalAssetBalance,
                TotalAccountBalance = totalAccountBalance,
                NetWorthChangeAbsolute = netWorthChangeAbsolute,
                NetWorthChangePercentage = netWorthChangePercentage,
                AssetAllocations = allocations,
                NetWorthChartLabels = chartLabels,
                NetWorthChartData = chartData,
                Accounts = accounts
            };

            return View(viewModel);
        }

        // GET: Assets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Assets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // THE FIX: We have added "Type" to the bouncer's list. It is now allowed in.
        public async Task<IActionResult> Create([Bind("AssetName,Type,CurrentValue")] Asset asset)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            asset.UserId = currentUser.Id;

            ModelState.Remove("User");
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                // This logic was already perfect. It creates the asset's starting history point.
                var initialHistory = new AssetHistory
                {
                    Asset = asset,
                    DateRecorded = DateTime.Now,
                    Value = asset.CurrentValue
                };

                _context.Add(asset);
                _context.Add(initialHistory);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // If the model is not valid (e.g., they didn't select a Type),
            // we return the view so they can see the validation error.
            return View(asset);
        }

        // We will implement Edit and Delete later.
    }
}