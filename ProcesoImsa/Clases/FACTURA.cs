using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProcesoImsa
{
    public class FACTURA { 
    public String Serie { get; set; }
    public int Numero { get; set; }
    public ENCABEZADO Encabezado { get; set; }
    public DETALLE Detalle { get; set; }
    public FACTURA(ENCABEZADO encabezado, DETALLE detalle, String serie, int numero)
    {
        Encabezado = encabezado;
        Detalle = detalle;
        Serie = serie;
        Numero = numero;
    }
    }
}
