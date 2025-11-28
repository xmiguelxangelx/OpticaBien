using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Optica1.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Optica1.Controllers
{
    public class LoginController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public LoginController(ProyectoopticaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 🔹 Verificar usuario y contraseña
                var usuario = _context.Usuarios
                    .FirstOrDefault(u => u.NombreUsuario == model.NombreUsuario && u.Clave == model.Clave && u.Estado == "Activo");

                if (usuario != null)
                {
                    // 🔹 Obtener el rol del usuario
                    var perfil = _context.UsuarioPerfils
                        .Where(up => up.IdUsuario == usuario.IdUsuario)
                        .Select(up => up.IdPerfilNavigation.Descripcion)
                        .FirstOrDefault();

                    if (perfil != null)
                    {
                        // 🔹 Crear los Claims (identidad de usuario)
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                            new Claim(ClaimTypes.Role, perfil.ToLower()) // 👈 el rol se guarda en minúscula
                        };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        // 🔹 Crear cookie de sesión
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                        // 🔹 Redirigir según el rol
                        switch (perfil.ToLower())
                        {
                            case "administrador":
                                return RedirectToAction("Index", "Admin");

                            case "optometra":
                                return RedirectToAction("Lista", "CitaControlador");

                            case "empleado":
                                return RedirectToAction("Index", "Tratamiento");

                            case "cliente":
                                return RedirectToAction("Index", "Producto");

                            default:
                                return RedirectToAction("Index", "Home");
                        }
                    }
                }

                // 🔹 Si las credenciales son incorrectas
                ViewBag.Error = "Usuario o contraseña incorrectos";
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return View();
        }
    }
}