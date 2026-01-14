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

            // Seed Boeken - 50 boeken met variërende voorraad (0-20)
            var books = new List<Book>
            {
                new Book { Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Price = 12.99m, Isbn = "9780743273565", ImageUrl = "https://via.placeholder.com/150", Description = "A classic novel of the Jazz Age.", VoorraadAantal = 15 },
                new Book { Title = "1984", Author = "George Orwell", Price = 10.50m, Isbn = "9780451524935", ImageUrl = "https://via.placeholder.com/150", Description = "A dystopian social science fiction novel.", VoorraadAantal = 8 },
                new Book { Title = "To Kill a Mockingbird", Author = "Harper Lee", Price = 14.20m, Isbn = "9780061120084", ImageUrl = "https://via.placeholder.com/150", Description = "A novel about racial inequality and justice.", VoorraadAantal = 20 },
                new Book { Title = "Pride and Prejudice", Author = "Jane Austen", Price = 9.99m, Isbn = "9780141439518", ImageUrl = "https://via.placeholder.com/150", Description = "A romantic novel of manners.", VoorraadAantal = 3 },
                new Book { Title = "The Catcher in the Rye", Author = "J.D. Salinger", Price = 11.50m, Isbn = "9780316769488", ImageUrl = "https://via.placeholder.com/150", Description = "A story about teenage rebellion and alienation.", VoorraadAantal = 12 },
                new Book { Title = "Harry Potter and the Philosopher's Stone", Author = "J.K. Rowling", Price = 15.99m, Isbn = "9780747532699", ImageUrl = "https://via.placeholder.com/150", Description = "The first book in the magical Harry Potter series.", VoorraadAantal = 18 },
                new Book { Title = "The Hobbit", Author = "J.R.R. Tolkien", Price = 13.50m, Isbn = "9780547928227", ImageUrl = "https://via.placeholder.com/150", Description = "A fantasy adventure about Bilbo Baggins.", VoorraadAantal = 14 },
                new Book { Title = "The Da Vinci Code", Author = "Dan Brown", Price = 16.75m, Isbn = "9780307474278", ImageUrl = "https://via.placeholder.com/150", Description = "A mystery thriller involving secrets and symbols.", VoorraadAantal = 0 },
                new Book { Title = "The Alchemist", Author = "Paulo Coelho", Price = 12.50m, Isbn = "9780061122415", ImageUrl = "https://via.placeholder.com/150", Description = "A philosophical novel about following your dreams.", VoorraadAantal = 19 },
                new Book { Title = "Sapiens: A Brief History of Humankind", Author = "Yuval Noah Harari", Price = 18.99m, Isbn = "9780062316110", ImageUrl = "https://via.placeholder.com/150", Description = "An exploration of the history and impact of Homo sapiens.", VoorraadAantal = 7 },
                
                new Book { Title = "Educated", Author = "Tara Westover", Price = 17.50m, Isbn = "9780399590504", ImageUrl = "https://via.placeholder.com/150", Description = "A memoir about education and family.", VoorraadAantal = 11 },
                new Book { Title = "Becoming", Author = "Michelle Obama", Price = 20.00m, Isbn = "9781524763138", ImageUrl = "https://via.placeholder.com/150", Description = "The memoir of the former First Lady.", VoorraadAantal = 16 },
                new Book { Title = "The Silent Patient", Author = "Alex Michaelides", Price = 14.99m, Isbn = "9781250301697", ImageUrl = "https://via.placeholder.com/150", Description = "A psychological thriller about a woman's act of violence.", VoorraadAantal = 0 },
                new Book { Title = "Where the Crawdads Sing", Author = "Delia Owens", Price = 15.50m, Isbn = "9780735219090", ImageUrl = "https://via.placeholder.com/150", Description = "A mystery and coming-of-age story set in the marshes.", VoorraadAantal = 9 },
                new Book { Title = "The Midnight Library", Author = "Matt Haig", Price = 16.99m, Isbn = "9780525559474", ImageUrl = "https://via.placeholder.com/150", Description = "A novel about infinite possibilities and second chances.", VoorraadAantal = 13 },
                new Book { Title = "Atomic Habits", Author = "James Clear", Price = 19.99m, Isbn = "9780735211292", ImageUrl = "https://via.placeholder.com/150", Description = "An easy and proven way to build good habits.", VoorraadAantal = 20 },
                new Book { Title = "The Four Winds", Author = "Kristin Hannah", Price = 17.99m, Isbn = "9781250178602", ImageUrl = "https://via.placeholder.com/150", Description = "A novel about the Great Depression era.", VoorraadAantal = 5 },
                new Book { Title = "Project Hail Mary", Author = "Andy Weir", Price = 18.50m, Isbn = "9780593135204", ImageUrl = "https://via.placeholder.com/150", Description = "A science fiction thriller about saving Earth.", VoorraadAantal = 10 },
                new Book { Title = "The Seven Husbands of Evelyn Hugo", Author = "Taylor Jenkins Reid", Price = 16.00m, Isbn = "9781501161933", ImageUrl = "https://via.placeholder.com/150", Description = "A story of a Hollywood icon's scandalous life.", VoorraadAantal = 17 },
                new Book { Title = "Circe", Author = "Madeline Miller", Price = 15.75m, Isbn = "9780316556347", ImageUrl = "https://via.placeholder.com/150", Description = "A retelling of Greek mythology from Circe's perspective.", VoorraadAantal = 6 },
                
                new Book { Title = "Normal People", Author = "Sally Rooney", Price = 14.50m, Isbn = "9781984822178", ImageUrl = "https://via.placeholder.com/150", Description = "A story of friendship and love between two people.", VoorraadAantal = 0 },
                new Book { Title = "The Book Thief", Author = "Markus Zusak", Price = 13.99m, Isbn = "9780375842207", ImageUrl = "https://via.placeholder.com/150", Description = "A story of a young girl living in Nazi Germany.", VoorraadAantal = 12 },
                new Book { Title = "Life of Pi", Author = "Yann Martel", Price = 14.25m, Isbn = "9780156027328", ImageUrl = "https://via.placeholder.com/150", Description = "A boy's survival story at sea with a tiger.", VoorraadAantal = 8 },
                new Book { Title = "The Kite Runner", Author = "Khaled Hosseini", Price = 13.50m, Isbn = "9781594631931", ImageUrl = "https://via.placeholder.com/150", Description = "A story of friendship and redemption in Afghanistan.", VoorraadAantal = 15 },
                new Book { Title = "A Thousand Splendid Suns", Author = "Khaled Hosseini", Price = 14.00m, Isbn = "9781594483851", ImageUrl = "https://via.placeholder.com/150", Description = "Two women's intertwined lives in Afghanistan.", VoorraadAantal = 4 },
                new Book { Title = "The Handmaid's Tale", Author = "Margaret Atwood", Price = 12.75m, Isbn = "9780385490818", ImageUrl = "https://via.placeholder.com/150", Description = "A dystopian novel about a totalitarian society.", VoorraadAantal = 11 },
                new Book { Title = "The Road", Author = "Cormac McCarthy", Price = 13.25m, Isbn = "9780307387899", ImageUrl = "https://via.placeholder.com/150", Description = "A post-apocalyptic journey of a father and son.", VoorraadAantal = 0 },
                new Book { Title = "Brave New World", Author = "Aldous Huxley", Price = 11.99m, Isbn = "9780060850524", ImageUrl = "https://via.placeholder.com/150", Description = "A dystopian vision of the future.", VoorraadAantal = 16 },
                new Book { Title = "The Giver", Author = "Lois Lowry", Price = 10.99m, Isbn = "9780544336261", ImageUrl = "https://via.placeholder.com/150", Description = "A young boy discovers the dark secrets of his society.", VoorraadAantal = 19 },
                new Book { Title = "Fahrenheit 451", Author = "Ray Bradbury", Price = 11.50m, Isbn = "9781451673319", ImageUrl = "https://via.placeholder.com/150", Description = "A dystopian novel about book burning.", VoorraadAantal = 7 },
                
                new Book { Title = "The Outsiders", Author = "S.E. Hinton", Price = 9.99m, Isbn = "9780142407332", ImageUrl = "https://via.placeholder.com/150", Description = "A story of teenage conflict between social classes.", VoorraadAantal = 18 },
                new Book { Title = "Of Mice and Men", Author = "John Steinbeck", Price = 10.50m, Isbn = "9780140177398", ImageUrl = "https://via.placeholder.com/150", Description = "A tragic story of friendship during the Great Depression.", VoorraadAantal = 13 },
                new Book { Title = "Animal Farm", Author = "George Orwell", Price = 9.75m, Isbn = "9780451526342", ImageUrl = "https://via.placeholder.com/150", Description = "A satirical allegory of Soviet totalitarianism.", VoorraadAantal = 20 },
                new Book { Title = "Lord of the Flies", Author = "William Golding", Price = 10.25m, Isbn = "9780399501487", ImageUrl = "https://via.placeholder.com/150", Description = "A story of boys stranded on an island.", VoorraadAantal = 2 },
                new Book { Title = "Slaughterhouse-Five", Author = "Kurt Vonnegut", Price = 11.75m, Isbn = "9780385333849", ImageUrl = "https://via.placeholder.com/150", Description = "A satirical novel about World War II.", VoorraadAantal = 0 },
                new Book { Title = "One Hundred Years of Solitude", Author = "Gabriel García Márquez", Price = 15.00m, Isbn = "9780060883287", ImageUrl = "https://via.placeholder.com/150", Description = "The multi-generational story of the Buendía family.", VoorraadAantal = 14 },
                new Book { Title = "The Little Prince", Author = "Antoine de Saint-Exupéry", Price = 8.99m, Isbn = "9780156012195", ImageUrl = "https://via.placeholder.com/150", Description = "A poetic tale of a young prince traveling the universe.", VoorraadAantal = 17 },
                new Book { Title = "Charlotte's Web", Author = "E.B. White", Price = 9.50m, Isbn = "9780064400558", ImageUrl = "https://via.placeholder.com/150", Description = "A story of friendship between a pig and a spider.", VoorraadAantal = 20 },
                new Book { Title = "The Chronicles of Narnia", Author = "C.S. Lewis", Price = 22.99m, Isbn = "9780066238500", ImageUrl = "https://via.placeholder.com/150", Description = "A fantasy series about children in a magical land.", VoorraadAantal = 10 },
                new Book { Title = "The Secret Garden", Author = "Frances Hodgson Burnett", Price = 8.75m, Isbn = "9780064401883", ImageUrl = "https://via.placeholder.com/150", Description = "A classic story of transformation and healing.", VoorraadAantal = 5 },
                
                new Book { Title = "Anne of Green Gables", Author = "L.M. Montgomery", Price = 9.25m, Isbn = "9780553213133", ImageUrl = "https://via.placeholder.com/150", Description = "The story of an imaginative young orphan girl.", VoorraadAantal = 15 },
                new Book { Title = "Little Women", Author = "Louisa May Alcott", Price = 10.00m, Isbn = "9780147514011", ImageUrl = "https://via.placeholder.com/150", Description = "The story of four sisters growing up.", VoorraadAantal = 12 },
                new Book { Title = "The Picture of Dorian Gray", Author = "Oscar Wilde", Price = 11.25m, Isbn = "9780141439570", ImageUrl = "https://via.placeholder.com/150", Description = "A philosophical novel about vanity and moral corruption.", VoorraadAantal = 8 },
                new Book { Title = "Wuthering Heights", Author = "Emily Brontë", Price = 10.75m, Isbn = "9780141439556", ImageUrl = "https://via.placeholder.com/150", Description = "A passionate and dark love story.", VoorraadAantal = 6 },
                new Book { Title = "Jane Eyre", Author = "Charlotte Brontë", Price = 11.00m, Isbn = "9780141441146", ImageUrl = "https://via.placeholder.com/150", Description = "A story of love, independence, and morality.", VoorraadAantal = 0 },
                new Book { Title = "Dracula", Author = "Bram Stoker", Price = 12.50m, Isbn = "9780141439846", ImageUrl = "https://via.placeholder.com/150", Description = "The classic vampire novel.", VoorraadAantal = 19 },
                new Book { Title = "Frankenstein", Author = "Mary Shelley", Price = 11.99m, Isbn = "9780141439471", ImageUrl = "https://via.placeholder.com/150", Description = "A gothic novel about creation and responsibility.", VoorraadAantal = 16 },
                new Book { Title = "The Adventures of Sherlock Holmes", Author = "Arthur Conan Doyle", Price = 13.00m, Isbn = "9780199536948", ImageUrl = "https://via.placeholder.com/150", Description = "Classic detective stories.", VoorraadAantal = 11 },
                new Book { Title = "Moby-Dick", Author = "Herman Melville", Price = 14.50m, Isbn = "9780142437247", ImageUrl = "https://via.placeholder.com/150", Description = "The epic tale of Captain Ahab's obsession.", VoorraadAantal = 4 },
                new Book { Title = "The Count of Monte Cristo", Author = "Alexandre Dumas", Price = 16.50m, Isbn = "9780140449266", ImageUrl = "https://via.placeholder.com/150", Description = "A story of betrayal, imprisonment, and revenge.", VoorraadAantal = 9 }
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
