using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finec.Models
{
    /// <summary>
    /// Represents a financial account, like a bank account or e-wallet.
    /// It holds a balance and is the source/destination for transactions.
    /// </summary>
    public class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string AccountName { get; set; } // e.g., "BCA Debit", "Bank Jago"

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal CurrentBalance { get; set; }

        // Foreign key to the owner
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // Establishes the one-to-many relationship: One Account has many Transactions.
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}