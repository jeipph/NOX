# 03 — Hoja de ruta (ROADMAP)

Marca `[x]` cuando algo esté terminado **y verificado por la persona usuaria**. Un hito a la vez.

**MVP (v1) = M0 a M4 + M9 (solo el tema) + M2 (pantalla completa).** Después: M5, M6, M7, M8, M10, M11.

---

## M0 — Preparar el equipo (≈30–45 min)
Objetivo: poder compilar y ejecutar un programa C# en este PC.
- [x] Instalar el SDK de .NET 10 (10.0.403): `winget install Microsoft.DotNet.SDK.10` (cerrar y volver a abrir la terminal).
- [x] Verificar: `dotnet --version` muestra `10.x`.
- [x] Editor: **VS Code + extensión "C# Dev Kit"** (elegido 2026-10-27).
- [x] `git init` en `NOX/` y primer commit con estos documentos.
- [x] Crear `.gitignore` de .NET: `dotnet new gitignore`.

**Terminado cuando:** `dotnet --version` funciona y existe un repositorio git con los documentos.

## M1 — Esqueleto de la solución (≈1 h)
- [x] `dotnet new sln -n NOX`
- [x] Crear proyectos: `Nox.Core` (classlib net10.0), `Nox.Windows` (classlib `net10.0-windows10.0.19041.0`), `Nox.App` (WPF `net10.0-windows10.0.19041.0`), `Nox.Core.Tests` (xUnit).
- [x] Añadir referencias: App→Windows, App→Core, Windows→Core, Tests→Core.
- [x] Añadir paquetes NuGet de `02-ARCHITECTURE.md` (solo CommunityToolkit.Mvvm y DI por ahora).
- [x] `NotchWindow`: ventana sin borde, transparente, siempre encima, sin barra de tareas, pastilla negra redondeada de 220×34 arriba al centro.
- [x] Icono en la bandeja con menú: "Ajustes (pendiente)" y "Salir".
- [x] `Mutex` de instancia única.

**Terminado cuando:** al ejecutar, aparece la pastilla arriba al centro, no sale en la barra de tareas, y "Salir" cierra la app. `dotnet build` y `dotnet test` pasan.

## M2 — Comportamiento del notch (≈2 h)
- [x] Estilos extendidos por P/Invoke (`WS_EX_TOOLWINDOW`, `WS_EX_NOACTIVATE`).
- [x] Expandir al entrar el ratón y contraer al salir (con retraso de ~400 ms), con animación de 220 ms.
- [ ] Ajuste correcto con escalado de pantalla (DPI 100 %, 125 %, 150 %). Probar cambiando el escalado en Windows. *(Código listo y revisado en M11; 100 % probado. Falta 125/150 %: lo cambia la persona.)*
- [x] Ocultar cuando una app está en pantalla completa (aviso `ABN_FULLSCREENAPP` de una AppBar sin espacio reservado + `FullscreenRules`).
- [x] Prueba: contenido de relleno ("Hola") dentro del panel expandido.

**Terminado cuando:** pasa el ratón → se despliega suave; sale → se pliega; abrir un video en pantalla completa lo oculta; al salir de pantalla completa vuelve.

## M2b — Estilos y modos de visibilidad (≈1.5 h) *(añadido tras la investigación)*
- [x] Tres estilos de la pastilla: **Notch** (pegado arriba), **Cápsula** (separada del borde), **Indicador** (puntito mínimo).
- [x] Modo "visible solo al pasar el ratón por el borde superior".
- [x] Tecla Esc contrae el notch.
- [x] Opción "No reservar la franja superior" (por defecto) frente a "Reservarla" con la API de barra de aplicaciones (`SHAppBarMessage`) — solo si se desea.

## M3 — Ahora suena (≈3 h)
- [x] Interfaz `IMediaService` + modelo `MediaInfo` en Core.
- [x] `SmtcMediaService` con los eventos descritos en la arquitectura.
- [x] Vista `NowPlayingView`: carátula, título, artista, botones ⏮ ⏯ ⏭, barra de progreso.
- [x] En el estado contraído mostrar título corto (máx. ~24 caracteres con "…").
- [ ] *(YouTube Music en Chrome verificado 2026-10-27: datos, carátula y los 3 botones. Falta MusicBee/carpetas locales.)* Probar con: YouTube Music en Chrome/Edge (y su app de escritorio si se usa) y música local con MusicBee / foobar2000 / Reproductor multimedia. Comprobar cuál publica bien en SMTC (MusicBee puede necesitar activar su integración con los controles de Windows; no confirmado).
- [x] Manejo de "no suena nada": mostrar texto discreto "Nada en reproducción".
- [x] **`FakeMediaService`** (canción y letra de mentira) para probar la interfaz sin abrir un reproductor; activable con el argumento `--fake` (idea del modo `--selftest` de Oyenza).

