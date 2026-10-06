using Microsoft.EntityFrameworkCore;
using mvc.Models;

var builder = WebApplication.CreateBuilder(args);

// Conexion con PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

  try
  {
    var conectado = await db.Database.CanConnectAsync();
    app.Logger.LogInformation(
        conectado ? "Conexión a PostgreSQL correcta." : "No se pudo conectar a PostgreSQL.");
  }
  catch (Exception ex)
  {
    app.Logger.LogError(ex, "Error al conectar con PostgreSQL.");
  }
}

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();