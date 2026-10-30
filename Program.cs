using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mvc.Models;
using mvc.Services;

var builder = WebApplication.CreateBuilder(args);

// Conexion con PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.MapEnum<Rol>("rol_enum")
    ));

builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<AvatarStorageService>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
      options.LoginPath = "/Account/Login";
      options.AccessDeniedPath = "/Account/AccessDenied";
      options.ExpireTimeSpan = TimeSpan.FromHours(8);
      options.SlidingExpiration = true;
      options.Cookie.Name = "GestionDeTurnos.Auth";
      options.Cookie.HttpOnly = true;
      options.Cookie.SameSite = SameSiteMode.Lax;
      options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
          ? CookieSecurePolicy.SameAsRequest
          : CookieSecurePolicy.Always;
    });
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<CredencialStorageService>();

var app = builder.Build();
// Si no hay usuarios, creamos el primer administrador
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

  if (!db.Usuarios.Any())
  {
    const string emailAdmin = "admin@admin.com";
    const string claveAdmin = "admin123";

    var admin = new Usuario
    {
      nombre = "Admin",
      apellido = "Sistema",
      email = emailAdmin,
      rol = Rol.Administrador,
      activo = true
    };
    admin.password_hash = hasher.HashPassword(admin, claveAdmin);

    db.Usuarios.Add(admin);
    db.SaveChanges();

    // Mostramos las credenciales en consola (solo desarrollo)
    if (app.Environment.IsDevelopment())
    {
      Console.WriteLine("========================================");
      Console.WriteLine("  Primer administrador creado");
      Console.WriteLine($"  Email: {emailAdmin}");
      Console.WriteLine($"  Clave: {claveAdmin}");
      Console.WriteLine("========================================");
    }
  }
}
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
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
