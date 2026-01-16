using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BestelAppBoeken.Infrastructure.Data
{
    public class BookstoreDbContextFactory : IDesignTimeDbContextFactory<BookstoreDbContext>
    {
        public BookstoreDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BookstoreDbContext>();
            optionsBuilder.UseSqlite("Data Source=bookstore.db");

            return new BookstoreDbContext(optionsBuilder.Options);
        }
    }
}
