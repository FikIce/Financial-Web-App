using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finec.Models
{
    /// <summary>
    /// Represents a user's financial plan for a category (Income, Saving, or Expense).
    /// </summary>
    public class Budget
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } // e.g., "Monthly Salary", "Groceries"

        [Required]
        public BudgetType Type { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PlannedAmount { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required]
        public int AccountId { get; set; }
        [ForeignKey("AccountId")]
        public virtual Account Account { get; set; }
        public int? AssetId { get; set; } // Nullable Foreign Key
        [ForeignKey("AssetId")]
        public virtual Asset Asset { get; set; }

        // Establishes the one-to-many relationship: One Budget category has many Transactions.
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public enum BudgetType { Income, Saving, Expense }
}