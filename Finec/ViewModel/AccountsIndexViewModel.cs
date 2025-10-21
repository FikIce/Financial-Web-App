using Finec.Models;
using System.Collections.Generic;

namespace Finec.ViewModels
{
    public class AccountsIndexViewModel
    {
        // This will hold the list of accounts to display
        public IEnumerable<Account> Accounts { get; set; }

        // This will hold the calculated total balance
        public decimal TotalBalance { get; set; }
    }
}