## M3b — Varios reproductores (≈1 h) *(añadido tras la investigación)*
- [x] Listar todas las sesiones SMTC activas (`GetSessions()`), no solo la actual.
- [x] Ajuste "Reproductor preferido" y cambio rápido desde el notch (etiqueta "Auto · Chrome" que rota Auto → reproductores → Auto; en memoria hasta M9). Automático = el que suena, aunque Windows marque otro como actual (`SourcePicker`).

**Terminado cuando:** la información cambia sola al cambiar de canción y los 3 botones funcionan en YouTube Music.

## M4 — Letras sincronizadas (≈3 h)
- [x] Tests primero: `LrcParserTests` (varias marcas, líneas vacías, metadatos) y `LyricsSyncEngineTests` (índice según posición y desfase).
- [x] `LrcParser` y `LyricsSyncEngine` en Core.
- [x] `LrclibClient` en Windows (HttpClient, User-Agent, tiempo de espera 5 s, manejo de 404).
- [x] Validación de duración (±3 s) y caché en disco.
- [x] `LyricsView`: línea actual resaltada y la anterior/siguiente atenuadas; desplazamiento suave.
- [x] Ajuste de desfase −5…+5 s con botones.
- [x] Opción para desactivar letras (privacidad): casilla "Buscar letras en internet" en la bandeja. Probado: desactivada muestra "Letras desactivadas" y no consulta lrclib.net.
- [x] ~~Importar un archivo `.lrc` manualmente~~ — **descartado** por la persona usuaria el 2026-10-27 (sobraba).
- [x] Estado discreto "Sin letra sincronizada" (no es un error).

**Terminado cuando:** una canción con letra sincronizada la muestra en tiempo con el audio y los tests pasan.
*(Verificado de oído por la persona usuaria el 2026-10-27.)*

## M5 — Vinilo (≈1.5 h)
- [x] `VinylView`: carátula circular con giro (sin brazo, por sencillez). Una vuelta cada 4 s a 30 imágenes/s (`Vinyl.TurnDuration`, `Vinyl.FrameRate`).
- [x] Pausar animación si no suena o está contraído (también si la pestaña Vinilo no está a la vista). Medido: ~6 % de un núcleo girando, ~0,6 % con el notch cerrado.
- [x] Pestañas abajo: Ahora suena / Letras / Vinilo.

## M6 — Volumen y reloj (≈1.5 h)
- [x] `IVolumeService` con NAudio (`NAudio.Wasapi` 2.2.1, MIT), deslizador y botón de silencio dentro de "Ahora suena". Probado en los dos sentidos (notch → Windows y teclas de volumen → notch).
- [x] Reloj en la pastilla contraída cuando no suena nada (formato de hora de Windows; despierta una vez por minuto). *(Sin probar: necesitaba parar la música.)*
- (Batería descartada: la app es para un PC de escritorio.)

## M6b — Enfoque (≈1.5 h) *(añadido tras la investigación)*
- [x] Temporizador de concentración con varias duraciones (15 min – 1 h 30). *(Sin descanso automático ni cronómetro: no se pidieron.)*
- [x] **Keep-awake:** «Mantener el PC despierto» en el menú de la bandeja (`IKeepAwake` → `KeepAwake` con `SetThreadExecutionState`: ni suspensión ni pantalla apagada). No se guarda: cada vez que se abre NOX empieza apagado, y al cerrar NOX se deshace solo. *(2026-10-29: comprobado con un script que Windows acepta y retira la petición; el menú no se pudo abrir en la prueba —clic derecho en la bandeja no permitido—: falta que la persona lo vea.)*
- [x] Temporizadores solo activos cuando se usan (un tic por segundo mientras corre).

