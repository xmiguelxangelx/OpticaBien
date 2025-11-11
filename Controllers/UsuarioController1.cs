using Microsoft.AspNetCore.Mvc;
using Optica1.Models;
using System.Linq;

namespace Optica1.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public UsuarioController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // 🔹 Formulario de registro (mismo que tienes)
        [HttpGet]
        public IActionResult RegistrarCliente()
        {
            return View();
        }

        // 🔹 Guarda persona + usuario + perfil cliente (todo en uno)
        [HttpPost]
        public IActionResult RegistrarCliente(Persona nuevaPersona, string nombreUsuario, string clave)
        {
            if (ModelState.IsValid)
            {
                // 1️⃣ Verificar si la persona ya existe
                var personaExiste = _context.Personas
                    .FirstOrDefault(p => p.IdPersona == nuevaPersona.IdPersona);

                if (personaExiste != null)
                {
                    ViewBag.Error = "Ya existe una persona con esta cédula.";
                    return View(nuevaPersona);
                }

                // 2️⃣ Verificar si el usuario ya existe
                var usuarioExiste = _context.Usuarios
                    .FirstOrDefault(u => u.NombreUsuario == nombreUsuario);

                if (usuarioExiste != null)
                {
                    ViewBag.Error = "El nombre de usuario ya está registrado.";
                    return View(nuevaPersona);
                }

                // 3️⃣ Guardar persona
                _context.Personas.Add(nuevaPersona);
                _context.SaveChanges();

                // 4️⃣ Crear usuario vinculado a la persona
                var nuevoUsuario = new Usuario
                {
                    NombreUsuario = nombreUsuario,
                    Clave = clave,
                    IdPersona = nuevaPersona.IdPersona
                };

                _context.Usuarios.Add(nuevoUsuario);
                _context.SaveChanges();

                // 5️⃣ Asignar perfil cliente (id_perfil = 1)
                var usuarioPerfil = new UsuarioPerfil
                {
                    IdUsuario = nuevoUsuario.IdUsuario,
                    IdPerfil = 1
                };

                _context.UsuarioPerfils.Add(usuarioPerfil);
                _context.SaveChanges();

                // 6️⃣ Redirigir al login
                return RedirectToAction("Index", "Login");
            }

            return View(nuevaPersona);
        }
    }
}