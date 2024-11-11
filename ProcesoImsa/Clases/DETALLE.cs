using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProcesoImsa
{
    public class DETALLE
    {
        public List<PRODUCTOS> Productos = new List<PRODUCTOS>();
        public void InsertarProductos(PRODUCTOS prod)
        {
            Productos.Add(prod);
        }
        public List<PRODUCTOS> ObtenerProductos()
        {
            return Productos;
        }
    }
    
}
