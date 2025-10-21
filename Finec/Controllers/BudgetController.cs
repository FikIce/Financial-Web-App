using Finec.Data;
using Finec.Models;
using Finec.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Finec.Controllers
{
    [Authorize]
    public class BudgetController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public BudgetController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? year)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var selectedYear = year ?? DateTime.Now.Year;

            var budgets = await _context.Budgets
                .Include(b => b.Account)
                .Include(b => b.Asset)
                .Where(b => b.UserId == currentUser.Id &&
                            b.Year == selectedYear &&
                            (b.Type == BudgetType.Income || b.Type == BudgetType.Saving))
                .ToListAsync();

            var viewModel = new BudgetIndexViewModel();
            viewModel.StartDate = new DateTime(selectedYear, 1, 1);

            var groupedBudgets = budgets.GroupBy(b => new {
                b.CategoryName,
                b.Type,
                b.AccountId,
                AccountName = b.Account.AccountName,
                b.AssetId
            });

            foreach (var group in groupedBudgets)
            {
                var categoryVm = new BudgetCategoryViewModel
                {
                    CategoryName = group.Key.CategoryName,
                    AccountId = group.Key.AccountId,
                    AccountName = group.Key.AccountName,
                    AssetId = group.Key.AssetId,
                    Type = group.Key.Type,
                    MonthlyAmounts = group.ToDictionary(item => item.Month, item => item.PlannedAmount)
                };
                if (group.Key.Type == BudgetType.Income) viewModel.IncomeCategories.Add(categoryVm);
                else if (group.Key.Type == BudgetType.Saving) viewModel.SavingCategories.Add(categoryVm);
            }

            var accounts = await _context.Accounts.Where(a => a.UserId == currentUser.Id).ToListAsync();
            var assets = await _context.Assets.Where(a => a.UserId == currentUser.Id).ToListAsync();
            ViewBag.AccountsList = new SelectList(accounts, "Id", "AccountName");
            ViewBag.AssetsList = new SelectList(assets, "Id", "AssetName");

            for (int month = 1; month <= 12; month++)
            {
                viewModel.MonthlyIncomeTotals[month] = viewModel.IncomeCategories.Sum(c => c.MonthlyAmounts.ContainsKey(month) ? c.MonthlyAmounts[month] : 0);
                viewModel.MonthlySavingTotals[month] = viewModel.SavingCategories.Sum(c => c.MonthlyAmounts.ContainsKey(month) ? c.MonthlyAmounts[month] : 0);
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string categoryName, BudgetType type, int year, int accountId, int? assetId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (!await _context.Budgets.AnyAsync(b => b.UserId == currentUser.Id && b.Year == year && b.CategoryName == categoryName && b.Type == type && b.AccountId == accountId))
            {
                _context.Budgets.Add(new Budget { CategoryName = categoryName, Type = type, AccountId = accountId, AssetId = assetId, PlannedAmount = 0, Month = 1, Year = year, UserId = currentUser.Id });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { year = year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(string categoryName, BudgetType type, int year, int accountId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var budgetEntriesToDelete = await _context.Budgets
                .Where(b => b.UserId == currentUser.Id && b.Year == year && b.CategoryName == categoryName && b.Type == type && b.AccountId == accountId)
                .ToListAsync();

            if (budgetEntriesToDelete.Any())
            {
                var budgetIdsToDelete = budgetEntriesToDelete.Select(b => b.Id).ToList();
                var transactionsToDelete = await _context.Transactions
                    .Where(t => t.BudgetId.HasValue && budgetIdsToDelete.Contains(t.BudgetId.Value))
                    .ToListAsync();

                _context.Transactions.RemoveRange(transactionsToDelete);
                _context.Budgets.RemoveRange(budgetEntriesToDelete);
                await _context.SaveChangesAsync();

                await RecalculateAccountBalanceAsync(accountId);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { year = year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBudgetRow(string categoryName, BudgetType type, int year, int accountId, int? assetId, Dictionary<int, decimal> monthlyAmounts)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            foreach (var monthAmount in monthlyAmounts)
            {
                var month = monthAmount.Key;
                var plannedAmount = monthAmount.Value;

                var budgetEntry = await _context.Budgets.FirstOrDefaultAsync(b =>
                    b.UserId == currentUser.Id && b.Year == year && b.Month == month &&
                    b.CategoryName == categoryName && b.Type == type && b.AccountId == accountId);

                decimal oldAmountInTransaction = 0;

                // If a budget entry for this month already exists, find its transaction's old amount
                if (budgetEntry != null)
                {
                    var existingTransaction = await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.BudgetId == budgetEntry.Id);
                    if (existingTransaction != null)
                    {
                        oldAmountInTransaction = existingTransaction.Amount;
                    }
                }

                if (budgetEntry == null)
                {
                    budgetEntry = new Budget { CategoryName = categoryName, Type = type, Month = month, Year = year, PlannedAmount = plannedAmount, UserId = currentUser.Id, AccountId = accountId, AssetId = assetId };
                    _context.Budgets.Add(budgetEntry);
                    // THE CRITICAL FIX: Save the new Budget entry IMMEDIATELY to get its ID.
                    await _context.SaveChangesAsync();
                }
                else
                {
                    budgetEntry.PlannedAmount = plannedAmount;
                }

                if (plannedAmount > 0)
                {
                    var transactionType = type == BudgetType.Income ? TransactionType.Income : TransactionType.Saving;
                    var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.BudgetId == budgetEntry.Id);
                    if (transaction == null)
                    {
                        transaction = new Transaction { BudgetId = budgetEntry.Id, UserId = currentUser.Id };
                        _context.Transactions.Add(transaction);
                    }
                    transaction.Type = transactionType;
                    transaction.Amount = plannedAmount;
                    transaction.Description = $"Budget Entry: {categoryName}";
                    transaction.TransactionDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                    transaction.AccountId = accountId;
                }
                else
                {
                    var transactionToDelete = await _context.Transactions.FirstOrDefaultAsync(t => t.BudgetId == budgetEntry.Id);
                    if (transactionToDelete != null)
                    {
                        _context.Transactions.Remove(transactionToDelete);
                    }
                }

                if (type == BudgetType.Saving && assetId.HasValue)
                {
                    var assetToUpdate = await _context.Assets.FindAsync(assetId.Value);
                    if (assetToUpdate != null)
                    {
                        // Apply the DIFFERENCE to the asset value
                        assetToUpdate.CurrentValue += (plannedAmount - oldAmountInTransaction);
                        _context.Update(assetToUpdate);
                    }
                }
            }

            await _context.SaveChangesAsync();
            await RecalculateAccountBalanceAsync(accountId);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { year = year });
        }

        //public async Task<IActionResult> Index(int? year)
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    var selectedYear = year ?? DateTime.Now.Year;

        //    // THE FENCE: This query now ONLY fetches Income and Saving types.
        //    // It is now physically incapable of being contaminated by expense data.
        //    var budgets = await _context.Budgets
        //        .Where(b => b.UserId == currentUser.Id &&
        //                    b.Year == selectedYear &&
        //                    (b.Type == BudgetType.Income || b.Type == BudgetType.Saving))
        //        .ToListAsync();

        //    var viewModel = new BudgetIndexViewModel();
        //    viewModel.StartDate = new DateTime(selectedYear, 1, 1);

        //    var groupedBudgets = budgets.GroupBy(b => new { b.CategoryName, b.Type });

        //    foreach (var group in groupedBudgets)
        //    {
        //        var categoryVm = new BudgetCategoryViewModel
        //        {
        //            CategoryName = group.Key.CategoryName,
        //            Type = group.Key.Type,
        //            MonthlyAmounts = group.ToDictionary(item => item.Month, item => item.PlannedAmount)
        //        };

        //        if (group.Key.Type == BudgetType.Income)
        //            viewModel.IncomeCategories.Add(categoryVm);
        //        else if (group.Key.Type == BudgetType.Saving)
        //            viewModel.SavingCategories.Add(categoryVm);
        //    }

        //    for (int month = 1; month <= 12; month++)
        //    {
        //        viewModel.MonthlyIncomeTotals[month] = viewModel.IncomeCategories.Sum(c => c.MonthlyAmounts.ContainsKey(month) ? c.MonthlyAmounts[month] : 0);
        //        viewModel.MonthlySavingTotals[month] = viewModel.SavingCategories.Sum(c => c.MonthlyAmounts.ContainsKey(month) ? c.MonthlyAmounts[month] : 0);
        //    }

        //    return View(viewModel);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddCategory(string categoryName, BudgetType type, int year)
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    if (!await _context.Budgets.AnyAsync(b => b.UserId == currentUser.Id && b.Year == year && b.CategoryName == categoryName && b.Type == type))
        //    {
        //        _context.Budgets.Add(new Budget { CategoryName = categoryName, Type = type, PlannedAmount = 0, Month = 1, Year = year, UserId = currentUser.Id });
        //        await _context.SaveChangesAsync();
        //    }
        //    return RedirectToAction(nameof(Index), new { year = year });
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> UpdateBudgetRow(string categoryName, BudgetType type, int year, Dictionary<int, decimal> monthlyAmounts)
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    var userAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == currentUser.Id);
        //    if (userAccount == null)
        //    {
        //        TempData["ErrorMessage"] = "Please add an account before logging a budget.";
        //        return RedirectToAction("Index", "Accounts");
        //    }

        //    foreach (var monthAmount in monthlyAmounts)
        //    {
        //        var month = monthAmount.Key;
        //        var plannedAmount = monthAmount.Value;

        //        var budgetEntry = await _context.Budgets.FirstOrDefaultAsync(b =>
        //            b.UserId == currentUser.Id && b.Year == year && b.Month == month &&
        //            b.CategoryName == categoryName && b.Type == type);

        //        if (budgetEntry == null)
        //        {
        //            budgetEntry = new Budget { CategoryName = categoryName, Type = type, Month = month, Year = year, PlannedAmount = plannedAmount, UserId = currentUser.Id };
        //            _context.Budgets.Add(budgetEntry);
        //        }
        //        else
        //        {
        //            budgetEntry.PlannedAmount = plannedAmount;
        //        }
        //        await _context.SaveChangesAsync();

        //        // THE CRITICAL FIX: The check for "budgetDate <= DateTime.Now" is GONE.
        //        // We now create a transaction as long as the planned amount is greater than zero.
        //        if (plannedAmount > 0)
        //        {
        //            var transactionType = type == BudgetType.Income ? TransactionType.Income : TransactionType.Saving;
        //            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.BudgetId == budgetEntry.Id);
        //            if (transaction == null)
        //            {
        //                transaction = new Transaction { BudgetId = budgetEntry.Id, UserId = currentUser.Id };
        //                _context.Transactions.Add(transaction);
        //            }
        //            transaction.Type = transactionType;
        //            transaction.Amount = plannedAmount;
        //            transaction.Description = $"Budget Entry: {categoryName}";
        //            transaction.TransactionDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        //            transaction.AccountId = userAccount.Id;
        //        }
        //        // If planned amount is 0, we should remove any existing transaction.
        //        else
        //        {
        //            var transactionToDelete = await _context.Transactions.FirstOrDefaultAsync(t => t.BudgetId == budgetEntry.Id);
        //            if (transactionToDelete != null)
        //            {
        //                _context.Transactions.Remove(transactionToDelete);
        //            }
        //        }
        //    }

        //    await _context.SaveChangesAsync();
        //    await RecalculateAccountBalanceAsync(userAccount.Id);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction(nameof(Index), new { year = year });
        //}

        //// Add this new method to your ExpensesController

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteCategory(string categoryName, int year)
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);

        //    // 1. Find all budget entries for this category and year.
        //    var budgetEntriesToDelete = await _context.Budgets
        //        .Where(b => b.UserId == currentUser.Id &&
        //                    b.Year == year &&
        //                    b.CategoryName == categoryName &&
        //                    b.Type == BudgetType.Income && b.Type == BudgetType.Saving)
        //        .ToListAsync();

        //    if (budgetEntriesToDelete.Any())
        //    {
        //        // 2. Find all transactions linked to these budget entries.
        //        var budgetIdsToDelete = budgetEntriesToDelete.Select(b => b.Id).ToList();
        //        var transactionsToDelete = await _context.Transactions
        //            .Where(t => t.BudgetId.HasValue && budgetIdsToDelete.Contains(t.BudgetId.Value))
        //            .ToListAsync();

        //        // 3. Get the affected account ID before we delete anything.
        //        // (Assuming all transactions in a category come from one account for now)
        //        var accountIdToUpdate = transactionsToDelete.Select(t => t.AccountId).FirstOrDefault();

        //        // 4. Delete everything.
        //        _context.Transactions.RemoveRange(transactionsToDelete);
        //        _context.Budgets.RemoveRange(budgetEntriesToDelete);
        //        await _context.SaveChangesAsync();

        //        // 5. Rewrite history by recalculating the balance.
        //        if (accountIdToUpdate > 0)
        //        {
        //            await RecalculateAccountBalanceAsync(accountIdToUpdate);
        //            await _context.SaveChangesAsync();
        //        }
        //    }

        //    return RedirectToAction(nameof(Index), new { year = year });
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteCategory(string categoryName, BudgetType type, int year)
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);

        //    // Find all budget entries for this specific category, type, and year.
        //    var budgetEntriesToDelete = await _context.Budgets
        //        .Where(b => b.UserId == currentUser.Id &&
        //                    b.Year == year &&
        //                    b.CategoryName == categoryName &&
        //                    b.Type == type) // We respect the specific type (Income or Saving)
        //        .ToListAsync();

        //    if (budgetEntriesToDelete.Any())
        //    {
        //        var budgetIdsToDelete = budgetEntriesToDelete.Select(b => b.Id).ToList();
        //        var transactionsToDelete = await _context.Transactions
        //            .Where(t => t.BudgetId.HasValue && budgetIdsToDelete.Contains(t.BudgetId.Value))
        //            .ToListAsync();

        //        var accountIdToUpdate = transactionsToDelete.Select(t => t.AccountId).FirstOrDefault();

        //        _context.Transactions.RemoveRange(transactionsToDelete);
        //        _context.Budgets.RemoveRange(budgetEntriesToDelete);
        //        await _context.SaveChangesAsync();

        //        if (accountIdToUpdate > 0)
        //        {
        //            await RecalculateAccountBalanceAsync(accountIdToUpdate);
        //            await _context.SaveChangesAsync();
        //        }
        //    }

        //    return RedirectToAction(nameof(Index), new { year = year });
        //}
    }
}