using MegaDesktop.App.Data;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace MegaDesktop.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MegaService _megaService = new MegaService();
        // Instancia del AccountManager para manejar la persistencia de cuentas
        private readonly AccountManager _accountManager = new AccountManager();

        // Lista dinámica que actualiza automáticamente el ComboBox y el ListBox
        private ObservableCollection<MegaCuenta> _listaCuentas = new ObservableCollection<MegaCuenta>();
        private string? _idCarpetaActual = "";
        private string? _idCarpetaRaiz = "";
        public MainWindow()
        {
            InitializeComponent();

            // Cargar las cuentas desde el archivo JSON al iniciar la aplicación
            _listaCuentas = _accountManager.CargarCuentas();

            // Enlazamos la lista a los controles visuales
            CboCuentas.ItemsSource = _listaCuentas;
            LstCuentasGestion.ItemsSource = _listaCuentas;

            // Enlace con DgResumenGlobal
            DgResumenGlobal.ItemsSource = _listaCuentas;
        }

        // ==========================================
        //  PÁGINA 1: OPERACIONES DE INICIO DE SESIÓN
        // ==========================================

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var cuentaSeleccionada = CboCuentas.SelectedItem as MegaCuenta;

            if (cuentaSeleccionada == null)
            {
                MessageBox.Show("Por favor, selecciona una cuenta de la lista.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnLogin.IsEnabled = false;
            CboCuentas.IsEnabled = false;
            TxtStatus.Text = $"Conectando a {cuentaSeleccionada.Email}...";

            bool exitoso = await _megaService.IniciarSesionAsync(cuentaSeleccionada.Email, cuentaSeleccionada.Password);

            if (exitoso)
            {
                // Detectamos la raíz de la cuenta
                _idCarpetaRaiz = _megaService.ObtenerIdRaiz();
                _idCarpetaActual = _idCarpetaRaiz;

                // Cargamos los archivos de la raíz
                var archivos = _megaService.ObtenerElementosPorCarpeta(_idCarpetaActual);
                DgArchivos.ItemsSource = archivos;

                BtnLogin.Visibility = Visibility.Collapsed;
                BtnLogout.Visibility = Visibility.Visible;
                BtnVolver.Visibility = Visibility.Visible; // Mostramos el botón de navegación

                //TxtStatus.Text = $"Sesión activa: {cuentaSeleccionada.Email}";

                // NUEVO: Muestra la ruta inicial
                TxtStatus.Text = $"📁 Ruta: {_megaService.ObtenerRutaActualTexto(_idCarpetaActual)}";
            }
            else
            {
                MessageBox.Show("Error de credenciales o conexión con MEGA.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnLogin.IsEnabled = true;
                CboCuentas.IsEnabled = true;
                TxtStatus.Text = "Error al conectar.";
            }
        }

        /// <summary>
        /// Doble clic en la tabla
        /// </summary>
        private void DgArchivos_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Obtenemos la fila seleccionada
            if (DgArchivos.SelectedItem is MegaElemento elementoSeleccionado)
            {
                // Solo navegamos si es una Carpeta
                if (elementoSeleccionado.Tipo == "📁 Carpeta")
                {
                    _idCarpetaActual = elementoSeleccionado.Id;

                    // Actualizamos la tabla con el contenido de la nueva carpeta
                    var contenido = _megaService.ObtenerElementosPorCarpeta(_idCarpetaActual);
                    DgArchivos.ItemsSource = contenido;

                    //TxtStatus.Text = $"Carpeta actual: {elementoSeleccionado.Nombre}";

                    // NUEVO: Actualiza la barra de estado con el camino recorrido
                    TxtStatus.Text = $"📁 Ruta: {_megaService.ObtenerRutaActualTexto(_idCarpetaActual)}";
                }
            }
        }

        /// <summary>
        /// Clic en Regresar
        /// </summary>
        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            if (_idCarpetaActual == _idCarpetaRaiz)
            {
                MessageBox.Show("Ya te encuentras en el directorio raíz de la cuenta.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Buscamos el ID de la carpeta superior
            string? idPadre = _megaService.ObtenerIdPadre(_idCarpetaActual);

            if (!string.IsNullOrEmpty(idPadre))
            {
                _idCarpetaActual = idPadre;
                var contenido = _megaService.ObtenerElementosPorCarpeta(_idCarpetaActual);
                DgArchivos.ItemsSource = contenido;

                //TxtStatus.Text = _idCarpetaActual == _idCarpetaRaiz ? "Mostrando carpeta raíz." : "Subió un nivel.";

                // NUEVO: Actualiza la barra al regresar
                TxtStatus.Text = $"📁 Ruta: {_megaService.ObtenerRutaActualTexto(_idCarpetaActual)}";
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            _megaService.CerrarSesion();
            DgArchivos.ItemsSource = null;
            _idCarpetaActual = "";
            _idCarpetaRaiz = "";

            // CAMBIO VISUAL: Mostrar Login de nuevo y ocultar Logout
            BtnLogout.Visibility = Visibility.Collapsed;
            BtnVolver.Visibility = Visibility.Collapsed; // Ocultamos regresar
            BtnLogin.Visibility = Visibility.Visible;

            BtnLogin.IsEnabled = true;
            CboCuentas.IsEnabled = true;
            TxtStatus.Text = "Sesión cerrada de forma segura.";
        }

        // ==========================================
        //  PÁGINA 2: MANTENIMIENTO (AGREGAR/MODIFICAR/BORRAR)
        // ==========================================

        // Al hacer clic en una cuenta de la lista de gestión, llena los campos para editar/borrar
        private void LstCuentasGestion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstCuentasGestion.SelectedItem is MegaCuenta cuenta)
            {
                TxtGestionEmail.Text = cuenta.Email;
                TxtGestionPassword.Text = cuenta.Password;
            }
        }
        /// <summary>
        /// Acción Agregar.
        /// </summary>
        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            string email = TxtGestionEmail.Text.Trim();
            string pass = TxtGestionPassword.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass)) return;

            // Agrega a la lista de la interfaz gráfica.
            _listaCuentas.Add(new MegaCuenta(email, pass));
            // Guarda la lista completa de cuentas en el archivo JSON
            _accountManager.GuardarCuentas(_listaCuentas);

            LimpiarCamposGestion();
            TxtStatus.Text = "Nueva cuenta añadida a la lista.";
        }
        /// <summary>
        /// Acción Modificar.
        /// </summary>
        private void BtnModificar_Click(object sender, RoutedEventArgs e)
        {
            if (LstCuentasGestion.SelectedItem is MegaCuenta cuentaSeleccionada)
            {
                cuentaSeleccionada.Email = TxtGestionEmail.Text.Trim();
                cuentaSeleccionada.Password = TxtGestionPassword.Text;

                // Refresca la vista de las listas
                CboCuentas.Items.Refresh();
                LstCuentasGestion.Items.Refresh();

                // Guardar los cambios hechos en Json
                _accountManager.GuardarCuentas(_listaCuentas);

                LimpiarCamposGestion();
                TxtStatus.Text = "Datos de cuenta actualizados.";
            }
        }

        /// <summary>
        /// Acción Borrar
        /// </summary>
        private void BtnBorrar_Click(object sender, RoutedEventArgs e)
        {
            if (LstCuentasGestion.SelectedItem is MegaCuenta cuentaSeleccionada)
            {
                // Remueve de la interfaz
                _listaCuentas.Remove(cuentaSeleccionada);

                // Actualiza archivo Json
                _accountManager.GuardarCuentas(_listaCuentas);

                LimpiarCamposGestion();
                TxtStatus.Text = "Cuenta eliminada del registro.";
            }
        }

        private void LimpiarCamposGestion()
        {
            TxtGestionEmail.Text = string.Empty;
            TxtGestionPassword.Text = string.Empty;
            LstCuentasGestion.SelectedItem = null;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _megaService.CerrarSesion();
            base.OnClosing(e);
        }

        // ==========================================
        //  PÁGINA 3: PROCESAMIENTO AUTOMATIZADO POR LOTES (LOGIN/LOGOUT MASIVO)
        //  CUMPLIENDO POLÍTICA DE MEGA
        // ==========================================

        // AGREGAR esta línea al final deL constructor MainWindow() existente:
        // DgResumenGlobal.ItemsSource = _listaCuentas;

        /// <summary>
        /// Procesamiento Automatizado por Lotes (Login/Logout Masivo)
        /// </summary>
        private async void BtnProcesarTodo_Click(object sender, RoutedEventArgs e)
        {
            if (_listaCuentas == null || _listaCuentas.Count == 0)
            {
                MessageBox.Show("No hay cuentas registradas para procesar.", "Lista vacía", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Bloqueamos la interfaz y preparamos los controles de progreso
            BtnProcesarTodo.IsEnabled = false;
            PrgGlobal.Visibility = Visibility.Visible;
            PrgGlobal.Value = 0;
            PrgGlobal.Maximum = _listaCuentas.Count;

            int procesadas = 0;
            TxtStatus.Text = "Iniciando mantenimiento masivo de actividad...";

            // Instanciamos un cliente temporal exclusivo para este proceso en segundo plano
            MegaService servicioTemporal = new MegaService();

            foreach (var cuenta in _listaCuentas)
            {
                cuenta.EstadoActividad = "🔄 Conectando...";
                DgResumenGlobal.Items.Refresh(); // Forzamos actualización visual en la tabla

                TxtStatus.Text = $"Procesando cuenta ({procesadas + 1}/{_listaCuentas.Count}): {cuenta.Email}";

                // Ejecutamos Login de forma segura
                bool loginExitoso = await servicioTemporal.IniciarSesionAsync(cuenta.Email, cuenta.Password);

                if (loginExitoso)
                {
                    // 1. Obtener total de archivos en la cuenta
                    int totalArchivos = servicioTemporal.ObtenerTotalArchivos();

                    // 2. Obtener cuotas de almacenamiento
                    var (usadoGB, totalGB) = await servicioTemporal.ObtenerCuotaEspacioAsync();

                    // 3. Asignar los datos recopilados al objeto de la lista
                    cuenta.TotalArchivos = totalArchivos;
                    cuenta.Almacenamiento = $"{usadoGB:N2} / {totalGB:N0} GB";
                    cuenta.EstadoActividad = $"✅ Activa ({System.DateTime.Now:dd/MM HH:mm})";

                    // Cerramos sesión inmediatamente de esta cuenta
                    servicioTemporal.CerrarSesion();
                }
                else
                {
                    cuenta.EstadoActividad = "❌ Error de conexión / credenciales";
                    cuenta.Almacenamiento = "Error";
                }

                // Incrementamos barra de progreso
                procesadas++;
                PrgGlobal.Value = procesadas;
                DgResumenGlobal.Items.Refresh();
            }

            // Al finalizar el bucle, guardamos los nuevos datos de reporte en el disco con DPAPI
            _accountManager.GuardarCuentas(_listaCuentas);

            // Restablecemos controles visuales
            BtnProcesarTodo.IsEnabled = true;
            PrgGlobal.Visibility = Visibility.Collapsed;
            TxtStatus.Text = "Mantenimiento masivo completado de forma exitosa. Cuentas renovadas.";

            MessageBox.Show("Se ha completado el inicio de sesión masivo.\nTodas tus cuentas han renovado su vigencia de actividad en los servidores de MEGA.", "Proceso Finalizado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}