# 05 — Glosario (términos explicados)

Añade aquí cada término nuevo que aparezca. Explicaciones cortas y sin jerga.

## Herramientas y proyecto
- **SDK de .NET:** el paquete con el que se *compila* programas C# (`dotnet build`). El *runtime* solo sirve para *ejecutarlos*. Para programar necesitas el SDK.
- **Solución (`.sln`):** archivo que agrupa varios proyectos.
- **Proyecto (`.csproj`):** archivo XML que describe un programa o librería: su tipo, framework y paquetes.
- **TFM (Target Framework Moniker):** la línea `<TargetFramework>` del `.csproj`. `net10.0-windows10.0.19041.0` significa ".NET 10 solo en Windows 10 versión 19041 o superior"; es lo que permite usar APIs de Windows como la de música.
- **NuGet:** tienda de paquetes (librerías) de .NET. Se instalan con `dotnet add package <nombre>`.
- **Referencia entre proyectos:** un proyecto usa el código de otro (`dotnet add reference`).
- **Compilar / build:** convertir el código en un programa ejecutable.
- **Publicar (`dotnet publish`):** generar la carpeta final para entregar.
- **Git / commit:** sistema para guardar versiones; un commit es un punto de guardado.
- **xUnit / prueba unitaria:** código que comprueba automáticamente que otra función hace lo correcto.

## Interfaz
- **WPF:** tecnología de Microsoft para ventanas de escritorio en Windows.
- **XAML:** lenguaje (parecido a HTML) con el que se describe cómo se ve una ventana.
- **Code-behind:** el archivo `.xaml.cs` junto a cada ventana, con su código C#.
- **Binding (enlace de datos):** conectar un elemento visual con una propiedad de C# para que se actualice solo. Ej.: `Text="{Binding Title}"`.
- **MVVM:** forma de organizar: **Model** (datos), **View** (lo que se ve), **ViewModel** (la lógica que une ambos). La vista no sabe de lógica; el ViewModel no sabe de botones.
- **ViewModel:** clase C# con las propiedades y comandos que usa una vista.
- **ResourceDictionary:** archivo XAML con colores, estilos y medidas reutilizables. Es donde vive el tema.
- **StaticResource / DynamicResource:** dos formas de usar un recurso. `Dynamic` se actualiza si el recurso cambia (necesario para cambiar de tema en vivo).
- **Dispatcher:** el "turno" del hilo de la interfaz. Solo ese hilo puede tocar la UI; desde otros hilos se usa `Dispatcher.InvokeAsync`.
- **Ventana transparente / en capas (layered window):** ventana cuyas partes pueden no verse, lo que permite la forma de pastilla.
- **Topmost:** la ventana siempre queda por encima de las demás.
- **DPI / escalado:** Windows puede mostrar todo al 125 %, 150 %... WPF usa "unidades independientes del dispositivo" para que se vea igual.

## Windows por dentro
- **API:** conjunto de funciones que ofrece un sistema o servicio para usarlas desde tu código.
- **Win32:** la API clásica de Windows (funciones en DLL como `user32`, `kernel32`).
- **P/Invoke:** manera de llamar desde C# a funciones Win32 (se declaran con `[DllImport]` o, en .NET moderno, `[LibraryImport]`, que es lo que usamos).
- **HWND:** "identificador de ventana" que usa Windows para cada ventana.
- **COM:** tecnología antigua de Windows para componentes; el audio de Windows la usa. NAudio nos la esconde.
- **WinRT:** API moderna de Windows (como la de música). Con el TFM correcto se usa directo desde C#.
- **SMTC (System Media Transport Controls):** servicio de Windows que sabe qué canción suena en cualquier app compatible (la barra de volumen de Windows también lo usa). Es lo que lee Kissa y Nock.
- **Hook de eventos (`SetWinEventHook`):** pedirle a Windows "avísame cuando cambie la ventana activa" en vez de preguntar cada segundo.
- **Mensaje de ventana (`WM_...`):** aviso que Windows manda a una ventana (ej. `WM_CLIPBOARDUPDATE` cuando cambia el portapapeles).
- **Sondeo (polling):** preguntar una y otra vez si algo cambió. Gasta CPU; lo evitamos usando eventos.
- **Evento:** aviso que llega solo cuando algo pasa.
- **Registro de Windows (`HKCU`):** base de datos de configuración. `HKCU` es la parte del usuario actual (no requiere administrador).
- **Mutex:** candado con nombre para saber si la app ya está abierta y no abrir dos copias.
- **Estilo extendido (`WS_EX_...`):** opciones de una ventana. `WS_EX_TOOLWINDOW` la quita de Alt+Tab; `WS_EX_NOACTIVATE` hace que no robe el foco al hacer clic.
- **AppBar:** ventana que Windows trata como "barra de escritorio" (como la barra de tareas). Las AppBar reciben el aviso `ABN_FULLSCREENAPP` cuando algo entra o sale de pantalla completa.
- **Píxeles físicos:** píxeles reales del monitor, sin aplicar el escalado.
- **Gancho de teclado (`WH_KEYBOARD_LL`):** pedirle a Windows ver las teclas que se pulsan en todo el sistema. Lo usamos **solo mientras el notch está abierto** y **solo para Esc**, porque el notch nunca tiene el foco del teclado. No bloquea la tecla ni guarda nada.

