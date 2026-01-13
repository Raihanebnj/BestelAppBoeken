using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class BookService : IBookService
    {
        private static readonly List<Book> _books = new List<Book>
        {
            new Book { Id = 1, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Price = 12.99m, Isbn = "9780743273565", ImageUrl = "https://via.placeholder.com/150", Description = "A classic novel of the Jazz Age." },
            new Book { Id = 2, Title = "1984", Author = "George Orwell", Price = 10.50m, Isbn = "9780451524935", ImageUrl = "https://via.placeholder.com/150", Description = "A dystopian social science fiction novel." },
            new Book { Id = 3, Title = "To Kill a Mockingbird", Author = "Harper Lee", Price = 14.20m, Isbn = "9780061120084", ImageUrl = "https://via.placeholder.com/150", Description = "A novel about the serious issues of rape and racial inequality." }
        };

        public IEnumerable<Book> GetAllBooks()
        {
            return _books;
        }

        public Book? GetBookById(int id)
        {
            return _books.FirstOrDefault(b => b.Id == id);
        }
    }
}
