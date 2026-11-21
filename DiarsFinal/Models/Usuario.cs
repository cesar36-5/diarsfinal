using System;
using System.Collections.Generic;

namespace DiarsFinal.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Usuario1 { get; set; } = null!;

    public string Contrasena { get; set; } = null!;
}
