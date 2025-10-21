using Finec.Data;
using Finec.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Finec.Controllers
{
    // This is a base class and not meant to be accessed directly via a URL.
    public abstract class BaseController : Controller
    {
        // It holds the logic that is common to other controllers.
        protected readonly ApplicationDbContext _context;

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // The ONE, TRUE version of our balance calculation engine.
        protected async Task RecalculateAccountBalanceAsync(int accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return;

            var transactions = await _context.Transactions
                                             .Where(t => t.AccountId == accountId)
                                             .ToListAsync();

            decimal newBalance = 0;
            foreach (var t in transactions)
            {
                if (t.Type == TransactionType.Income)
                {
                    newBalance += t.Amount;
                }
                else // Any other type is money leaving the account
                {
                    newBalance -= t.Amount;
                }
            }

            account.CurrentBalance = newBalance;
            _context.Update(account);
        }
    }
}