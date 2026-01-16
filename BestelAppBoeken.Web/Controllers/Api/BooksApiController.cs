using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace BestelAppBoeken.Web.Controllers.Api
{
    [Route("api/books")]
    [ApiController]
    [Produces("application/json")]
    public class BooksApiController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly ILogger<BooksApiController> _logger;

        public BooksApiController(IBookService bookService, ILogger<BooksApiController> logger)
        {
            _bookService = bookService;
            _logger = logger;
        }

        /// <summary>
        /// Haalt alle boeken op
        /// </summary>
        /// <param name="searchQuery">Optionele zoekterm voor titel, auteur of ISBN</param>
        /// <returns>Lijst van boeken</returns>
        /// <response code="200">Boeken succesvol opgehaald</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Book>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IEnumerable<Book>> GetAllBooks([FromQuery] string? searchQuery = null)
        {
            try
            {
                IEnumerable<Book> books;

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    books = _bookService.SearchBooks(searchQuery);
                }
                else
                {
                    books = _bookService.GetAllBooks();
                }

                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij ophalen boeken");
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het ophalen van boeken" });
            }
        }

        /// <summary>
        /// Haalt een specifiek boek op via ID
        /// </summary>
        /// <param name="id">Boek ID</param>
        /// <returns>Boek details</returns>
        /// <response code="200">Boek gevonden</response>
        /// <response code="404">Boek niet gevonden</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Book), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Book> GetBook(int id)
        {
            try
            {
                var book = _bookService.GetBookById(id);

                if (book == null)
                {
                    return NotFound(new { error = "Boek niet gevonden" });
                }

                return Ok(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij ophalen boek {Id}", id);
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het ophalen van het boek" });
            }
        }

        /// <summary>
        /// Maakt een nieuw boek aan
        /// </summary>
        /// <param name="book">Boek gegevens</param>
        /// <returns>Het aangemaakte boek</returns>
        /// <response code="201">Boek succesvol aangemaakt</response>
        /// <response code="400">Ongeldige input</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(Book), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<Book> CreateBook([FromBody] Book book)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdBook = _bookService.CreateBook(book);
                return CreatedAtAction(nameof(GetBook), new { id = createdBook.Id }, createdBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij aanmaken boek");
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het aanmaken van het boek" });
            }
        }

        /// <summary>
        /// Werkt een bestaand boek bij
        /// </summary>
        /// <param name="id">Boek ID</param>
        /// <param name="book">Bijgewerkte boek gegevens</param>
        /// <returns>Het bijgewerkte boek</returns>
        /// <response code="200">Boek succesvol bijgewerkt</response>
        /// <response code="400">Ongeldige input</response>
        /// <response code="404">Boek niet gevonden</response>
        /// <response code="500">Server error</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Book), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<Book> UpdateBook(int id, [FromBody] Book book)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedBook = _bookService.UpdateBook(id, book);

                if (updatedBook == null)
                {
                    return NotFound(new { error = "Boek niet gevonden" });
                }

                return Ok(updatedBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij bijwerken boek {Id}", id);
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het bijwerken van het boek" });
            }
        }

        /// <summary>
        /// Verwijdert een boek
        /// </summary>
        /// <param name="id">Boek ID</param>
        /// <returns>Bevestiging van verwijdering</returns>
        /// <response code="200">Boek succesvol verwijderd</response>
        /// <response code="404">Boek niet gevonden</response>
        /// <response code="500">Server error</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult DeleteBook(int id)
        {
            try
            {
                var success = _bookService.DeleteBook(id);

                if (!success)
                {
                    return NotFound(new { error = "Boek niet gevonden" });
                }

                return Ok(new { message = "Boek succesvol verwijderd" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij verwijderen boek {Id}", id);
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het verwijderen van het boek" });
            }
        }
    }
}