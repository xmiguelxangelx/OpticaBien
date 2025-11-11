using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class UsuarioPerfil
{
    public int IdUsuarioperfil { get; set; }

    public int? IdUsuario { get; set; }

    public int? IdPerfil { get; set; }

    public virtual Perfil? IdPerfilNavigation { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
