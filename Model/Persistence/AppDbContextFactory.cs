using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlashMemo.Model.Persistence;

public class AppDbContextFactory: IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=flashmemo.db") // TO DO: change this to dynamic path getting to target AppData\Roaming\FlashMemo on Windows
            .Options;

        return new AppDbContext(options);
    }
}
