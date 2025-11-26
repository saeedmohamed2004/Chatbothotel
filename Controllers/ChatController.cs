using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
using System;

namespace Chatbothotel.Controllers
{
    public class ChatController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        //token
        private readonly string HotelUserToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJiM2IzMzBiMC0zMTQ2LTQ4YzYtOGI4Yi01OWMyYzI0ODRlMmQiLCJqdGkiOiJkYjA1MmNjYy04MWQ0LTQwZGYtYjJjYi04Yzk1ZGNmZjU2MDQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6ImIzYjMzMGIwLTMxNDYtNDhjNi04YjhiLTU5YzJjMjQ4NGUyZCIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6InNhZWVkbW9oYW1lZDg1NUB5YWhvby5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoic2FlZWQgbW9oYW1lZCIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IlVzZXIiLCJleHAiOjE3NjMzOTc2MjcsImlzcyI6IkhvdGVsTWFuYWdlbWVudEFQSSIsImF1ZCI6IkhvdGVsTWFuYWdlbWVudENsaWVudCJ9.YI13sAkXuySyH3Cs1y5HI7oWXgqknxPFAmxzHrd-Kbc";

        public ChatController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IActionResult ShowChat()
        {
            return View("ChatFullPage");
        }

        [HttpPost]
        public async Task<JsonResult> SendMessage(string message)
        {
            string reply = await HandleUserMessage(message);
            return Json(new { reply });
        }

        private async Task<string> HandleUserMessage(string msg)
        {
            msg = msg.ToLower();

            var roomKeywords = new List<string> { "room", "غرفة", "available rooms", "price", "type", "الغرف المتاحة","الاسعار","السعر" };

            if (msg.Contains("وريني حجوزاتي") || msg.Contains(" حجوزاتي") || msg.Contains("look"))
                return await GetMyBookings();

            if (msg.Contains("حجز رقم") || msg.Contains("booking number"))
            {
                int bookingId = ExtractRoomId(msg);
                return await GetBookingById(bookingId);
            }

            if (msg.Contains("الغى حجز") || msg.Contains("cancel booking"))
            {
                int bookingId = ExtractRoomId(msg);
                return await CancelBooking(bookingId);
            }

            if (msg.Contains("عدل حجز") || msg.Contains("change booking"))
            {
                int bookingId = ExtractRoomId(msg);
                string newStatus = ExtractDate(msg); 
                return await UpdateBooking(bookingId, newStatus);
            }

            if (msg.Contains("احصائياتي") || msg.Contains("my stats"))
                return await GetUserStats();

            if (msg.Contains("آخر حجوزاتي") || msg.Contains("recent bookings"))
                return await GetRecentBookings();

            if (msg.Contains("صرفت اد ايه") || msg.Contains("صرفت كام") || msg.Contains("spending summary"))
                return await GetSpendingSummary();

            if (msg.Contains("احجز") || msg.Contains("book"))
            {
                int roomId = ExtractRoomId(msg);
                return await CreateBooking(roomId);
            }

            if (roomKeywords.Exists(k => msg.Contains(k)))
                return await GetRooms();

            return "معرفتش أقلك  🤔، حاول تكتب حاجة تانية!";
        }

        private int ExtractRoomId(string msg)
        {
            foreach (var word in msg.Split(' '))
            {
                if (int.TryParse(word, out int number))
                    return number;
            }
            return 1; 
        }

        private string ExtractDate(string msg)
        {
            int index = msg.IndexOf("يوم");
            if (index != -1)
                return msg.Substring(index + 3).Trim();
            return DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
        }

