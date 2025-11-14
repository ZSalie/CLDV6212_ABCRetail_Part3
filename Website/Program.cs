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

            // Get Azure Functions configuration
            var azureFunctionBaseUrl = builder.Configuration["AzureFunctions:BaseUrl"];
            var azureFunctionKey = builder.Configuration["AzureFunctions:DefaultKey"];

            // Validate and set default if configuration is missing
            if (string.IsNullOrEmpty(azureFunctionBaseUrl))
            {
                azureFunctionBaseUrl = "https://your-function-app.azure.net/api/";
                Console.WriteLine("Warning: Using default Azure Function Base URL");
            }

            if (string.IsNullOrEmpty(azureFunctionKey))
            {
                azureFunctionKey = "default-key";
                Console.WriteLine("Warning: Using default Azure Function Key");
            }

            Console.WriteLine($"Azure Function Base URL: {azureFunctionBaseUrl}");
            Console.WriteLine($"Azure Function Key configured: {!string.IsNullOrEmpty(azureFunctionKey)}");

            // Register AzureFunctionService with proper HttpClient configuration
            builder.Services.AddHttpClient<IAzureFunctionService, AzureFunctionService>(client =>
            {
                client.BaseAddress = new Uri(azureFunctionBaseUrl);
                if (!string.IsNullOrEmpty(azureFunctionKey) && azureFunctionKey != "default-key")
                {
                    client.DefaultRequestHeaders.Add("x-functions-key", azureFunctionKey);
                }
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Add Azure Storage Service
            builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();

            // Add Entity Framework for SQL Database
            var connectionString = builder.Configuration.GetConnectionString("AuthDbConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Warning: AuthDbConnection string is not configured");
            }
            else
            {
                builder.Services.AddDbContext<AuthDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }

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