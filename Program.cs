using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Chatbothotel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllersWithViews();

            // Add HttpClient for Hotel API
            builder.Services.AddHttpClient("HotelAPI", client =>
            {
                client.BaseAddress = new Uri("https://cozyhotel.runasp.net/api/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            // Add HttpClient for Gemini API
            builder.Services.AddHttpClient("GeminiAPI", client =>
            {
                client.BaseAddress = new Uri("https://generativeai.googleapis.com/v1beta2/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                // Authorization ”Ì÷«› ⁄‰œ «·ÿ·» · Ã‰» „‘«ﬂ· Duplicate
            });

            builder.Services.AddLogging(); // ·≈ŸÂ«— —”«∆· «·‹ debug

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