## M6d — Salida de audio y mezclador (≈2 h) *(añadido tras la investigación)*
- [x] Listar salidas y cambiar la predeterminada: botón «Salida: …» del mezclador; con varias salidas, cada clic pasa a la siguiente (para los tres usos: general, multimedia y llamadas). Usa `IPolicyConfig` (no documentada; riesgo evaluado: bajo, ver `06-LICENSES-AND-RISKS.md`); si Windows la rechaza, se avisa.
- [x] Volumen y silencio por app con `AudioSessionManager` de NAudio (`NAudioMixer`). Las sesiones de una misma app van juntas (Chrome abre varias), NOX no sale, «Sonidos del sistema» al final (`MixerApps` en Core, 6 pruebas). Sin sondeo: se lee al abrir el mezclador y Windows avisa si aparece una app con sonido o cambian las salidas.
- [x] Panel **Mezclador** (sin pestaña propia): botón «Mezclador» en «Ahora suena», junto al volumen (también cuando no suena nada); flecha para volver.
- *Verificado por Claude (2026-10-29, `--fake`): lista la salida (Altavoces Realtek, la única activa en este PC) y «Sonidos del sistema»; silenciar y quitar el silencio llega a Windows; volver funciona. Se encontró y corrigió un fallo: tras un aviso de cambio de salida, el mezclador decía «Sin salida de audio». El cambio de salida solo se probó poniendo la misma (en este PC hay una sola): falta probarlo con dos salidas (p. ej. auriculares).*

## M7 — Portapapeles (≈2.5 h)
- [x] Tests de `ClipboardHistory` (límite 50, sin duplicados: una copia repetida sube arriba, fijar, borrar sin tocar fijados).
- [x] `ClipboardMonitor` con `WM_CLIPBOARDUPDATE` y API Win32 del portapapeles, reintentos y exclusión de gestores de contraseñas (probado: una copia con `ExcludeClipboardContentFromMonitorProcessing` no aparece).
- [x] `ClipboardView`: lista, clic para copiar, botón fijar (se guarda en `pinned-clips.json`), "Borrar historial" (los fijados se quedan).

## M8 — Notas rápidas (≈1.5 h)
- [x] `NotesView`: cuadro de texto, Ctrl+Enter guarda `.md` de forma atómica (nunca sobrescribe: `-2.md` si coincide el segundo). "Modo escritura": al hacer clic en el cuadro el notch acepta el teclado y no se cierra; Esc lo cierra y devuelve el teclado a tu ventana. Probado con ratón y teclado reales.
- [x] Lista de las últimas 10 notas (también las creadas fuera); clic abre el archivo con el programa de Windows. Botón "Abrir carpeta".
- [x] Carpeta configurable desde el menú de la bandeja ("Carpeta de notas…"), guardada en settings.json. *(El diálogo no se probó.)*

## M9 — Ajustes y temas (≈3 h)
- [x] `AppSettings` + `JsonSettingsStore` (con valores por defecto y tolerancia a archivo dañado).
- [x] ~~`SettingsWindow`~~ → **todo en el menú de la bandeja** (decisión 2026-10-27, por sencillez): Tema, Estilo, Solo al pasar el ratón, Reservar franja, Buscar letras, Iniciar con Windows. Módulos activables y desfase por defecto: no hacen falta por ahora.
- [x] Cambio de tema **en vivo** sin reiniciar. *(Probado Claro/Oscuro; "Sistema" sigue a Windows con `UserPreferenceChanged`, sin probar para no tocar la configuración de Windows.)*
- [x] Arranque con Windows por `HKCU\...\Run`.
- [x] (Para el MVP basta con el tema.) Además se guardan todas las opciones y el reproductor fijado.

## M9b — Atajos globales (≈1 h) *(añadido tras la investigación)*
- [x] Atajos configurables con `RegisterHotKey` (`GlobalHotkeys` + `HotkeyService`; `Hotkey`/`HotkeySettings` en Core con 12 pruebas): abrir/cerrar el notch **Ctrl+Alt+N**, play/pausa **Ctrl+Alt+P**, letra 0,5 s antes **Ctrl+Alt+,**, después **Ctrl+Alt+.**, sin desfase **Ctrl+Alt+0**. Se guardan en `settings.json` → `Hotkeys`. Ventana **«Atajos de teclado…»** en el menú de la bandeja: clic en un cuadro y pulsar la combinación (Retroceso/Supr la quita), «Restablecer», casilla para desactivarlos todos.
- [x] Conflictos: Windows rechaza un atajo que ya tiene otra app → «Ocupado por otra app» en rojo; también «Repetido» y «No se entiende». El menú dice «(⚠ N sin funcionar)».
- *Verificado por Claude (2026-10-29, `--fake`): Ctrl+Alt+N abre y cierra, Ctrl+Alt+P pausa, Ctrl+Alt+, ×3 → +1,5 s y Ctrl+Alt+. → +1,0 s; Ctrl+Alt+0 ocupado por otro proceso → detectado; capturar una combinación repetida → aviso; Restablecer y guardado. **En este PC Ctrl+Alt+Espacio lo tiene otra app**: por eso play/pausa es Ctrl+Alt+P. Por defecto se evitan teclas que escriben algo con AltGr (= Ctrl+Alt) en teclados español/latino. No se pudo abrir el menú de la bandeja (clic derecho no permitido): falta que la persona lo vea. Pendiente menor: la barra de título de la ventana sale clara con el tema oscuro.*

