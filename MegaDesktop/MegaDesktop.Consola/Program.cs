using CG.Web.MegaApiClient;

class Program
{
    static async Task Main(string[] args)
    {
        // Inicializa el cliente de MegaApiClient
        var client = new MegaApiClient();

        string email = "tu_correo_electronico";
        string password = "tu_contraseña";

        try
        {
            Console.WriteLine($"Iniciando sesión en Mega con {email}...");
            // Inicia sesión en Mega
            await client.LoginAsync(email, password);
            Console.WriteLine("Inicio de sesión exitoso.");

            Console.WriteLine("Obteniendo listado de archivos...");
            // Obtiene todos los nodos (archivos y carpetas)
            IEnumerable<INode> nodes = await client.GetNodesAsync();

            Console.WriteLine("Listado de archivos y carpetas:");

            // Filtro para mostrar sólo archivos (File) y carpetas (Directory)
            // Se evita mostrar la papelera de reciclaje (Rubbish Bin), el directorio raíz como texto plano y otros nodos no deseados.
            foreach (INode node in nodes)
            {
                if (node.Type == NodeType.File || node.Type == NodeType.Directory)
                {
                    string tipo = node.Type == NodeType.Directory ? "[CARPETA]" : "[ARCHIVO]";

                    //Muestra el nombre del archivo o carpeta y su tamaño en bytes si es un archivo
                    if (node.Type == NodeType.File)
                    {
                        Console.WriteLine($"{tipo} {node.Name} (Tamaño: {node.Size} bytes)");
                    }
                    else
                    {
                        Console.WriteLine($"{tipo} {node.Name}");
                    }
                }
            }
            // Cierre la sesión después de obtener los nodos
            await client.LogoutAsync();
            Console.WriteLine("Sesión cerrada correctamente.");

            // Cierre de la aplicación hasta que el usuario presione una tecla.
            Console.WriteLine("Presiona cualquier tecla para salir...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al iniciar sesión: {ex.Message}");
            // En caso de error, se espera la acción del usuario antes de cerrar la aplicación.
            Console.WriteLine("\n\nPresiona cualquier tecla para salir...");
            Console.ReadKey();
        }
    }
}