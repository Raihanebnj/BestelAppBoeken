using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using BestelAppBoeken.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class BookService : IBookService
    {
        private readonly BookstoreDbContext _context;

        public BookService(BookstoreDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Book> GetAllBooks()
        {
            return _context.Books.ToList();
        }

        public Book? GetBookById(int id)
        {
            return _context.Books.FirstOrDefault(b => b.Id == id);
        }

        public Book CreateBook(Book book)
        {
            _context.Books.Add(book);
            _context.SaveChanges();
            return book;
        }

        public Book? UpdateBook(int id, Book book)
        {
            var existingBook = _context.Books.FirstOrDefault(b => b.Id == id);
            if (existingBook == null) return null;

            existingBook.Title = book.Title;
            existingBook.Author = book.Author;
            existingBook.Price = book.Price;
            existingBook.Isbn = book.Isbn;
            existingBook.Description = book.Description;
            existingBook.ImageUrl = book.ImageUrl;
            existingBook.VoorraadAantal = book.VoorraadAantal;

            _context.SaveChanges();
            return existingBook;
        }

        public bool DeleteBook(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null) return false;

            _context.Books.Remove(book);
            _context.SaveChanges();
            return true;
        }

        public IEnumerable<Book> SearchBooks(string query)
        {
            query = query.ToLower();
            return _context.Books
                .Where(b => b.Title.ToLower().Contains(query) ||
                           b.Author.ToLower().Contains(query) ||
                           b.Isbn.Contains(query))
                .ToList();
        }

        // Voorraad toevoegen/bijbestellen functionaliteit
        public bool AddStock(int bookId, int amount)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == bookId);
            if (book == null || amount <= 0) return false;

            book.VoorraadAantal += amount;
            _context.SaveChanges();
            return true;
        }

        // Voorraad verminderen (bij bestelling)
        public bool ReduceStock(int bookId, int amount)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == bookId);
            if (book == null || amount <= 0 || book.VoorraadAantal < amount) return false;

            book.VoorraadAantal -= amount;
            _context.SaveChanges();
            return true;
        }
    }
}