## M10 — Empaquetado (≈2 h)
- [x] `dotnet publish` **con .NET incluido** (`--self-contained true`), para poder enviarlo a PCs sin .NET. Comandos en `installer/README.md`.
- [x] Probado aquí: instalación silenciosa en una carpeta de prueba, NOX arranca sin errores, desinstalación limpia (conserva notas y ajustes).
- [x] Instalador con **Inno Setup 6** (`installer/nox.iss`): para el usuario (sin administrador), español, acceso en el menú Inicio, casilla "Iniciar con Windows", cierra NOX antes de instalar o desinstalar. Resultado: `dist/NOX-0.1.0-instalador.exe` (47,5 MB).
- [x] Icono (N en círculo negro), nombre final NOX, versión 0.1.0.
- [x] Nota: el instalador sin firma digital hará que SmartScreen avise; es normal en proyectos personales.
- [x] ~~Firma gratuita (SignPath)~~: no aplica, exige que el código sea público y el repositorio será privado. Se acepta el aviso de SmartScreen.

## M11 — Pulido y calidad
- [x] Revisar consumo (Administrador de tareas): objetivo **≤150 MB de RAM y ~0 % de CPU en reposo** con 1 monitor. *(2026-10-27, Release con todas las pestañas abiertas: 90 MB de memoria privada —lo que muestra el Administrador de tareas—, 0 ms de CPU en 10 s de reposo.)*
- [~] Probar con 1 y 2 monitores, y con escalados distintos. *(2026-10-29: con los dos monitores de la persona —S24F350 principal y DELL P2412H a la izquierda, ambos 1920×1080 al 100 %— el notch, la ventana de atajos y el aviso de descanso salen en el principal y nada en el secundario. El notch y el aviso usan ahora el escalado del monitor principal (`WindowHelper.GetPrimaryMonitorScale`), no el del monitor donde nace la ventana. **Falta probar 125 % y 150 %**: hay que cambiar el escalado en Configuración → Pantalla, y eso lo hace la persona.)*
- [x] Revisar excepciones y mensajes de log. *(NOX nunca había escrito `error.log`. Ahora `ErrorLogFile` (Core, 3 pruebas) apunta también los errores de tareas en segundo plano y los que cierran la app; un error de la interfaz ya no cierra NOX: se apunta y se avisa solo la primera vez; solo un fallo al arrancar cierra. El archivo no pasa de 512 KB: el viejo se aparta como `error.old.log`.)*
- [x] **Fallo encontrado y corregido:** las capas transparentes que ocupan la pantalla (la del control de Claude, la de NVIDIA con Alt+Z, grabadores) se tomaban por pantalla completa y el notch se escondía, a veces para siempre porque Windows no avisa de que «terminó». Ahora `FullscreenRules` ignora las ventanas que dejan pasar el ratón o no aceptan el teclado (`WS_EX_TRANSPARENT`, `WS_EX_NOACTIVATE`; 3 pruebas), y mientras el notch está escondido se escucha cada cambio de ventana activa (`SetWinEventHook`, sin sondeo) para volver a mostrarlo. *(Comprobado: con la capa de Claude delante el notch ya no se esconde. El regreso tras una pantalla completa de verdad no se pudo probar: necesita un juego o vídeo a pantalla completa.)*
- [x] Barra de título oscura en la ventana de atajos (`TitleBar.SetDark`, sigue al tema de NOX).
- [x] README de usuario (sin capturas por ahora).
- [x] Revisar `THIRD-PARTY-NOTICES.md` (creado: todas MIT, copyright leído de cada repositorio oficial).

