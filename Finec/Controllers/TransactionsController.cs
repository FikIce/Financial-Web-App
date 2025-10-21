using Finec.Data;
using Finec.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Finec.Controllers
{
    [Authorize]
    public class TransactionsController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context)
        {
            _userManager = userManager;
        }

        // GET: Transactions
        public async Task<IActionResult> Index(string typeFilter, string monthFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Start with a query for all transactions for the user
            var transactionsQuery = _context.Transactions
                                        .Include(t => t.Account)
                                        .Include(t => t.Budget)
                                        .Where(t => t.UserId == currentUser.Id); //if we add to list async ww are fethcing the data from datbase directly, but if we dont that means we only stored them in the tracking system.

            // Apply the Type filter if one was provided
            if (!String.IsNullOrEmpty(typeFilter) && Enum.TryParse<TransactionType>(typeFilter, out var type)) // try parse is the method to safely convert to something.
            {
                transactionsQuery = transactionsQuery.Where(t => t.Type == type);
            }

            // Apply the Month filter if one was provided
            if (!String.IsNullOrEmpty(monthFilter) && DateTime.TryParse(monthFilter, out var date))
            {
                transactionsQuery = transactionsQuery.Where(t => t.TransactionDate.Year == date.Year && t.TransactionDate.Month == date.Month);
            }

            // Pass the current filters back to the View so the dropdowns can remember their state
            ViewBag.TypeFilter = typeFilter;
            ViewBag.MonthFilter = monthFilter;

            var transactions = await transactionsQuery.OrderByDescending(t => t.TransactionDate).ToListAsync();

            return View(transactions);
        }

        // GET: Transactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var transaction = await _context.Transactions
                                        .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUser.Id);

            if (transaction == null) return NotFound();

            // We need to populate the accounts dropdown, just like in the Create view
            await PopulateAccountsDropDownList(transaction.AccountId);
            return View(transaction);
        }

        // POST: Transactions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Type,Amount,Description,TransactionDate,AccountId,UserId")] Transaction transaction)
        {
            if (id != transaction.Id) return NotFound();

            // Security check: ensure the transaction being updated belongs to the current user.
            var currentUser = await _userManager.GetUserAsync(User);
            if (transaction.UserId != currentUser.Id) return Forbid();

            ModelState.Remove("User");
            ModelState.Remove("Account");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(transaction);
                    await _context.SaveChangesAsync();

                    // THE CRITICAL STEP: Recalculate the balance after the change.
                    await RecalculateAccountBalanceAsync(transaction.AccountId);
                    await _context.SaveChangesAsync(); // Save the balance update
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Transactions.Any(e => e.Id == transaction.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateAccountsDropDownList(transaction.AccountId);
            return View(transaction);
        }

        // We need to add the helper methods to this controller
        private async Task PopulateAccountsDropDownList(object selectedAccount = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var accountsQuery = _context.Accounts.Where(a => a.UserId == currentUser.Id).OrderBy(a => a.AccountName);
            ViewBag.AccountId = new SelectList(await accountsQuery.AsNoTracking().ToListAsync(), "Id", "AccountName", selectedAccount);
        }

        // GET: Transactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var transaction = await _context.Transactions
                .Include(t => t.Account) // Include account name for the confirmation page
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == currentUser.Id);

            if (transaction == null) return NotFound();

            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUser.Id);

            if (transaction != null)
            {
                int accountIdToUpdate = transaction.AccountId; // Store the ID before deleting
                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();

                // THE CRITICAL STEP: Recalculate the balance of the affected account
                await RecalculateAccountBalanceAsync(accountIdToUpdate);
                await _context.SaveChangesAsync(); // Save the balance update
            }

            return RedirectToAction(nameof(Index));
        }

        // We will add back Details, Edit, and Delete logic here later if needed.
        // For now, the primary function is to list all transactions.
    }
}