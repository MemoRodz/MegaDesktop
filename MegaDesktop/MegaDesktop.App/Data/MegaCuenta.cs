using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace MegaDesktop.App.Data
{
    public class MegaCuenta
    {
        public string Email { get; set; }
        public string Password { get; set; }

        #region Cumpliendo Política de MEGA

        // NUEVOS CAMPOS PARA REPORTE Y CONTROL DE ACTIVIDAD
        public string EstadoActividad { get; set; } = "Sin verificar";
        public int TotalArchivos { get; set; } = 0;
        public string Almacenamiento { get; set; } = "- / - GB";

        #endregion


        public MegaCuenta(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }

    public class AccountManager
    {
        // Ruta donde se guardará el archivo de configuración en la carpeta del usuario
        private readonly string _pathArchivo;

        // Una clave de entropía opcional para añadir una capa extra de complejidad al cifrado
        private readonly byte[] _entropiaAdicional = Encoding.UTF8.GetBytes("ClaveSecretaMegaApp2026"); // Entropía adicional para la protección de datos

        public AccountManager()
        {
            string carpetaApp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MegaLoginApp");

            // Crear el directorio si no existe
            if (!Directory.Exists(carpetaApp))
            {
                Directory.CreateDirectory(carpetaApp);
            }

            _pathArchivo = Path.Combine(carpetaApp, "cuentas.dat"); //  Cambio de extensión a .dat para indicar que no es JSON plano.

        }

        /// <summary>
        /// Carga las cuentas guardadas desde el archivo JSON del disco.
        /// </summary>
        public ObservableCollection<MegaCuenta> CargarCuentas()
        {
            try
            {
                if (!File.Exists(_pathArchivo))
                {
                    // Si el archivo no existe, devolvemos una lista vacía inicializada
                    return new ObservableCollection<MegaCuenta>();
                }

                // 1. Leer los bytes cifrados en Base64 desde el archivo
                string base64Cifrado = File.ReadAllText(_pathArchivo);
                byte[] datosCifrados = Convert.FromBase64String(base64Cifrado);

                // 2. Descifrar los bytes usando DPAPI (DataProtectionScope.CurrentUser)
                byte[] datosDescifrados = ProtectedData.Unprotect(
                    datosCifrados,
                    _entropiaAdicional,
                    DataProtectionScope.CurrentUser
                );

                // 3. Convertir los bytes descifrados a texto JSON string
                string jsonString = Encoding.UTF8.GetString(datosDescifrados);

                // 4. Reconstruir la lista de objetos
                var lista = JsonSerializer.Deserialize<List<MegaCuenta>>(jsonString);
                return lista != null ? new ObservableCollection<MegaCuenta>(lista) : new ObservableCollection<MegaCuenta>();
            }
            catch (CryptographicException)
            {
                MessageBox.Show("No se pudieron descifrar las cuentas. Es posible que el archivo haya sido modificado o pertenezca a otra sesión de usuario.", "Error de Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new ObservableCollection<MegaCuenta>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las cuentas: {ex.Message}");
                return new ObservableCollection<MegaCuenta>();
            }
        }

        /// <summary>
        /// Guarda la colección completa de cuentas de manera asíncrona o directa en el disco.
        /// </summary>
        public void GuardarCuentas(ObservableCollection<MegaCuenta> cuentas)
        {

            try
            {
                // 1. Convertir la colección de cuentas a un texto JSON normal
                var opciones = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(cuentas, opciones);
                byte[] datosAEncriptar = Encoding.UTF8.GetBytes(jsonString);

                // 2. Cifrar los bytes usando DPAPI
                // Esto vincula los datos al usuario de Windows que tiene la sesión iniciada
                byte[] datosCifrados = ProtectedData.Protect(
                    datosAEncriptar,
                    _entropiaAdicional,
                    DataProtectionScope.CurrentUser
                );

                // 3. Convertir a Base64 para guardarlo como texto seguro en el archivo
                string base64Cifrado = Convert.ToBase64String(datosCifrados);
                File.WriteAllText(_pathArchivo, base64Cifrado);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar las cuentas con cifrado: {ex.Message}", "Error de Persistencia", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
