namespace Weather
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register the backend API WeatherService engine
            builder.Services.AddHttpClient<Weather.Services.WeatherService>();

            // Add services to the MVC container
            builder.Services.AddControllersWithViews();

            // --- MEMBER 2 REGISTERED SERVICES ---
            builder.Services.AddMemoryCache(); // Required for IMemoryCache injection
            builder.Services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(20); // Configure short session timeouts
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            // -------------------------------------

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            // --- MEMBER 2 MIDDLEWARE ACTIVATOR ---
            app.UseSession(); // Activates Session State pipeline tracking middleware
            // -------------------------------------

            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}