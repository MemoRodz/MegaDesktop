using CG.Web.MegaApiClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace MegaDesktop.App.Data
{
    public class MegaElemento
    {
        public string? Id { get; set; }        // ID único del nodo
        public string? ParentId { get; set; }  // ID de la carpeta que lo contiene
        public string? Tipo { get; set; }
        public string? Nombre { get; set; }
        public double TamanoMB { get; set; }
    }

}
