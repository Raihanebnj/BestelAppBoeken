using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using BestelAppBoeken.Web.Services;

namespace BestelAppBoeken.Web.Controllers.Api
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly OrderNotificationService _notifications;

        public NotificationsController(OrderNotificationService notifications)
        {
            _notifications = notifications;
        }

        [HttpGet("stream")]
        public async Task Stream(CancellationToken ct)
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("X-Accel-Buffering", "no"); // disable buffering on some proxies

            await foreach (var message in _notifications.SubscribeAsync(ct))
            {
                var sse = $"data: {message}\n\n";
                var bytes = Encoding.UTF8.GetBytes(sse);
                await Response.Body.WriteAsync(bytes, 0, bytes.Length, ct);
                await Response.Body.FlushAsync(ct);
            }
        }
    }
}
