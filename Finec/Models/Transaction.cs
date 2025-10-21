using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finec.Models
{
    /// <summary>
    /// Represents a single financial event. This is the most fundamental data point.
    /// </summary>
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // Foreign key to the Account for this transaction
        [Required]
        public int AccountId { get; set; }

        [ForeignKey("AccountId")]
        public virtual Account Account { get; set; }

        // This nullable foreign key links a transaction to a specific budget category.
        // This is the key to tracking spending against a budget.
        public int? BudgetId { get; set; }

        [ForeignKey("BudgetId")]
        public virtual Budget Budget { get; set; }
    }

    public enum TransactionType { Income, Expense, Saving, Transfer, AssetPurchase }
}