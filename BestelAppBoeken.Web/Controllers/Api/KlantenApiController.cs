using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace BestelAppBoeken.Web.Controllers.Api
{
    [Route("api/klanten")]
    [ApiController]
    [Produces("application/json")]
    public class KlantenApiController : ControllerBase
    {
        private readonly IKlantService _klantService;
        private readonly ILogger<KlantenApiController> _logger;

        public KlantenApiController(IKlantService klantService, ILogger<KlantenApiController> logger)
        {
            _klantService = klantService;
            _logger = logger;
        }

        /// <summary>
        /// Haalt alle klanten op
        /// </summary>
        /// <param name="searchQuery">Optionele zoekterm voor naam, email, telefoon of adres</param>
        /// <returns>Lijst van klanten</returns>
        /// <response code="200">Klanten succesvol opgehaald</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Klant>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IEnumerable<Klant>> GetAllKlanten([FromQuery] string? searchQuery = null)
        {
            try
            {
                IEnumerable<Klant> klanten;

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    klanten = _klantService.SearchKlanten(searchQuery);
                }
                else
                {
                    klanten = _klantService.GetAllKlanten();
                }

                return Ok(klanten);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij ophalen klanten");
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het ophalen van klanten" });
            }
        }

        /// <summary>
        /// Haalt een specifieke klant op via ID
        /// </summary>
        /// <param name="id">Klant ID</param>
        /// <returns>Klant details</returns>
        /// <response code="200">Klant gevonden</response>
        /// <response code="404">Klant niet gevonden</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Klant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Klant> GetKlant(int id)
        {
            try
            {
                var klant = _klantService.GetKlantById(id);
                
                if (klant == null)
                {
                    return NotFound(new { error = "Klant niet gevonden" });
                }

                return Ok(klant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij ophalen klant {Id}", id);
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het ophalen van de klant" });
            }
        }

        /// <summary>
        /// Maakt een nieuwe klant aan
        /// </summary>
        /// <param name="klant">Klant gegevens</param>
        /// <returns>De aangemaakte klant</returns>
        /// <response code="201">Klant succesvol aangemaakt</response>
        /// <response code="400">Ongeldige input</response>
        /// <response code="500">Server error (bijv. email bestaat al)</response>
        [HttpPost]
        [ProducesResponseType(typeof(Klant), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<Klant> CreateKlant([FromBody] Klant klant)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdKlant = _klantService.CreateKlant(klant);
                return CreatedAtAction(nameof(GetKlant), new { id = createdKlant.Id }, createdKlant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij aanmaken klant");
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het aanmaken van de klant. Mogelijk bestaat het e-mailadres al." });
            }
        }

        /// <summary>
        /// Werkt een bestaande klant bij
        /// </summary>
        /// <param name="id">Klant ID</param>
        /// <param name="klant">Bijgewerkte klant gegevens</param>
        /// <returns>De bijgewerkte klant</returns>
        /// <response code="200">Klant succesvol bijgewerkt</response>
        /// <response code="400">Ongeldige input</response>
        /// <response code="404">Klant niet gevonden</response>
        /// <response code="500">Server error</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Klant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<Klant> UpdateKlant(int id, [FromBody] Klant klant)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedKlant = _klantService.UpdateKlant(id, klant);
                
                if (updatedKlant == null)
                {
                    return NotFound(new { error = "Klant niet gevonden" });
                }

                return Ok(updatedKlant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij bijwerken klant {Id}", id);
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het bijwerken van de klant. Mogelijk bestaat het e-mailadres al." });
            }
        }

        /// <summary>
        /// Verwijdert een klant
        /// </summary>
        /// <param name="id">Klant ID</param>
        /// <returns>Bevestiging van verwijdering</returns>
        /// <response code="200">Klant succesvol verwijderd</response>
        /// <response code="404">Klant niet gevonden</response>
        /// <response code="500">Server error</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult DeleteKlant(int id)
        {
            try
            {
                var success = _klantService.DeleteKlant(id);
                
                if (!success)
                {
                    return NotFound(new { error = "Klant niet gevonden" });
                }

                return Ok(new { message = "Klant succesvol verwijderd" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij verwijderen klant {Id}", id);
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het verwijderen van de klant" });
            }
        }
    }
}
