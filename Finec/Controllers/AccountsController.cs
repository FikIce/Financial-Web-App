using Finec.Data;
using Finec.Models;
using Finec.ViewModels;
using Microsoft.AspNetCore.Authorization; // Required for authorization
using Microsoft.AspNetCore.Identity; // Required for UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Finec.Controllers
{
    [Authorize] // This is a critical attribute. It ensures only logged-in users can access this controller.
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // Service to get the current user

        // We inject both the DbContext and the UserManager
        public AccountsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Accounts
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var accounts = await _context.Accounts
                                         .Where(a => a.UserId == currentUser.Id)
                                         .ToListAsync();

            // Create an instance of our new ViewModel
            var viewModel = new AccountsIndexViewModel
            {
                Accounts = accounts,
                TotalBalance = accounts.Sum(a => a.CurrentBalance)
            };

            return View(viewModel);
        }

        // GET: Accounts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // ALSO FIXED: Ensure the user can only see details of their own accounts.
            var account = await _context.Accounts
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == currentUser.Id);

            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // GET: Accounts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Accounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AccountName,CurrentBalance")] Account account)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // We manually set the UserId before saving.
            account.UserId = currentUser.Id;

            // THE FIX: We must remove both the User and UserId from ModelState validation
            // because they are not coming from the form.
            ModelState.Remove("User");
            ModelState.Remove("UserId"); // This was the missing line.

            if (ModelState.IsValid)
            {
                _context.Add(account);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // If we get here, something failed, redisplay form.
            return View(account);
        }

        // GET: Accounts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            // ALSO FIXED: Ensure the user can only get the edit page for their own accounts.
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == currentUser.Id);

            if (account == null)
            {
                return NotFound();
            }
            return View(account);
        }

        // POST: Accounts/Edit/5
        // POST: Accounts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AccountName,CurrentBalance")] Account account)
        {
            if (id != account.Id)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // THE FIX: First, fetch the original, trusted entity from the database.
            // This query also ensures the account belongs to the current user.
            var accountToUpdate = await _context.Accounts
                                        .FirstOrDefaultAsync(a => a.Id == id && a.UserId == currentUser.Id);

            if (accountToUpdate == null)
            {
                // If we can't find the account, it either doesn't exist or the user
                // is trying to access someone else's data.
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Now, update the properties of the entity we fetched from the database
                    // with the new values from the form.
                    accountToUpdate.AccountName = account.AccountName;
                    accountToUpdate.CurrentBalance = account.CurrentBalance;

                    _context.Update(accountToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AccountExists(account.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            // If model state is invalid, return the view with the data we fetched
            return View(accountToUpdate);
        }

        // GET: Accounts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var currentUser = await _userManager.GetUserAsync(User);
            // ALSO FIXED: Ensure the user can only get the delete page for their own accounts.
            var account = await _context.Accounts
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == currentUser.Id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // POST: Accounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            // ALSO FIXED: Ensure the user can only delete their own accounts.
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == currentUser.Id);
            if (account != null)
            {
                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AccountExists(int id)
        {
            return _context.Accounts.Any(e => e.Id == id);
        }
    }
}