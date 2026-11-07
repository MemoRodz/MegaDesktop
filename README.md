<img alt="AR Contenidos" src='https://firebasestorage.googleapis.com/v0/b/capacitechshop.appspot.com/o/IMAGENES_LOGO%2F768c4102b7f64dfa8d0570d79acccba2.jpeg?alt=media&token=4fb82046-4f6b-4cbe-807f-ffb9b0811911' width='48' height='48' style="object-fit:cover;border-radius:50%;border:1px solid #ddd;display:block;margin:auto;" />

# MegaDesktopManager ☁️🛡️

Un gestor de escritorio avanzado y ligero desarrollado en **C#** y **WPF (.NET 10 / Visual Studio 2026)** para administrar y automatizar de forma masiva múltiples cuentas de **MEGA**.

El proyecto nació inicialmente como una prueba de concepto en una aplicación de consola para validar la API de MEGA, evolucionando hacia un entorno gráfico moderno que soluciona de raíz el problema de vencimiento y congelamiento de cuentas por inactividad.

---

## ✨ Características Principales

*   **🗂️ Explorador y Navegación de Archivos:** Visualización de archivos en una cuadrícula (`DataGrid`) con soporte para navegación interna (doble clic para entrar en carpetas, botón de retorno y barra de ruta dinámica *breadcrumbs*).
*   **👥 Soporte Multicuenta Persistente (ABC):** Panel completo para Registrar (Altas), Modificar (Cambios) y Eliminar (Bajas) cuentas. Los datos quedan guardados y listos para usarse cada vez que abres la app.
*   **🔒 Cifrado de Credenciales Local (DPAPI):** Las contraseñas no se exponen en texto plano. Se guardan en un archivo `.dat` encriptado de forma nativa mediante la API de Protección de Datos de Windows (`ProtectedData`), vinculada de forma exclusiva a tu sesión de usuario de Windows.
*   **⚡ Mantenedor de Actividad Automatizado (Anti-Congelamiento):** Un sistema de procesamiento por lotes que realiza un ciclo rápido de *Login ➔ Consulta de Métricas ➔ Logout* de todas tus cuentas guardadas con un solo clic, cumpliendo de forma automática la política de uso regular de MEGA.
*   **📊 Monitor Central de Cuotas:** Reporte global instantáneo que detalla el estado de actividad de cada cuenta, número total de archivos físicos y el almacenamiento consumido (`Usado / Total GB`).

---

## 🛠️ Tecnologías y Librerías Utilizadas

*   **Lenguaje:** C# (.NET 9.0)
*   **Entorno de Desarrollo:** Visual Studio 2026
*   **Interfaz Gráfica:** WPF (Windows Presentation Foundation) con XAML.
*   **Conectividad:** [MegaApiClient](https://github.com) (API Cliente Nativa de MEGA para .NET).
*   **Seguridad:** `System.Security.Cryptography.ProtectedData` (DPAPI).
*   **Serialización:** `System.Text.Json`.

---

## 📐 Evolución de la Arquitectura del Código

### 1. Etapa Inicial: Aplicación de Consola (`MegaLogin.Consola`)
La fase de investigación del proyecto se centró en un espacio de nombres dedicado a validar los métodos de autenticación síncronos de la librería dentro de un entorno controlado.

*   **Ubicación lógica propuesta:** `MegaLogin.Consola.Data`
*   **Enfoque:** Validaciones directas del método `Login(email, password)` e impresiones jerárquicas simples en la consola de comandos mediante la lectura del arreglo plano devuelto por `GetNodes()`.

### 2. Etapa Final: Arquitectura WPF Multicapa
Para la aplicación de escritorio, el código se reestructuró para separar las responsabilidades, asegurar la interfaz de usuario ante operaciones bloqueantes y garantizar la persistencia criptográfica:

```text
MegaDesktopApp/
│
├── Data/
│   ├── AccountManager.cs   <-- Orquestador de persistencia. Cifra/Descifra con DPAPI y gestiona el JSON.
│   └── MegaCuenta.cs       <-- Modelo de datos de la cuenta (E-mail, Password, Almacenamiento, Estado).
│
├── Services/
│   └── MegaService.cs      <-- Envoltura asíncrona de MegaApiClient. Maneja memoria caché de nodos y cálculo de cuotas.
│
├── MainWindow.xaml         <-- Diseño visual dividido en 3 pestañas dinámicas con XAML.
└── MainWindow.xaml.cs      <-- Code-Behind que controla los estados de los hilos de UI y eventos de botones.
```

---

## 🛠️ Cómo Compilar e Instalar el Proyecto

1.  **Clonar el repositorio:**
    ```bash
    git clone https://github.com
    ```
2.  **Abrir la solución:** Ejecuta Visual Studio 2026 y abre el archivo `.sln`.
3.  **Instalar dependencias NuGet:** Abre la Consola del Administrador de paquetes NuGet (`Herramientas > Administrador de paquetes NuGet > Consola...`) y ejecuta:
    ```shell
    Install-Package MegaApiClient
	Install-Package Newtonsoft.Json
    Install-Package System.Security.Cryptography.ProtectedData
    ```
4.  **Compilar y Ejecutar:** Presiona `F5` o el botón de Inicio en Visual Studio.

#### NOTA: Es posible que se deba actualizar "Newtonsoft.Json" y probablemente no sea necesario instalar "System.Security.Cryptography.ProtectedData" pues ya están includos en System. 

---

## 💻 Guía de Uso del Entorno Gráfico

El programa se encuentra estructurado en tres pestañas estratégicas:

1.  **Explorador de Archivos:** Selecciona cualquiera de tus cuentas registradas en el menú desplegable y presiona **Iniciar Sesión**. Los controles de edición se congelarán por seguridad mientras navegas. El botón cambiará automáticamente a **Cerrar Sesión** para liberar el entorno.
2.  **Configurar Cuentas:** Modulo CRUD (ABC) para tus accesos. Al agregar, editar o remover ítems, el archivo en segundo plano se reescribe de inmediato.
3.  **Estado y Actividad Global:** Muestra la tabla concentradora. Al presionar **⚡ Mantener Cuentas Activas**, una barra de progreso guiará el avance del bucle masivo que simula la actividad en los servidores de MEGA y actualiza las cuotas en tiempo real.

---

## 🔒 Detalles del Almacenamiento Seguro (DPAPI)

Los datos sensibles de las cuentas no se almacenan en texto plano en directorios del sistema. La clase `AccountManager` compila la información y la guarda en la ruta local del usuario:
`%AppData%\Roaming\MegaDesktopApp\cuentas.dat`

Los bytes del archivo resultante se encuentran encriptados bajo el espectro `DataProtectionScope.CurrentUser`. Esto garantiza que:
1.  **No se requiere una contraseña maestra** por parte del usuario para abrir la aplicación.
2.  Si el archivo de datos es robado o copiado a otra computadora, **es totalmente imposible de descifrar o leer**, ya que requiere de las llaves criptográficas únicas del perfil de Windows del creador original.

---

## 📝 Licencia

Este proyecto está bajo la Licencia MIT. Eres libre de usarlo, modificarlo y distribuirlo para tus propios fines de desarrollo o personales.

## 📝 Dependencias

Este proyecto utiliza la biblioteca MegaApiClient, licenciada bajo MIT:

https://github.com/gpailler/MegaApiClient

Todos los derechos de dicha biblioteca pertenecen a sus autores respectivos.

