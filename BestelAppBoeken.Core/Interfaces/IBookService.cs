using BestelAppBoeken.Core.Models;

namespace BestelAppBoeken.Core.Interfaces
{
    public interface IBookService
    {
        IEnumerable<Book> GetAllBooks();
        Book? GetBookById(int id);
        Book CreateBook(Book book);
        Book? UpdateBook(int id, Book book);
        bool DeleteBook(int id);
        IEnumerable<Book> SearchBooks(string query);
        bool AddStock(int bookId, int amount);
        bool ReduceStock(int bookId, int amount);
    }
}
