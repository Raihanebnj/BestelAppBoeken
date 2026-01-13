using BestelAppBoeken.Core.Models;
using System.Collections.Generic;

namespace BestelAppBoeken.Core.Interfaces
{
    public interface IBookService
    {
        IEnumerable<Book> GetAllBooks();
        Book? GetBookById(int id);
    }
}
