using KarzounERP.Models;
using Microsoft.EntityFrameworkCore;

namespace KarzounERP.Data;

public class AppDbContext : DbContext
{
    public DbSet<Company> Companies { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<SalesDocument> Documents { get; set; }
    public DbSet<SalesDocumentItem> DocumentItems { get; set; }
    public DbSet<CustomerNote> CustomerNotes { get; set; }
    public DbSet<CompanyLocalizedSetting> CompanyLocalizedSettings { get; set; }
    public DbSet<ProductLocalizedText> ProductLocalizedTexts { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // SQLite has no native decimal type; store as REAL so SUM/AVG aggregates work in SQL.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
        configurationBuilder.Properties<decimal?>().HaveConversion<double?>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.TaxRate).HasColumnType("decimal(18,4)");
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.Company)
             .WithMany(co => co.Customers)
             .HasForeignKey(c => c.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(c => new { c.CompanyId, c.FullName });
        });

        modelBuilder.Entity<CustomerNote>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasOne(n => n.Customer)
             .WithMany(c => c.NotesHistory)
             .HasForeignKey(n => n.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.Company)
             .WithMany(co => co.Products)
             .HasForeignKey(p => p.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(p => p.UnitPrice).HasColumnType("decimal(18,4)");
            e.Property(p => p.Weight).HasColumnType("decimal(18,4)");
        });

        modelBuilder.Entity<ProductLocalizedText>(e =>
        {
            e.HasKey(plt => plt.Id);
            e.HasOne(plt => plt.Product)
             .WithMany(p => p.LocalizedTexts)
             .HasForeignKey(plt => plt.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(plt => new { plt.ProductId, plt.LanguageCode }).IsUnique();
        });

        modelBuilder.Entity<SalesDocument>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasOne(d => d.Company)
             .WithMany(co => co.Documents)
             .HasForeignKey(d => d.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Customer)
             .WithMany(c => c.Documents)
             .HasForeignKey(d => d.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(d => d.DiscountAmount).HasColumnType("decimal(18,4)");
            e.Property(d => d.DiscountPercent).HasColumnType("decimal(18,4)");
            e.Property(d => d.TaxRate).HasColumnType("decimal(18,4)");
            e.Property(d => d.Subtotal).HasColumnType("decimal(18,4)");
            e.Property(d => d.TaxAmount).HasColumnType("decimal(18,4)");
            e.Property(d => d.GrandTotal).HasColumnType("decimal(18,4)");
            e.HasIndex(d => new { d.CompanyId, d.Type, d.Status });
        });

        modelBuilder.Entity<SalesDocumentItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasOne(i => i.Document)
             .WithMany(d => d.Items)
             .HasForeignKey(i => i.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,4)");
            e.Property(i => i.Weight).HasColumnType("decimal(18,4)");
            e.Property(i => i.LineTotal).HasColumnType("decimal(18,4)");
        });

        modelBuilder.Entity<CompanyLocalizedSetting>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.Company)
             .WithMany()
             .HasForeignKey(s => s.CompanyId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => new { s.CompanyId, s.LanguageCode }).IsUnique();
        });
    }
}