        private async Task<string> GetRooms()
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HotelUserToken);

            try
            {
                var response = await client.GetAsync("https://cozyhotel.runasp.net/api/Rooms");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var rooms = JArray.Parse(json);

                var replyLines = new List<string>();
                foreach (var room in rooms)
                {
                    bool available = room.Value<bool>("isAvailable");
                    replyLines.Add(
                        $"Room {room.Value<string>("roomNumber")} | Type: {room.Value<string>("type")} | Price: ${room.Value<int>("pricePerNight")} | {(available ? "Available ✅" : "Not Available ❌")}"
                    );
                }

                return string.Join("\n", replyLines);
            }
            catch (Exception e)
            {
                return $"معرفتش أجيبلك بيانات الغرف . Error: {e.Message}";
            }
        }


        private async Task<string> CreateBooking(int roomId)
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HotelUserToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var bookingBody = new
            {
                roomId = roomId,
                checkIn = DateTime.UtcNow,
                checkOut = DateTime.UtcNow.AddDays(1)
            };

            var json = new StringContent(
                Newtonsoft.Json.JsonConvert.SerializeObject(bookingBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("https://cozyhotel.runasp.net/api/Bookings", json);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"معرفتش أعمل الحجز ! \nError:\n{error}";
            }

            var content = await response.Content.ReadAsStringAsync();
            var booking = JObject.Parse(content);

            return $"تم الحجز ! \nRoom {booking["roomNumber"]} | Hotel: {booking["hotelName"]} | CheckIn: {booking["checkIn"]} | CheckOut: {booking["checkOut"]} | Price: ${booking["totalPrice"]}";
        }

        private async Task<string> GetMyBookings()
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HotelUserToken);

            var response = await client.GetAsync("https://cozyhotel.runasp.net/api/Bookings/my-bookings");
            var content = await response.Content.ReadAsStringAsync();
            var bookings = JArray.Parse(content);

            if (bookings.Count == 0) return "ما عندكش أي حجوزات حالياً ";

            var lines = new List<string>();
            foreach (var b in bookings)
            {
                lines.Add(
                    $"Room {b["roomNumber"]} | Hotel: {b["hotelName"]} | CheckIn: {b["checkIn"]} | CheckOut: {b["checkOut"]} | Price: ${b["totalPrice"]} | Status: {b["status"]}"
                );
            }

            return string.Join("\n", lines);
        }


        private async Task<string> GetBookingById(int id)
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HotelUserToken);

            var response = await client.GetAsync($"https://cozyhotel.runasp.net/api/Bookings/{id}");
            if (!response.IsSuccessStatusCode) return $"معرفتش أجيب الحجز رقم {id} ";

            var b = JObject.Parse(await response.Content.ReadAsStringAsync());
            return $"Booking {b["id"]} | Room {b["roomNumber"]} | Hotel {b["hotelName"]} | CheckIn: {b["checkIn"]} | CheckOut: {b["checkOut"]} | Price: ${b["totalPrice"]} | Status: {b["status"]}";
        }

        private async Task<string> CancelBooking(int id)
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HotelUserToken);

            var response = await client.PutAsync($"https://cozyhotel.runasp.net/api/Bookings/{id}/cancel", null);
            return response.IsSuccessStatusCode ? $"تم إلغاء الحجز رقم {id} " : $"معرفتش ألغي الحجز رقم {id} ";
        }

        private async Task<string> UpdateBooking(int id, string newStatus)
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HotelUserToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var jsonBody = new StringContent($"\"{newStatus}\"", Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"https://cozyhotel.runasp.net/api/Bookings/{id}/status", jsonBody);
            if (!response.IsSuccessStatusCode) return $"معرفتش أعدل الحجز رقم {id} ";

            var b = JObject.Parse(await response.Content.ReadAsStringAsync());
            return $"تم تعديل الحجز رقم {id} ✅\nRoom {b["roomNumber"]} | Hotel {b["hotelName"]} | New Status: {b["status"]}";
        }

        private async Task<string> GetUserStats()
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HotelUserToken);

            var response = await client.GetAsync("https://cozyhotel.runasp.net/api/Dashboard/user/stats");
            var stats = JObject.Parse(await response.Content.ReadAsStringAsync());

            return $"إجمالي الحجوزات: {stats["totalBookings"]}\nالحجوزات الفعالة: {stats["activeBookings"]}\nالحجوزات السابقة: {stats["pastBookings"]}\nالحجوزات الملغية: {stats["cancelledBookings"]}\nالمتبقي: {stats["pendingBookings"]}\nالمصروف الكلي: ${stats["totalSpent"]}\nقادم للتحقق: {stats["upcomingCheckIns"]}";
        }

        private async Task<string> GetRecentBookings()
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HotelUserToken);

            var response = await client.GetAsync("https://cozyhotel.runasp.net/api/Dashboard/user/recent-bookings?count=5");
            var bookings = JArray.Parse(await response.Content.ReadAsStringAsync());

            var lines = new List<string>();
            foreach (var b in bookings)
                lines.Add($"Booking {b["id"]} | Room {b["roomId"]} | CheckIn: {b["checkIn"]} | CheckOut: {b["checkOut"]} | Status: {b["status"]} | Price: ${b["totalPrice"]}");

            return string.Join("\n", lines);
        }

        private async Task<string> GetSpendingSummary()
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HotelUserToken);

            var response = await client.GetAsync("https://cozyhotel.runasp.net/api/Dashboard/user/spending-summary");
            var summary = JArray.Parse(await response.Content.ReadAsStringAsync());

            var lines = new List<string>();
            foreach (var s in summary)
                lines.Add($"Month {s["monthName"]} | Total Spent: ${s["totalSpent"]} | Bookings: {s["bookingCount"]}");

            return string.Join("\n", lines);
        }
    }
}
