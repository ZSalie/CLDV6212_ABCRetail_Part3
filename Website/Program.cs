using ABC_Retailers_Part3.Services;
using ABC_Retailers_Part3.Data;
using Microsoft.EntityFrameworkCore;

namespace ABC_Retailers_Part3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add HttpClient for Azure Functions
            builder.Services.AddHttpClient("functions", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["AzureFunctions:BaseUrl"] ?? "https://abcreta-dreffpadg7ctgwbj.canadacentral-01.azurewebsites.net/api/");
                client.DefaultRequestHeaders.Add("x-functions-key", builder.Configuration["AzureFunctions:DefaultKey"] ?? "xxx");
            });

            // Add Entity Framework for SQL Database
            builder.Services.AddDbContext<AuthDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDbConnection")));

            // Register services - ONLY Azure Functions service
            builder.Services.AddScoped<IAzureFunctionService, AzureFunctionService>();

            // Add session support for shopping cart
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}