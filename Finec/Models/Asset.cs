using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finec.Models
{
    /// <summary>
    /// Represents a user's asset (e.g., stocks, property).
    /// </summary>
    public class Asset
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string AssetName { get; set; }

        [Required]
        public AssetType Type { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal CurrentValue { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // This navigation property links an Asset to its value history.
        public virtual ICollection<AssetHistory> History { get; set; } = new List<AssetHistory>();
    }

    public enum AssetType
    {
        Stocks,
        MutualFund,
        Crypto,
        RealEstate,
        Vehicle,
        Commodities, // e.g., Gold, Silver
        Other
    }

    /// <summary>
    /// A new class to track the historical value of an asset over time.
    /// This is the data source for the Net Worth chart.
    /// </summary>
    public class AssetHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AssetId { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset Asset { get; set; }

        [Required]
        public DateTime DateRecorded { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Value { get; set; }
    }
}