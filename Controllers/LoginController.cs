using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace Optica1.Controllers
{
    public class LoginController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public LoginController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // GET: /Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Login
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var nombreUsuario = model.NombreUsuario?.Trim();
            var clave = model.Clave?.Trim();

            // 1️⃣ Buscar usuario por nombre
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

            if (usuario == null)
            {
                ViewBag.Error = "Usuario o contraseña incorrectos.";
                return View(model);
            }

            // 2️⃣ Validar contraseña (texto plano)
            if ((usuario.Clave ?? string.Empty).Trim() != (clave ?? string.Empty))
            {
                ViewBag.Error = "Usuario o contraseña incorrectos.";
                return View(model);
            }

            // 3️⃣ Obtener todos los roles del usuario
            var perfiles = await _context.UsuarioPerfils
                .Where(up => up.IdUsuario == usuario.IdUsuario)
                .Select(up => up.IdPerfilNavigation.Descripcion)
                .ToListAsync();

            // 4️⃣ Si NO tiene roles, asignar rol por defecto "cliente"
            if (perfiles == null || !perfiles.Any())
            {
                // Aseguramos que exista el perfil "cliente"
                var perfilCliente = await _context.Perfiles
                    .FirstOrDefaultAsync(p => p.Descripcion.ToLower() == "cliente");

                if (perfilCliente == null)
                {
                    // Si no existe, lo creamos
                    perfilCliente = new Perfil
                    {
                        Descripcion = "cliente"
                    };
                    _context.Perfiles.Add(perfilCliente);
                    await _context.SaveChangesAsync();
                }

                // Asignamos el rol cliente al usuario
                var nuevoUsuarioPerfil = new UsuarioPerfil
                {
                    IdUsuario = usuario.IdUsuario,
                    IdPerfil = perfilCliente.IdPerfil
                };

                _context.UsuarioPerfils.Add(nuevoUsuarioPerfil);
                await _context.SaveChangesAsync();

                perfiles = new List<string> { perfilCliente.Descripcion };
            }

            // 5️⃣ Si por alguna razón aún no hay roles, no dejamos pasar
            if (perfiles == null || !perfiles.Any())
            {
                ViewBag.Error = "El usuario no tiene un rol asignado. Contacte al administrador.";
                return View(model);
            }

            // 6️⃣ Crear Claims básicos
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario)
            };

            // 7️⃣ Crear Claims de roles
            var rolesNormalizados = new List<string>();

            foreach (var perfil in perfiles)
            {
                var rol = perfil?.ToLower().Trim();
                if (string.IsNullOrEmpty(rol)) continue;

                rolesNormalizados.Add(rol);
                claims.Add(new Claim(ClaimTypes.Role, rol));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // 8️⃣ Crear cookie de autenticación
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // 9️⃣ Redirección según rol prioritario
            if (rolesNormalizados.Contains("administrador"))
                return RedirectToAction("Index", "Admin");

            if (rolesNormalizados.Contains("optometra"))
                return RedirectToAction("Index", "Optometra");

            if (rolesNormalizados.Contains("empleado"))
                return RedirectToAction("Index", "Empleado");

            if (rolesNormalizados.Contains("cliente"))
                return RedirectToAction("Index", "Cita");

            // Si el rol no coincide con ninguno de los anteriores
            return RedirectToAction("Index", "Home");
        }

        // GET: /Logout
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }

        // GET: /Login/AccesoDenegado
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccesoDenegado()
        {
            return View();
        }
    }
}
