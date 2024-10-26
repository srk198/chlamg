using Serilog; // Import Serilog

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Log to the console
    .CreateLogger();

// Ensure the app uses Serilog for logging
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Change the default route to point to DataController and Index action
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Data}/{action=Index}/{id?}");

// Log that the application is starting up
Log.Information("Application starting up");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application failed to start correctly");
}
finally
{
    // Ensure the logs are flushed and closed on application exit
    Log.CloseAndFlush();
}