## Música y letras
- **LRC:** formato de texto de letras con tiempo: `[01:23.45] texto de la línea`.
- **LRCLIB:** servicio web gratuito que guarda letras en formato LRC.
- **Desfase (offset):** corrección manual en segundos cuando la letra va adelantada o atrasada.
- **Carátula / miniatura:** imagen del álbum que entrega el reproductor.

## Arquitectura
- **Inyección de dependencias (DI):** en vez de crear tú los objetos que necesita una clase, se los entregan al construirla. Permite cambiar piezas (y probarlas) fácilmente.
- **Interfaz (`IMediaService`):** contrato que dice *qué* se puede hacer, sin decir *cómo*. Core define interfaces; Windows las implementa.
- **Self-contained:** programa publicado con su propio .NET dentro (no necesita instalar nada, pero pesa más).
- **Atómico (escritura atómica):** escribir a un archivo temporal y renombrarlo, para que un fallo no deje el archivo a medias.
- **Atajo global (`RegisterHotKey`):** combinación de teclas que Windows reserva para una app y le avisa al pulsarla, aunque estés en otra ventana. Si otra app ya la tiene, Windows la rechaza.
- **Sprite / pixel art:** dibujo pequeño hecho píxel a píxel (aquí 20×14). Para verlo grande sin que se emborrone se amplía con `NearestNeighbor` (cada píxel se vuelve un cuadrado).
- **AltGr:** en teclados español y latino equivale a Ctrl+Alt; por eso un atajo Ctrl+Alt+Q taparía la @.
- **Capturar el ratón (`CaptureMouse`):** pedir que todos los movimientos del ratón lleguen a un elemento aunque el puntero salga de él. Se usa al arrastrar el notch por el asa.
- **`GetCursorPos` / `SetWindowPos`:** funciones de Windows para leer dónde está el ratón y mover una ventana, las dos en píxeles físicos.
- **Área de trabajo:** la parte de la pantalla sin la barra de tareas. La posición del notch se guarda como fracción de ella (0 = izquierda/arriba, 1 = derecha/abajo), para que siga en su sitio si cambia la resolución.
- **`DragMove`:** método de WPF que deja arrastrar una ventana sin barra de título; mientras tanto Windows manda `WM_MOVING`, donde se puede corregir el destino (así el aviso de descanso no se sale de la pantalla).
- **Convertidor (`IValueConverter`):** pieza de WPF que transforma un valor al enlazarlo. `EqualsConverter` dice si los minutos elegidos son iguales a los de una opción del menú, para marcarla.
- **Archivos temporales (`%TEMP%`):** carpeta donde las apps dejan archivos de usar y tirar. NOX solo borra los que tienen más de 24 h y salta los que estén en uso.
- **Enlace o «junction»:** carpeta que en realidad apunta a otra. Al limpiar no se entra en ellas, para no borrar nada fuera de la carpeta elegida.
- **Memoria de trabajo (working set) y `EmptyWorkingSet`:** la RAM que una app tiene cargada ahora. `EmptyWorkingSet` le pide que la devuelva a Windows; si la vuelve a necesitar, Windows se la carga otra vez (por eso la cifra sube de nuevo poco a poco).
- **`GlobalMemoryStatusEx`:** función de Windows que dice cuánta RAM hay en total y cuánta está libre.
- **Temporizador hasta el próximo evento:** en vez de mirar cada poco si la mascota tiene hambre, se calcula cuándo le tocará y se programa un solo temporizador para esa hora (`PetCare.NextChange`).
- **Keep-awake (`SetThreadExecutionState`):** aviso a Windows de "estoy trabajando, no te suspendas ni apagues la pantalla". No toca las opciones de energía; Windows lo olvida en cuanto el programa se cierra.
