using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DiarsFinal.Models;

public partial class Especificacione
{
    public int? IdEspecificaciones { get; set; }

    public string DiasTrabajo { get; set; } = null!;

    public int? CapacidadTotal { get; set; }

    public int? CapacidadActual { get; set; }
}
