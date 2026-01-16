using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using BestelAppBoeken.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BestelAppBoeken.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBookService _bookService;
        private readonly IMessageQueueService _messageQueue;
        private readonly ISalesforceService _salesforceService;
        private readonly ISapService _sapService;

        public HomeController(ILogger<HomeController> logger,
            IBookService bookService,
            IMessageQueueService messageQueue,
            ISalesforceService salesforceService,
            ISapService sapService)
        {
            _logger = logger;
            _bookService = bookService;
            _messageQueue = messageQueue;
            _salesforceService = salesforceService;
            _sapService = sapService;
        }

        public IActionResult Index()
        {
            var books = _bookService.GetAllBooks();
            return View(books);
        }

        [HttpGet]
        public IActionResult Order(int id)
        {
            var book = _bookService.GetBookById(id);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(int bookId, int quantity, string email)
        {
            var book = _bookService.GetBookById(bookId);
            if (book == null) return NotFound();

            var order = new Order
            {
                OrderDate = DateTime.Now,
                CustomerEmail = email,
                TotalAmount = book.Price * quantity,
                Items = new List<OrderItem>
                {
                    new OrderItem { BookId = book.Id, BookTitle = book.Title, Quantity = quantity, UnitPrice = book.Price }
                }
            };

            // 1. Publish to RabbitMQ
            await _messageQueue.PublishOrderAsync(order);

            // 2. Sync to Salesforce (Async)
            await _salesforceService.SyncOrderAsync(order);

            // 3. Post to SAP (Async)
            await _sapService.PostInvoiceAsync(order);

            return RedirectToAction("OrderConfirmation");
        }

        public IActionResult OrderConfirmation()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}