using Finec.Data;
using Finec.Models;
using Finec.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Finec.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context)
        {
            _userManager = userManager;
        }

        //public async Task<IActionResult> Index(int? year, int? month)
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    var selectedDate = new DateTime(year ?? DateTime.Now.Year, month ?? DateTime.Now.Month, 1);
        //    var previousMonthDate = selectedDate.AddMonths(-1);

        //    // Fetch transactions for the selected month
        //    var selectedMonthTransactions = await _context.Transactions
        //        .Include(t => t.Budget)
        //        .Where(t => t.UserId == currentUser.Id && t.TransactionDate.Year == selectedDate.Year && t.TransactionDate.Month == selectedDate.Month)
        //        .ToListAsync();

        //    // Fetch transactions for the previous month for comparison
        //    var previousMonthTransactions = await _context.Transactions
        //        .Where(t => t.UserId == currentUser.Id && t.TransactionDate.Year == previousMonthDate.Year && t.TransactionDate.Month == previousMonthDate.Month)
        //        .ToListAsync();

        //    var accounts = await _context.Accounts.Where(a => a.UserId == currentUser.Id).OrderBy(a => a.AccountName).ToListAsync();

        //    // Calculate totals for cards
        //    var totalIncome = selectedMonthTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        //    var totalSpending = selectedMonthTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        //    var totalSaving = selectedMonthTransactions.Where(t => t.Type == TransactionType.Saving).Sum(t => t.Amount);
        //    var prevTotalIncome = previousMonthTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        //    var prevTotalSpending = previousMonthTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        //    var prevTotalSaving = previousMonthTransactions.Where(t => t.Type == TransactionType.Saving).Sum(t => t.Amount);

        //    // Prepare data for all three line charts
        //    var dailyExpenses = selectedMonthTransactions.Where(t => t.Type == TransactionType.Expense).GroupBy(t => t.TransactionDate.Day).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        //    var dailyIncome = selectedMonthTransactions.Where(t => t.Type == TransactionType.Income).GroupBy(t => t.TransactionDate.Day).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        //    var dailySavings = selectedMonthTransactions.Where(t => t.Type == TransactionType.Saving).GroupBy(t => t.TransactionDate.Day).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        //    var chartLabels = new List<string>();
        //    var expenseChartData = new List<decimal>();
        //    var incomeChartData = new List<decimal>();
        //    var savingChartData = new List<decimal>();

        //    int daysInMonth = DateTime.DaysInMonth(selectedDate.Year, selectedDate.Month);
        //    for (int day = 1; day <= daysInMonth; day++)
        //    {
        //        chartLabels.Add(day.ToString("D2"));
        //        expenseChartData.Add(dailyExpenses.ContainsKey(day) ? dailyExpenses[day] : 0);
        //        incomeChartData.Add(dailyIncome.ContainsKey(day) ? dailyIncome[day] : 0);
        //        savingChartData.Add(dailySavings.ContainsKey(day) ? dailySavings[day] : 0);
        //    }

        //    // Prepare data for the income doughnut chart
        //    var incomeBreakdown = selectedMonthTransactions
        //        .Where(t => t.Type == TransactionType.Income)
        //        .GroupBy(t => t.Budget?.CategoryName ?? "Other Income")
        //        .Select(g => new IncomeBreakdownItem { SourceName = g.Key, Amount = g.Sum(t => t.Amount) }).ToList();

        //    // Assemble the final ViewModel
        //    var viewModel = new DashboardViewModel
        //    {
        //        TotalIncome = totalIncome,
        //        TotalSpending = totalSpending,
        //        TotalSaving = totalSaving,
        //        IncomeChangePercentage = prevTotalIncome == 0 ? 0 : ((double)(totalIncome - prevTotalIncome) / (double)prevTotalIncome),
        //        SpendingChangePercentage = prevTotalSpending == 0 ? 0 : ((double)(totalSpending - prevTotalSpending) / (double)prevTotalSpending),
        //        SavingChangePercentage = prevTotalSaving == 0 ? 0 : ((double)(totalSaving - prevTotalSaving) / (double)prevTotalSaving),
        //        ChartLabels = chartLabels,
        //        ExpenseChartData = expenseChartData,
        //        IncomeChartData = incomeChartData,
        //        SavingChartData = savingChartData,
        //        IncomeBreakdown = incomeBreakdown,
        //        Accounts = accounts
        //    };

        //    ViewBag.SelectedDate = selectedDate;
        //    return View(viewModel);
        //}

        public async Task<IActionResult> Index(int? year, int? month)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var selectedDate = new DateTime(year ?? DateTime.Now.Year, month ?? DateTime.Now.Month, 1);
            var previousMonthDate = selectedDate.AddMonths(-1);

            var selectedMonthTransactions = await _context.Transactions
                .Include(t => t.Budget)
                .Where(t => t.UserId == currentUser.Id && t.TransactionDate.Year == selectedDate.Year && t.TransactionDate.Month == selectedDate.Month)
                .ToListAsync();

            var previousMonthTransactions = await _context.Transactions
                .Where(t => t.UserId == currentUser.Id && t.TransactionDate.Year == previousMonthDate.Year && t.TransactionDate.Month == previousMonthDate.Month)
                .ToListAsync();

            var accounts = await _context.Accounts.Where(a => a.UserId == currentUser.Id).OrderBy(a => a.AccountName).ToListAsync();

            // Calculate Totals
            var totalIncome = selectedMonthTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var totalSpending = selectedMonthTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var totalSaving = selectedMonthTransactions.Where(t => t.Type == TransactionType.Saving).Sum(t => t.Amount);
            var prevTotalIncome = previousMonthTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var prevTotalSpending = previousMonthTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var prevTotalSaving = previousMonthTransactions.Where(t => t.Type == TransactionType.Saving).Sum(t => t.Amount);

            // Calculate Intelligent Percentage Changes and Messages
            (double incomeChangePercentage, string incomeChangeMessage) = CalculateChange(totalIncome, prevTotalIncome);
            (double spendingChangePercentage, string spendingChangeMessage) = CalculateChange(totalSpending, prevTotalSpending);
            (double savingChangePercentage, string savingChangeMessage) = CalculateChange(totalSaving, prevTotalSaving);

            // Prepare Chart Data
            var dailyExpenses = selectedMonthTransactions.Where(t => t.Type == TransactionType.Expense).GroupBy(t => t.TransactionDate.Day).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
            var dailyIncome = selectedMonthTransactions.Where(t => t.Type == TransactionType.Income).GroupBy(t => t.TransactionDate.Day).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
            var dailySavings = selectedMonthTransactions.Where(t => t.Type == TransactionType.Saving).GroupBy(t => t.TransactionDate.Day).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            var chartLabels = new List<string>();
            var expenseChartData = new List<decimal>();
            var incomeChartData = new List<decimal>();
            var savingChartData = new List<decimal>();
            int daysInMonth = DateTime.DaysInMonth(selectedDate.Year, selectedDate.Month);
            for (int day = 1; day <= daysInMonth; day++)
            {
                chartLabels.Add(day.ToString("D2"));
                expenseChartData.Add(dailyExpenses.ContainsKey(day) ? dailyExpenses[day] : 0);
                incomeChartData.Add(dailyIncome.ContainsKey(day) ? dailyIncome[day] : 0);
                savingChartData.Add(dailySavings.ContainsKey(day) ? dailySavings[day] : 0);
            }

            var incomeBreakdown = selectedMonthTransactions
                .Where(t => t.Type == TransactionType.Income)
                .GroupBy(t => t.Budget?.CategoryName ?? "Other Income")
                .Select(g => new IncomeBreakdownItem { SourceName = g.Key, Amount = g.Sum(t => t.Amount) }).ToList();

            // Assemble the final ViewModel
            var viewModel = new DashboardViewModel
            {
                TotalIncome = totalIncome,
                TotalSpending = totalSpending,
                TotalSaving = totalSaving,
                IncomeChangePercentage = incomeChangePercentage,
                IncomeChangeMessage = incomeChangeMessage,
                SpendingChangePercentage = spendingChangePercentage,
                SpendingChangeMessage = spendingChangeMessage,
                SavingChangePercentage = savingChangePercentage,
                SavingChangeMessage = savingChangeMessage,
                ChartLabels = chartLabels,
                ExpenseChartData = expenseChartData,
                IncomeChartData = incomeChartData,
                SavingChartData = savingChartData,
                IncomeBreakdown = incomeBreakdown,
                Accounts = accounts
            };

            ViewBag.SelectedDate = selectedDate;
            return View(viewModel);
        }

        // Helper method for clean, reusable calculation logic
        private (double, string) CalculateChange(decimal current, decimal previous)
        {
            if (previous > 0)
            {
                return ((double)((current - previous) / previous), "vs. previous period");
            }
            if (previous == 0 && current > 0)
            {
                return (1, "new this month"); // Represent as +100%
            }
            return (0, "vs. previous period");
        }
    }
}