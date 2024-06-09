using Microsoft.EntityFrameworkCore;
using CtrlWin.Models;

namespace CtrlWin.Data
{
    public class ClipboardDbContext : DbContext
    {
        public DbSet<TextItem> TextItems { get; set; }
        public DbSet<ImageItem> ImageItems { get; set; }
        public DbSet<VideoItem> VideoItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Ensure this path points to the actual location of your clipboard.db file
            optionsBuilder.UseSqlite("Data Source=C:\\Users\\dalde\\source\\repos\\CtrlWin\\CtrlWin\\clipboard.db");
        }
    }
}