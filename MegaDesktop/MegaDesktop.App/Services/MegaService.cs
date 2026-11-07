using CG.Web.MegaApiClient;
using MegaDesktop.App.Data;

namespace MegaDesktop.App.Services
{
    public class MegaService
    {
        private readonly MegaApiClient _client = new MegaApiClient();

        /// <summary>
        /// Caché local para guardar todos los nodos y navegar rápido
        /// </summary>
        private IEnumerable<INode>? _nodosEnMemoria;

        public async Task<bool> IniciarSesionAsync(string email, string password)
        {
            try
            {
                await Task.Run(() => _client.Login(email, password));
                if (_client.IsLoggedIn)
                {
                    // Descargamos la estructura completa de carpetas y archivos una sola vez
                    _nodosEnMemoria = await Task.Run(() => _client.GetNodes());
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene el ID de la carpeta raíz (Root) de la cuenta de MEGA.
        /// </summary>
        public string? ObtenerIdRaiz()
        {
            if (!_client.IsLoggedIn || _nodosEnMemoria == null) return null;

            // Buscamos el nodo principal de tipo Root
            var nodoRaiz = _nodosEnMemoria.FirstOrDefault(n => n.Type == NodeType.Root);
            return nodoRaiz?.Id;
        }

        /// <summary>
        /// Filtra y devuelve los archivos pertenecientes a una carpeta específica.
        /// </summary>
        public List<MegaElemento> ObtenerElementosPorCarpeta(string carpetaId)
        {
            var lista = new List<MegaElemento>();
            if (_nodosEnMemoria == null || string.IsNullOrEmpty(carpetaId)) return lista;

            // Filtramos los nodos cuyo padre sea el ID solicitado
            var hijos = _nodosEnMemoria.Where(n => n.ParentId == carpetaId);

            foreach (var node in hijos)
            {
                if (node.Type == NodeType.File || node.Type == NodeType.Directory)
                {
                    lista.Add(new MegaElemento
                    {
                        Id = node.Id,
                        ParentId = node.ParentId,
                        Tipo = node.Type == NodeType.Directory ? "📁 Carpeta" : "📄 Archivo",
                        Nombre = node.Name,
                        TamanoMB = node.Type == NodeType.File ? (double)node.Size / (1024 * 1024) : 0
                    });
                }
            }

            // Ordenamos para que aparezcan primero las carpetas y luego los archivos
            return lista.OrderByDescending(e => e.Tipo).ThenBy(e => e.Nombre).ToList();
        }

        /// <summary>
        /// Busca el ID de la carpeta padre para poder "subir" un nivel.
        /// </summary>
        public string? ObtenerIdPadre(string carpetaIdActual)
        {
            if (_nodosEnMemoria == null) return null;
            var nodoActual = _nodosEnMemoria.FirstOrDefault(n => n.Id == carpetaIdActual);
            return nodoActual?.ParentId;
        }

        /// <summary>
        /// Reconstruye textualmente el camino completo desde la raíz hasta la carpeta especificada.
        /// </summary>
        public string ObtenerRutaActualTexto(string carpetaIdActual)
        {
            if (_nodosEnMemoria == null || string.IsNullOrEmpty(carpetaIdActual))
                return "Raíz";

            var camino = new List<string>();
            var nodoActual = _nodosEnMemoria.FirstOrDefault(n => n.Id == carpetaIdActual);

            // Vamos subiendo en el árbol de carpetas hasta que ya no haya padre o lleguemos al nodo Root
            while (nodoActual != null)
            {
                if (nodoActual.Type == NodeType.Root)
                {
                    camino.Insert(0, "Raíz");
                    break;
                }
                else if (nodoActual.Type == NodeType.Directory)
                {
                    camino.Insert(0, nodoActual.Name);
                }

                // Buscamos el nodo del padre para la siguiente iteración
                nodoActual = _nodosEnMemoria.FirstOrDefault(n => n.Id == nodoActual.ParentId);
            }

            // Unimos todas las carpetas usando un separador elegante
            return string.Join(" ❯ ", camino);
        }

        public void CerrarSesion()
        {
            if (_client.IsLoggedIn) _client.Logout();
            _nodosEnMemoria = null;
        }

        #region Cumpliendo política de MEGA.

        /// <summary>
        /// Obtiene las métricas de almacenamiento de la cuenta (Espacio Utilizado y Total).
        /// Devuelve un texto formateado o un objeto con los datos.
        /// </summary>
        public async Task<(double UsadoGB, double TotalGB)> ObtenerCuotaEspacioAsync()
        {
            if (!_client.IsLoggedIn) return (0, 0);

            try
            {
                // GetStorageMetrics requiere envolverse en Task.Run por ser síncrono en la librería
                var infoServidor = await Task.Run(() => _client.GetAccountInformation());

                // Convertimos de Bytes a Gigabytes (GB)
                double usadoGB = (double)infoServidor.UsedQuota / (1024 * 1024 * 1024);
                double totalGB = (double)infoServidor.TotalQuota / (1024 * 1024 * 1024);

                return (usadoGB, totalGB);
            }
            catch
            {
                return (0, 0);
            }
        }

        /// <summary>
        /// Cuenta rápidamente el total de archivos cargados en memoria.
        /// </summary>
        public int ObtenerTotalArchivos()
        {
            if (_nodosEnMemoria == null) return 0;
            // Contamos únicamente nodos que correspondan a archivos físicos
            return _nodosEnMemoria.Count(n => n.Type == CG.Web.MegaApiClient.NodeType.File);
        }


        #endregion
    }
}
