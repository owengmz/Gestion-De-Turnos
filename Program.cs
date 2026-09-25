using GestorTurnos.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Entity Framework + PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

var app = builder.Build();

//////// temporal para ver la conexion a la base de datos, se puede borrar luego//////////
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

  try
  {
    if (db.Database.CanConnect())
    {
      Console.WriteLine("====================================");
      Console.WriteLine("✅ POSTGRESQL CONECTADO");
      Console.WriteLine("====================================");
    }
    else
    {
      Console.WriteLine("❌ NO SE PUDO CONECTAR A POSTGRESQL");
    }
  }
  catch (Exception ex)
  {
    Console.WriteLine("====================================");
    Console.WriteLine("❌ ERROR DE POSTGRESQL");
    Console.WriteLine(ex.Message);
    Console.WriteLine("====================================");
  }
}
/////////////////////////////////////////////
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();