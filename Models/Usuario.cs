using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string Clave { get; set; } = null!;

    public long? IdPersona { get; set; }

    public virtual ICollection<Citum> CitumIdUsuarioempleadoNavigations { get; set; } = new List<Citum>();

    public virtual ICollection<Citum> CitumIdUsuariopacienteNavigations { get; set; } = new List<Citum>();

    public virtual Persona? IdPersonaNavigation { get; set; }

    public virtual ICollection<UsuarioPerfil> UsuarioPerfils { get; set; } = new List<UsuarioPerfil>();

    public virtual ICollection<Ventum> VentumIdUsuarioempleadoNavigations { get; set; } = new List<Ventum>();

    public virtual ICollection<Ventum> VentumIdUsuariopacienteNavigations { get; set; } = new List<Ventum>();
}
