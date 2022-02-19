using Microsoft.EntityFrameworkCore;
using WinTenDev.Zizi.Models.Tables;

namespace WinTenDev.Zizi.DbMigrations.EfMigrations;

public class BlockListContext : DbContext
{
    public DbSet<BlockList> BlockLists { get; set; }

    public BlockListContext(DbContextOptions<BlockListContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlockList>().ToTable("BlockLists")
            .HasKey(list => list.Id);

        // base.OnModelCreating(modelBuilder);
    }
}