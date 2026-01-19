using System;
using System.Collections.Generic;
using System.Text;
using BestelAppBoeken.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BestelAppBoeken.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static void SeedData(BookstoreDbContext context)
        {
            // Zorg dat database bestaat
            context.Database.EnsureCreated();

            // Check of er al data is
            if (context.Books.Any() || context.Klanten.Any())
            {
                Console.WriteLine("ℹ️  Database is al geseed - bestaande data wordt behouden");
                return; // Database is al geseed
            }

            Console.WriteLine("🌱 Database seeding gestart...");

            // Seed Boeken - 20 populaire boeken met variërende voorraad (0-20)
            var books = new List<Book>
            {
                // Top 20 Klassiekers & Bestsellers
                new Book { Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Price = 12.99m, Isbn = "9780743273565", ImageUrl = "https://via.placeholder.com/150", Description = "A classic novel of the Jazz Age.", VoorraadAantal = 15 },
                new Book { Title = "1984", Author = "George Orwell", Price = 10.50m, Isbn = "9780451524935", ImageUrl = "https://via.placeholder.com/150", Description = "A dystopian social science fiction novel.", VoorraadAantal = 8 },
                new Book { Title = "To Kill a Mockingbird", Author = "Harper Lee", Price = 14.20m, Isbn = "9780061120084", ImageUrl = "https://via.placeholder.com/150", Description = "A novel about racial inequality and justice.", VoorraadAantal = 20 },
                new Book { Title = "Pride and Prejudice", Author = "Jane Austen", Price = 9.99m, Isbn = "9780141439518", ImageUrl = "https://via.placeholder.com/150", Description = "A romantic novel of manners.", VoorraadAantal = 3 },
                new Book { Title = "The Catcher in the Rye", Author = "J.D. Salinger", Price = 11.50m, Isbn = "9780316769488", ImageUrl = "https://via.placeholder.com/150", Description = "A story about teenage rebellion and alienation.", VoorraadAantal = 12 },
                
                // Fantasy & Adventure
                new Book { Title = "Harry Potter and the Philosopher's Stone", Author = "J.K. Rowling", Price = 15.99m, Isbn = "9780747532699", ImageUrl = "https://via.placeholder.com/150", Description = "The first book in the magical Harry Potter series.", VoorraadAantal = 18 },
                new Book { Title = "The Hobbit", Author = "J.R.R. Tolkien", Price = 13.50m, Isbn = "9780547928227", ImageUrl = "https://via.placeholder.com/150", Description = "A fantasy adventure about Bilbo Baggins.", VoorraadAantal = 14 },
                new Book { Title = "The Chronicles of Narnia", Author = "C.S. Lewis", Price = 22.99m, Isbn = "9780066238500", ImageUrl = "https://via.placeholder.com/150", Description = "A fantasy series about children in a magical land.", VoorraadAantal = 10 },
                
                // Thriller & Mystery
                new Book { Title = "The Da Vinci Code", Author = "Dan Brown", Price = 16.75m, Isbn = "9780307474278", ImageUrl = "https://via.placeholder.com/150", Description = "A mystery thriller involving secrets and symbols.", VoorraadAantal = 0 },
                new Book { Title = "The Silent Patient", Author = "Alex Michaelides", Price = 14.99m, Isbn = "9781250301697", ImageUrl = "https://via.placeholder.com/150", Description = "A psychological thriller about a woman's act of violence.", VoorraadAantal = 6 },
                
                // Contemporary & Literary
                new Book { Title = "The Alchemist", Author = "Paulo Coelho", Price = 12.50m, Isbn = "9780061122415", ImageUrl = "https://via.placeholder.com/150", Description = "A philosophical novel about following your dreams.", VoorraadAantal = 19 },
                new Book { Title = "Where the Crawdads Sing", Author = "Delia Owens", Price = 15.50m, Isbn = "9780735219090", ImageUrl = "https://via.placeholder.com/150", Description = "A mystery and coming-of-age story set in the marshes.", VoorraadAantal = 9 },
                new Book { Title = "The Midnight Library", Author = "Matt Haig", Price = 16.99m, Isbn = "9780525559474", ImageUrl = "https://via.placeholder.com/150", Description = "A novel about infinite possibilities and second chances.", VoorraadAantal = 13 },
                
                // Non-Fiction & Self-Help
                new Book { Title = "Sapiens: A Brief History of Humankind", Author = "Yuval Noah Harari", Price = 18.99m, Isbn = "9780062316110", ImageUrl = "https://via.placeholder.com/150", Description = "An exploration of the history and impact of Homo sapiens.", VoorraadAantal = 7 },
                new Book { Title = "Educated", Author = "Tara Westover", Price = 17.50m, Isbn = "9780399590504", ImageUrl = "https://via.placeholder.com/150", Description = "A memoir about education and family.", VoorraadAantal = 11 },
                new Book { Title = "Becoming", Author = "Michelle Obama", Price = 20.00m, Isbn = "9781524763138", ImageUrl = "https://via.placeholder.com/150", Description = "The memoir of the former First Lady.", VoorraadAantal = 16 },
                new Book { Title = "Atomic Habits", Author = "James Clear", Price = 19.99m, Isbn = "9780735211292", ImageUrl = "https://via.placeholder.com/150", Description = "An easy and proven way to build good habits.", VoorraadAantal = 20 },
                
                // Science Fiction
                new Book { Title = "Project Hail Mary", Author = "Andy Weir", Price = 18.50m, Isbn = "9780593135204", ImageUrl = "https://via.placeholder.com/150", Description = "A science fiction thriller about saving Earth.", VoorraadAantal = 10 },
                
                // Dystopian
                new Book { Title = "The Handmaid's Tale", Author = "Margaret Atwood", Price = 12.75m, Isbn = "9780385490818", ImageUrl = "https://via.placeholder.com/150", Description = "A dystopian novel about a totalitarian society.", VoorraadAantal = 11 },
                new Book { Title = "Brave New World", Author = "Aldous Huxley", Price = 11.99m, Isbn = "9780060850524", ImageUrl = "https://via.placeholder.com/150", Description = "A dystopian vision of the future.", VoorraadAantal = 0 }
            };

            context.Books.AddRange(books);
            Console.WriteLine($"📚 {books.Count} boeken toegevoegd");

            // Toon statistieken over voorraad
            var uitVoorraad = books.Count(b => b.VoorraadAantal == 0);
            var laagVoorraad = books.Count(b => b.VoorraadAantal > 0 && b.VoorraadAantal < 10);
            var normaalVoorraad = books.Count(b => b.VoorraadAantal >= 10);

            Console.WriteLine($"   ❌ Uit voorraad: {uitVoorraad} boeken");
            Console.WriteLine($"   ⚠️  Laag voorraad (<10): {laagVoorraad} boeken");
            Console.WriteLine($"   ✅ Normale voorraad (≥10): {normaalVoorraad} boeken");

            if (uitVoorraad > 0)
            {
                Console.WriteLine("\n🚨 Waarschuwing: Volgende boeken zijn UIT VOORRAAD:");
                foreach (var book in books.Where(b => b.VoorraadAantal == 0))
                {
                    Console.WriteLine($"   - {book.Title} (ISBN: {book.Isbn})");
                }
            }

            // Seed Klanten
            var klanten = new List<Klant>
            {
                new Klant
                {
                    Naam = "Jan Janssen",
                    Email = "jan@example.com",
                    Telefoon = "0612345678",
                    Adres = "Hoofdstraat 1, 1000 Brussel"
                },
                new Klant
                {
                    Naam = "Marie Peeters",
                    Email = "marie@example.com",
                    Telefoon = "0687654321",
                    Adres = "Kerkstraat 5, 2000 Antwerpen"
                },
                new Klant
                {
                    Naam = "Peter De Vries",
                    Email = "peter@example.com",
                    Telefoon = "0623456789",
                    Adres = "Stationsplein 10, 9000 Gent"
                }
            };

            context.Klanten.AddRange(klanten);

            // Sla alles op
            context.SaveChanges();

            Console.WriteLine("✅ Database succesvol geseed met testdata!");
        }
    }
}