## M12 — Mascota pixel art (≈3 h) *(pedido 2026-10-29)*
- [x] Pestaña **Mascota**: pixel art propio (`Nox.Core/Pets/PetSprites.cs`, dibujos de 20×14 escritos como letras; cada letra es un color de la paleta del dibujo). Un **gato gris oscuro de ojos amarillos** se sienta y parpadea, anda, persigue un ovillo, le da zarpazos y salta (`PetPlayground`, determinista con semilla; 30 pruebas).
- [x] Elegir mascota arriba del panel: **Gato gris**, **Gato atigrado** (amarillo con rayas, ojos verdes) o **Beagle**. Se guarda en `settings.json` → `Pet`.
- [x] Solo se anima mientras se ve (20 imágenes/s con `DispatcherTimer`), ampliado ×4 sin suavizar (`NearestNeighbor`). Medido en Debug: **2,5 %** de un núcleo con la pestaña a la vista, **0 %** con el notch cerrado.
- Las paletas de las mascotas están en Core junto a los dibujos: son parte del dibujo, no del tema (la regla de colores en `Themes/*.xaml` es para la interfaz).
- *Verificado por Claude (2026-10-29, `--fake`): las tres mascotas andan, golpean el ovillo, saltan y se giran; el cambio se guarda.*
- [x] Posturas nuevas: **dormir** (con «z»), **estirarse** (el beagle hace la reverencia de juego) y **lamerse** (el beagle jadea).
- [x] Juguetes a elegir: **ovillo** (frena pronto), **pelota** (bota y rueda lejos) y **ratón** (se escapa solo). Se guarda en `PetToy`.
- [x] Al pasar el ratón cerca, la mascota se gira hacia él y, si dormía, se despierta de un salto (como mucho una vez cada 2 s).
- [x] **Mascota en el notch cerrado:** sentada a la izquierda, imagen quieta (no gasta nada). Opción en la bandeja.
- [x] **Aviso de descanso** (`BreakReminder` en Core con 8 pruebas, `BreakService`, `BreakWindow`): cada **40 min** de uso la mascota cruza la parte de abajo de la pantalla, por encima de todo y sin robar el teclado, con «¡Hora de descansar!», **Ya descansé**, **5 min más** o Esc. Si dejas el PC 5 min cuenta como descanso (y cierra el aviso). Con una app en pantalla completa espera. Bandeja → «Recordar descansar»: cada 20/40/60 min, Nunca, o «Descansar ahora».
- *Verificado por Claude (2026-10-29, `--fake`, donde los minutos son segundos): el aviso aparece a los 40 s, la mascota cruza la pantalla, «5 min más» lo vuelve a mostrar, el notch cerrado muestra al gato. CPU: 1,6 % con el aviso abierto. **La persona lo vio y quedó conforme.** 229 pruebas.*

---

## Decisiones pendientes (la persona usuaria debe elegir)
- [x] Nombre definitivo: **NOX** (2026-10-27).
- [x] Editor: VS Code + C# Dev Kit.
- [ ] ¿Notas compartidas con Morrow (misma carpeta)?
- [ ] ¿Repositorio privado en GitHub? **Sí** (2026-10-27); falta instalar `gh` y `gh auth login` (pasos de la persona usuaria).

