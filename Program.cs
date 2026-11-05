using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddScoped<CredencialStorageService>();

// Configuracion de autenticacion dual: Cookies (para la web MVC) y JWT Bearer (para la Web API)
builder.Services
    .AddAuthentication(options =>
    {
      options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
      options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
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
    })
    // Configuracion de autenticacion JWT Bearer para la Web API 
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
      var jwtKey = builder.Configuration["Jwt:Key"] ?? "ClaveSecretaSuperSeguraParaCentroMedicoTurnos2026!*";
      options.RequireHttpsMetadata = false;
      options.SaveToken = true;
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "GestionDeTurnosAPI",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "GestionDeTurnosClients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
      };
    });
// Configuracion de autorizacion
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
var app = builder.Build();
// siembra de usuarios por rol
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
  UsuariosDePrueba.Sembrar(db, hasher);
}
// Verificacion de conexion a PostgreSQL 
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
// Configuracion del pipeline de la aplicacion
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}
// Redireccionamiento a HTTPS y configuracion de archivos estaticos
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
