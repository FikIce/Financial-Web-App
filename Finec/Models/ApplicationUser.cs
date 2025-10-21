using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Finec.Models
{
    /// <summary>
    /// Represents a user. Inherits from IdentityUser to get standard authentication
    /// properties and allows us to add custom ones. It will own all other data.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // Navigation properties to easily access all data for a user
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }
        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}