## Registro de sesiones
- **2026-10-29 — M11.** Dos monitores probados (100 %); escalado tomado del monitor principal; registro de errores que no cierra NOX; corregido el notch que se escondía con capas transparentes; barra de título oscura. 242 pruebas. Falta: escalado 125/150 % (lo cambia la persona) y el instalador 0.2.0.
- **2026-10-29 — M6d mezclador.** Volumen y silencio por app y cambio de salida de audio, desde «Ahora suena» → «Mezclador». 235 pruebas. Queda: M11 (monitores/escalados, logs, barra de título oscura en la ventana de atajos) e instalador 0.2.0.
- **2026-10-29 — M12 ampliado.** Aviso de descanso cada 40 min con la mascota por la pantalla, posturas nuevas, juguetes (ovillo, pelota, ratón), reacción al ratón y mascota en el notch cerrado. La persona lo aprobó. Queda: M6d (mezclador), M11 (monitores/escalados, logs) y un instalador nuevo (0.2.0).
- **2026-10-29 — M12 mascota.** Pestaña «Mascota» con gato gris (ojos amarillos), gato atigrado o beagle en pixel art propio, jugando con un ovillo y saltando. 199 pruebas. Queda: M6d (mezclador) y M11 (monitores/escalados, revisar logs).
- **2026-10-29 — M9b atajos globales.** Cinco atajos Ctrl+Alt configurables desde la bandeja, con aviso de conflictos. `settings.json` ya no escapa el «+» (`+`). 169 pruebas. Siguiente: M12 (mascota pixel art) o M6d (mezclador).
- **2026-10-29 — Cambios de alcance.** La persona quitó M6c (monitor del sistema) y la prueba con juegos en pantalla completa de M11; añadió M12 (mascota pixel art: gato gris oscuro de ojos amarillos, gato amarillo atigrado o beagle).
- **2026-10-29 — M6b keep-awake.** Opción «Mantener el PC despierto» en la bandeja (no se guarda entre aperturas). 148 pruebas. Falta que la persona la marque y vea que el PC no se suspende. Siguientes pendientes: M9b (atajos), M6d (mezclador), M11 (pruebas con monitores), M12 (mascota).
- **2026-10-26 — Planificación.** Se analizaron Kissa, Nock, Morrow y OrbitDrop. Se decidió: Programa 1 en C#/.NET 8/WPF, estilo minimalista blanco/negro con tema cambiable. Documentos creados. No hay código ni .NET instalado todavía. Siguiente paso: M0.
- **2026-10-26 — Investigación.** Se buscaron proyectos similares (ver `07-RESEARCH-SIMILAR-PROJECTS.md`). Cambios: se quitó la batería; se pasó a **.NET 10** (.NET 8 termina soporte en nov. 2026); se añadieron M2b, M3b, M6b, M6c, M6d, M9b; objetivo de RAM ≤150 MB. Instalar con `winget install Microsoft.DotNet.SDK.10`.
- **2026-10-27 — Clones y reparto.** Se estudiaron boring.notch, BetterLyrics, WindowsMediaController (MIT), Poricom y Clone-Wars (ver `07-RESEARCH…` §6). Se creó `../REPARTO-MODELOS.md` (qué modelo hace cada tarea). Pendiente: decidir si usar `Dubya.WindowsMediaController` o `Windows.Media.Control` directo. Siguiente paso: M0.
- **2026-10-27 — M0 terminado.** .NET SDK 10.0.403 instalado, VS Code + C# Dev Kit, telemetría de la CLI desactivada (`DOTNET_CLI_TELEMETRY_OPTOUT=1`). Los proyectos se movieron de OneDrive a `C:\Dev\NOX` y `C:\Dev\MOON` (OneDrive bloquea `bin/obj`). Repositorio git creado. Siguiente paso: M1.
- **2026-10-27 — M1 y decisión de música.** Esqueleto creado (4 proyectos, notch 220×34, bandeja, instancia única); compila sin avisos, 54 MB RAM. Falta la verificación visual de la persona usuaria para marcar M1. Música: opción A (YouTube Music + reproductor externo para carpetas locales, todo vía SMTC con `Windows.Media.Control`).
- **2026-10-27 — M2b y M3.** Estilos, modo invisible, Esc y franja reservada probados con UI Automation y capturas. M3 funciona con YouTube Music en Chrome (datos, carátula, botones). Errores encontrados en las pruebas y corregidos: carátula con el logo de Chrome, parpadeo de "Nada en reproducción" al cambiar de canción (Chrome cierra y reabre su sesión; ahora se esperan 1,5 s) y notch desplazado tras quitar la franja reservada (ahora se recoloca con `WM_SETTINGCHANGE`). Pendiente: probar música local con MusicBee. Siguiente: M3b.
- **2026-10-27 — M3b.** Varios reproductores: se vigila play/pausa de todos y se muestra el que suena; etiqueta para fijar uno. Hallazgo en pruebas: al abrir un reproductor nuevo Windows no siempre manda `SessionsChanged`, así que la lista también se rehace con `CurrentSessionChanged`. 45 pruebas. Siguiente: M4 (letras).
- **2026-10-27 — M4.** Letras de LRCLIB con caché en disco, pestañas Ahora suena / Letras, desfase ±5 s, importar .lrc, opción de privacidad en la bandeja. 99 pruebas. Verificado: búsqueda real (una canción solo tenía letra sin tiempos en LRCLIB, comprobado con su API) y letra sincronizada con `--fake` (avance y desfase). Pendiente de probar: letra no avanza en pausa, importar .lrc, desactivar letras.
- **2026-10-27 — M4, ajustes tras pruebas.** "La letra desaparece al pausar": no era un fallo; al pausar desde YouTube Music el ratón sale del notch y este se cierra. Se añadieron ⏮ ⏯ ⏭ en la pestaña Letras. Comprobado: en pausa la letra se queda quieta. Hallazgo: lrclib.net a veces responde 503 "ServerOverloaded"; ahora se reintenta (3 veces con Retry-After y luego hasta 3 veces cada 30 s, mostrando "reintento N de 3"). Letra sincronizada real vista en el notch. Falta: confirmar de oído que va al ritmo, importar .lrc y la casilla de privacidad.
- **2026-10-27 — M4 terminado.** La persona usuaria confirmó que la letra va al ritmo. Se quitó "Importar .lrc" (lo pidió: sobraba). 100 pruebas. Solo queda probar la casilla de privacidad "Buscar letras en internet". Siguiente: M5 (vinilo).
- **2026-10-27 — M5.** Pestaña Vinilo: disco con surcos y la carátula como etiqueta, gira solo con el notch abierto, la pestaña a la vista y la música sonando (pedido expreso de la persona usuaria). CPU medida: ~6 % de un núcleo girando, ~0,6 % cerrado. Siguiente: M6 (volumen y reloj) o M9 (tema) para completar el MVP.
- **2026-10-27 — M9. MVP completo (M0–M5 + M9).** Opciones guardadas en settings.json, tema Sistema/Oscuro/Claro en vivo, Iniciar con Windows. Sin ventana de ajustes: todo en el menú de la bandeja (más simple). Probado con UI Automation: cambio de tema, persistencia tras reiniciar, registro de arranque. 105 pruebas. Siguiente: M10 (empaquetado) o funciones extra (M6 volumen/reloj, M7 portapapeles, M8 notas).
- **2026-10-27 — M6, M7 y M8.** Volumen (dentro de Ahora suena) y reloj en el notch cerrado; Portapapeles (historial de 50, fijados en disco, ignora gestores de contraseñas); Notas (.md con Ctrl+Enter y "modo escritura"). Errores encontrados probando: (1) un token de ancho con el tipo equivocado hacía que la ventana no se creara y el error se perdía → ahora los errores se muestran y se registran; (2) al salir de una nota con Esc el teclado no volvía a tu ventana → se recuerda al abrir el notch. Un script de prueba hizo clic a ciegas cuando falló la app; ahora los scripts se detienen si no encuentran un botón. 128 pruebas.
- **2026-10-27 — M6b (parcial): Enfoque.** Temporizador de concentración con duraciones de un clic (15, 25, 30, 45 min, 1 h, 1 h 30); pausar/continuar/cancelar; cuenta atrás en el notch cerrado; al terminar, sonido y el notch se abre 6 s. Probado de punta a punta en `--fake` (tiene una duración extra de 10 s solo para pruebas). Sin cronómetro ni "mantener despierto" (no se pidieron). Añadido `.gitattributes`. 143 pruebas.
- **2026-10-27 — Renombrado a NOX (antes NotchBar).** Decisión de la persona usuaria antes de empaquetar. Cambió todo: carpeta `C:\Dev\NOX`, `Nox.sln`, proyectos `Nox.*`, ejecutable `NOX.exe`, datos en `%APPDATA%\NOX`, `%LOCALAPPDATA%\NOX` y `Documentos\NOX`. `LegacyData` traslada solas las carpetas viejas al arrancar (probado: ajustes, fijados y caché de letras trasladados). Icono nuevo: N blanca en círculo negro (`Assets/nox.ico`). Pendiente: copia privada en GitHub (falta instalar `gh` e iniciar sesión) y M10.
- **2026-10-27 — M10: instalador 0.1.0.** `dist\NOX-0.1.0-instalador.exe` (47,5 MB, .NET incluido, sin administrador). Probado instalar → abrir → desinstalar. Añadidos `THIRD-PARTY-NOTICES.md`, README de usuario y versión 0.1.0. La persona usuaria no quiere más funciones por ahora. Pendiente: probarlo en otro PC (lo hará la persona usuaria), escalado 125/150 % y dos monitores, juego a pantalla completa, MusicBee, GitHub.
