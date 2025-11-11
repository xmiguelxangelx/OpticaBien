using Optica1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------
// 1?? Agregar controladores con vistas
// -----------------------------------------------------
builder.Services.AddControllersWithViews();

// -----------------------------------------------------
// 2?? Configurar la conexión a la base de datos MySQL
// -----------------------------------------------------
builder.Services.AddDbContext<ProyectoopticaContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("conexion"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("conexion"))
    )
);

// -----------------------------------------------------
// 3?? Configurar autenticación con cookies
// -----------------------------------------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";              // ?? Página de inicio de sesión
        options.AccessDeniedPath = "/Login/AccesoDenegado"; // ?? Página si intenta acceder sin permiso
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);  // ?? Expira después de 30 min
        options.SlidingExpiration = true;                   // ?? Se renueva si el usuario sigue activo
    });

// -----------------------------------------------------
// 4?? Agregar autorización
// -----------------------------------------------------
builder.Services.AddAuthorization();

var app = builder.Build();

// -----------------------------------------------------
// 5?? Configurar el pipeline de la aplicación
// -----------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ?? Primero autenticación, luego autorización
app.UseAuthentication();
app.UseAuthorization();

// -----------------------------------------------------
// 6?? Configurar la ruta principal
// -----------------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();