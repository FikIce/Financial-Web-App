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
    public class ExpensesController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ExpensesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? year)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var selectedYear = year ?? DateTime.Now.Year;

            var budgets = await _context.Budgets
                .Include(b => b.Account)
                .Where(b => b.UserId == currentUser.Id && b.Year == selectedYear && b.Type == BudgetType.Expense)
                .ToListAsync();

            var viewModel = new ExpensesIndexViewModel { StartDate = new DateTime(selectedYear, 1, 1) };
            var groupedBudgets = budgets.GroupBy(b => new { b.CategoryName, b.AccountId, b.Account.AccountName });

            foreach (var group in groupedBudgets)
            {
                viewModel.ExpenseCategories.Add(new BudgetCategoryViewModel
                {
                    CategoryName = group.Key.CategoryName,
                    AccountId = group.Key.AccountId,
                    AccountName = group.Key.AccountName,
                    Type = BudgetType.Expense,
                    MonthlyAmounts = group.ToDictionary(item => item.Month, item => item.PlannedAmount)
                });
            }

            for (int month = 1; month <= 12; month++)
            {
                viewModel.MonthlyExpenseTotals[month] = viewModel.ExpenseCategories.Sum(c => c.MonthlyAmounts.ContainsKey(month) ? c.MonthlyAmounts[month] : 0);
            }

            ViewBag.AccountsList = new SelectList(await _context.Accounts.Where(a => a.UserId == currentUser.Id).ToListAsync(), "Id", "AccountName");
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string categoryName, int year, int accountId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (!await _context.Budgets.AnyAsync(b => b.UserId == currentUser.Id && b.Year == year && b.CategoryName == categoryName && b.Type == BudgetType.Expense && b.AccountId == accountId))
            {
                _context.Budgets.Add(new Budget { 
                    CategoryName = categoryName, 
                    Type = BudgetType.Expense, 
                    AccountId = accountId, 
                    PlannedAmount = 0, Month = 1, 
                    Year = year, 
                    UserId = currentUser.Id });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { year = year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(string categoryName, int year, int accountId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var budgetEntriesToDelete = await _context.Budgets
                .Where(b => b.UserId == currentUser.Id && b.Year == year && b.CategoryName == categoryName && b.Type == BudgetType.Expense && b.AccountId == accountId)
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
        public async Task<IActionResult> UpdateBudgetRow(string categoryName, int year, int accountId, Dictionary<int, decimal> monthlyAmounts)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            foreach (var monthAmount in monthlyAmounts)
            {
                var month = monthAmount.Key;
                var plannedAmount = monthAmount.Value;

                var budgetEntry = await _context.Budgets.FirstOrDefaultAsync(b =>
                    b.UserId == currentUser.Id && b.Year == year && b.Month == month &&
                    b.CategoryName == categoryName && b.Type == BudgetType.Expense && b.AccountId == accountId);

                if (budgetEntry == null)
                {
                    budgetEntry = new Budget { CategoryName = categoryName, Type = BudgetType.Expense, Month = month, Year = year, PlannedAmount = plannedAmount, UserId = currentUser.Id, AccountId = accountId };
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
                    var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.BudgetId == budgetEntry.Id);
                    if (transaction == null)
                    {
                        transaction = new Transaction { BudgetId = budgetEntry.Id, UserId = currentUser.Id };
                        _context.Transactions.Add(transaction);
                    }
                    transaction.Type = TransactionType.Expense;
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
            }

            await _context.SaveChangesAsync();
            await RecalculateAccountBalanceAsync(accountId);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { year = year });
        }

        // The Index and AddCategory methods are correct and do not need changes.
        //public async Task<IActionResult> Index(int? year)
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    var selectedYear = year ?? DateTime.Now.Year;

        //    var budgets = await _context.Budgets
        //        .Where(b => b.UserId == currentUser.Id && b.Year == selectedYear && b.Type == BudgetType.Expense)
        //        .ToListAsync();

        //    var viewModel = new ExpensesIndexViewModel
        //    {
        //        StartDate = new DateTime(selectedYear, 1, 1)
        //    };

        //    var groupedBudgets = budgets.GroupBy(b => b.CategoryName);

        //    foreach (var group in groupedBudgets)
        //    {
        //        viewModel.ExpenseCategories.Add(new BudgetCategoryViewModel
        //        {
        //            CategoryName = group.Key,
        //            Type = BudgetType.Expense,
        //            MonthlyAmounts = group.ToDictionary(item => item.Month, item => item.PlannedAmount)
        //        });
        //    }

        //    for (int month = 1; month <= 12; month++)
        //    {
        //        viewModel.MonthlyExpenseTotals[month] = viewModel.ExpenseCategories.Sum(c => c.MonthlyAmounts.ContainsKey(month) ? c.MonthlyAmounts[month] : 0);
        //    }

        //    return View(viewModel);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddCategory(string categoryName, int year)
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    if (!await _context.Budgets.AnyAsync(b => b.UserId == currentUser.Id && b.Year == year && b.CategoryName == categoryName && b.Type == BudgetType.Expense))
        //    {
        //        _context.Budgets.Add(new Budget { CategoryName = categoryName, Type = BudgetType.Expense, PlannedAmount = 0, Month = 1, Year = year, UserId = currentUser.Id });
        //        await _context.SaveChangesAsync();
        //    }
        //    return RedirectToAction(nameof(Index), new { year = year });
        //}

        //// The DeleteCategory method from our previous step is also correct.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteCategory(string categoryName, int year)
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    var budgetEntriesToDelete = await _context.Budgets
        //        .Where(b => b.UserId == currentUser.Id && b.Year == year && b.CategoryName == categoryName && b.Type == BudgetType.Expense)
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


        //// THIS IS THE DEFINITIVE, CORRECTED METHOD
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> UpdateBudgetRow(string categoryName, int year, Dictionary<int, decimal> monthlyAmounts)
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    var userAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == currentUser.Id);
        //    if (userAccount == null)
        //    {
        //        TempData["ErrorMessage"] = "Please add an account before logging expenses.";
        //        return RedirectToAction("Index", "Accounts");
        //    }

        //    foreach (var monthAmount in monthlyAmounts)
        //    {
        //        var month = monthAmount.Key;
        //        var plannedAmount = monthAmount.Value;

        //        var budgetEntry = await _context.Budgets.FirstOrDefaultAsync(b =>
        //            b.UserId == currentUser.Id && b.Year == year && b.Month == month &&
        //            b.CategoryName == categoryName && b.Type == BudgetType.Expense);

        //        if (budgetEntry == null)
        //        {
        //            budgetEntry = new Budget { CategoryName = categoryName, Type = BudgetType.Expense, Month = month, Year = year, PlannedAmount = plannedAmount, UserId = currentUser.Id };
        //            _context.Budgets.Add(budgetEntry);
        //        }
        //        else
        //        {
        //            budgetEntry.PlannedAmount = plannedAmount;
        //        }
        //        await _context.SaveChangesAsync(); // Save budget to ensure it has an ID

        //        // THE CRITICAL FIX: The check for "budgetDate <= DateTime.Now" is GONE.
        //        if (plannedAmount > 0)
        //        {
        //            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.BudgetId == budgetEntry.Id);
        //            if (transaction == null)
        //            {
        //                transaction = new Transaction { BudgetId = budgetEntry.Id, UserId = currentUser.Id };
        //                _context.Transactions.Add(transaction);
        //            }
        //            transaction.Type = TransactionType.Expense;
        //            transaction.Amount = plannedAmount;
        //            transaction.Description = $"Budget Entry: {categoryName}";
        //            transaction.TransactionDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        //            transaction.AccountId = userAccount.Id;
        //        }
        //        else // If planned amount is 0, remove any existing transaction.
        //        {
        //            var transactionToDelete = await _context.Transactions.FirstOrDefaultAsync(t => t.BudgetId == budgetEntry.Id);
        //            if (transactionToDelete != null)
        //            {
        //                _context.Transactions.Remove(transactionToDelete);
        //            }
        //        }
        //    }

        //    await _context.SaveChangesAsync(); // Save all transaction changes
        //    await RecalculateAccountBalanceAsync(userAccount.Id); // Recalculate the balance
        //    await _context.SaveChangesAsync(); // Save the new balance

        //    return RedirectToAction(nameof(Index), new { year = year });
        //}
    }
}