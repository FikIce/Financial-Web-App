using Finec.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Finec.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetHistory> AssetHistories { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // THIS IS THE FINAL AND CORRECT SET OF RULES.
            // IT ESTABLISHES A SINGLE, UNAMBIGUOUS CHAIN OF COMMAND FOR DELETIONS.

            // RULE #1: ESTABLISH THE PRIMARY OWNER.
            // The User is the ultimate owner. Deleting a User will cascade to their Accounts and Assets.
            builder.Entity<ApplicationUser>()
                .HasMany(u => u.Accounts)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ApplicationUser>()
                .HasMany(u => u.Assets)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // RULE #2: THE ACCOUNT IS THE FINANCIAL OWNER.
            // If an Account is deleted, its entire financial history (Transactions)
            // AND all of its plans (Budgets) must be destroyed with it.
            // This is the single, authoritative path for cascading deletes of financial data.
            builder.Entity<Transaction>()
                .HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Budget>()
                .HasOne(b => b.Account)
                .WithMany() // An account doesn't need a direct list of budgets
                .HasForeignKey(b => b.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // RULE #3: SEVER ALL OTHER CONFLICTING PATHS.
            // All other relationships are for context ONLY. They MUST NOT cascade deletes.
            // Their actions would conflict with the primary path established in Rule #2.

            // This severs the User -> Budget path.
            builder.Entity<Budget>()
                .HasOne(b => b.User)
                .WithMany(u => u.Budgets)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // This severs the User -> Transaction path.
            builder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // THIS IS THE SPECIFIC FIX for the FK_Transactions_Budgets_BudgetId error.
            // We are explicitly telling the database that if a Budget is deleted,
            // it should take NO ACTION on the related Transaction. The Transaction's deletion
            // will be handled by the master Account->Transaction rule. This resolves the conflict.
            builder.Entity<Transaction>()
                .HasOne(t => t.Budget)
                .WithMany(b => b.Transactions)
                .HasForeignKey(t => t.BudgetId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}