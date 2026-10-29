# 07 — Investigación de proyectos similares

Fecha: 2026-10-26. Fuente: búsqueda en GitHub (API) y lectura de los README. Lo que no pudo confirmarse se marca como **(no confirmado)**. Las estrellas son orientativas.

## 1. Proyectos encontrados

### Referencias principales (C#/WPF, licencia permisiva → se pueden consultar y, con aviso, reutilizar)

| Proyecto | ★ | Tecnología | Licencia | Qué aporta |
|---|---|---|---|---|
| **nazimferragh/windows-dynamic-island** | 3 | C# .NET 9 + WPF | MIT | **La mejor guía técnica.** Un notch por monitor, sin foco, oculto de Alt+Tab, reserva de franja superior, detección de pantalla completa, "estante de ventanas". Métricas propias: ~1 % de un núcleo, ~200 MB RAM con 3 monitores. |
| **BillyNabil/nabyscape** | 141 | C# .NET 10 + WPF | MIT | La más completa: música (selección de fuente, cambio de salida de audio, mezclador por app, letras, forma de onda, color según la portada), enfoque (temporizador, cronómetro, keep-awake), clima, notas, tareas, estante de archivos, portapapeles, calendario, limpiador de enlaces, asistente de IA. Instalador sin firma. |
| **MoFeng2223/BrimDeck** | 150 | C# WPF | Apache-2.0 | Tres estilos de ventana (**Notch / Capsule / Indicator**), se abre al pasar el puntero y se cierra con Esc, se oculta en pantallas completas, temas personalizados, monitor del sistema (CPU, GPU, RAM, red), avisos de cuota. |
| **ademerci/Holm** | 8 | C# | MIT | Notch con medios, descargas, mensajes y compartir archivos. (Menciona batería; la omitimos.) No se leyó a fondo. |

### Referencias de ideas (licencia restrictiva → solo mirar la *idea*, no copiar código)

| Proyecto | ★ | Licencia | Por qué importa |
|---|---|---|---|
| **BochengYao/LyricIsland** (en su README se llama *LyricHover*) | 7 | GPL-3.0 | Isla de **letras**: varias fuentes con respaldo (LRCLIB → QQ Música → KuGou → Netease), varios reproductores, traducciones, atajos para ajustar el desfase de 500 ms. Arquitectura `App` + `Core` + `Tests`, igual que nuestro plan. |
| **WinIslandProject/WinIsland** | 381 | GPL-3.0 | Rust. Isla minimalista. |
| **JNTMTMTM/eIsland** | 322 | GPL-3.0 | Electron. |
| **DynamicWin** (FlorianButz) | 638 | CC-BY-SA-4.0 (el repositorio solo contiene releases; **no hay código fuente**) | Muy popular. Controles de medios, calendario de Google, bandeja de archivos, vistas por scroll. Quitó la función "agitar un archivo" por **problemas graves de rendimiento**. |
| **lucacadalora/oyenza-studio** (MIT, Rust) | 87 | MIT | Música con letras, Pomodoro, tareas, portapapeles, estante de archivos, temas (Classic / Glass / Pill), modo `--selftest` con datos falsos. Ideas excelentes de diseño. |
| Otros menores | — | — | `qnotch`, `Dynamic-Edge` (WinUI 3), `TopTaskBarWin11` (barra superior estilo macOS), `pillar-dynamic-island-for-windows` (Electron), `Ripple` (multiplataforma). |

**Conclusión:** el nicho está ocupado, pero no por una app *minimalista, blanco/negro, ligera y centrada en música + letras*. Nuestro diferencial es **simpleza y ligereza**, no tener más funciones.

## 2. Qué trae el conjunto de proyectos (mapa de funciones)

| Función | ¿Cuántos la tienen? | ¿La planeamos? |
|---|---|---|
| Controles de medios + carátula | Todos | Sí (M3) |
| Letras sincronizadas | nabyscape, LyricIsland, BrimDeck, Oyenza | Sí (M4) |
| Varias fuentes de letras con respaldo | LyricIsland | **Parcial:** LRCLIB + importar `.lrc` + caché. Otras fuentes usan APIs no oficiales (riesgo). |
| Elegir entre varios reproductores | LyricIsland, nabyscape, BrimDeck | **Añadir (M3b)** |
| Notch por monitor | nazim | Posponer (v2) |
| Estilos Notch / Capsule / Indicator | BrimDeck | **Añadir (M2b)** |
| Se oculta en pantalla completa | Varios | Sí (M2) |
| Reservar franja superior (como barra de tareas) | nazim | Opcional, **no** por defecto |
| Modo "visible solo al pasar el ratón" | Oyenza | **Añadir (M2b)** |
| Pomodoro / temporizador de enfoque | nabyscape, Oyenza | **Añadir (M6b)**, fácil |
| Keep-awake (evitar suspensión) | nabyscape | **Añadir (M6b)**, muy fácil |
| Monitor del sistema (CPU, RAM, red) | BrimDeck, Oyenza | ~~Añadir (M6c)~~ Descartado por la persona (2026-10-29) |
| Mezclador de volumen por app y cambio de salida de audio | nabyscape | **Añadir (M6d)** con NAudio |
| Estante de archivos (file shelf) | nabyscape, Oyenza, DynamicWin | Idea para v2 / Programa 2 |
| Portapapeles | nabyscape, Oyenza | Sí (M7) |
| Notas, tareas | nabyscape | Notas sí (M8); tareas v2 |
| Calendario, clima | nabyscape, DynamicWin | v2 |
| Color de acento según la portada | nazim, nabyscape | Opcional; choca con el estilo monocromo → solo como ajuste |
| Atajos globales | LyricIsland | **Añadir (M9b)** |
| Autoactualización | DynamicWin, nabyscape | v2 |
| Asistente de IA integrado | nabyscape | No (fuera del estilo minimalista) |
| Batería | Varios | **Descartada** (PC de escritorio) |

## 3. Lecciones y trampas (lo que nos podemos encontrar mañana)

1. **Rendimiento con juegos y pantallas completas.** DynamicWin eliminó una función por rendimiento; nazim desactiva animaciones, baja la frecuencia de comprobaciones y reduce la prioridad del proceso en pantalla completa. → Nuestro plan: ocultar el notch y **detener** temporizadores/animaciones cuando no está visible.
2. **Hooks globales solo cuando hacen falta.** nazim instala el hook de ratón de bajo nivel únicamente durante un arrastre. → Evitar `WH_MOUSE_LL` permanente.
3. **Una ventana por monitor** y tamaño según el escalado DPI de cada uno (si algún día hay varios monitores).
4. **La ventana no debe tomar el foco** ni aparecer en Alt+Tab ni en la barra de tareas (`WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`). Ya está en el plan.
5. **Mantener la ventana pequeña cuando está contraída** para reducir el redibujado. Ya está en el plan.
6. **Las letras no están en todas las canciones.** nabyscape lo dice explícitamente: "una sesión compatible no garantiza que la canción tenga letras". → Mostrar estado "Sin letra" sin molestar; permitir importar `.lrc`.
7. **No todos los reproductores permiten saltar la posición.** (Ya en riesgos.) LyricIsland reporta que en Netease arrastrar la barra dentro del reproductor no sincroniza las letras en tiempo real.
8. **Apple Music/Spotify:** extraer letras de APIs privadas es frágil. Oyenza avisa que las letras de Apple pueden dejar de funcionar si Apple cambia su sitio. → Nos quedamos con LRCLIB (pública).
9. **Instaladores sin firma → avisos de SmartScreen.** nabyscape y Oyenza lo documentan. nazim usa firma gratuita para proyectos de código abierto con **SignPath** + GitHub Actions (investigar en M10; no confirmado que aplique a nuestro caso).
10. **Windows 10 sin probar:** nabyscape solo probó Windows 11. Tú tienes Windows 11, así que está bien.
11. **Modo de autoprueba (idea de Oyenza):** `poni.exe --selftest` recorre todos los estados con datos falsos y genera capturas. → Para nosotros: una implementación `FakeMediaService` (canción de mentira con letra) que permita probar la interfaz sin abrir YouTube Music. **Hacerlo en M3**; ahorra mucho tiempo.
12. **Consumo realista de memoria.** nazim reporta ~200 MB con 3 monitores. Nuestro objetivo de "<100 MB" puede ser demasiado ajustado para WPF con ventana transparente. → Nuevo objetivo: **≤150 MB y ~0 % CPU en reposo con 1 monitor** (medir en M11).
13. **Licencias engañosas.** DynamicWin usa CC BY-SA 4.0 (licencia de contenido, no de software) y no publica código; Holm y otros se declaran "vibe coded". No tomar código de proyectos sin revisar su `LICENSE`.
14. **.NET 8 deja de tener soporte en noviembre de 2026** → el plan usa **.NET 10 (LTS)**.

## 4. Cambios aplicados al plan
- Se quitó la **batería** (PC de escritorio) de la visión, arquitectura y roadmap.
- Se cambió a **.NET 10**.
- Se añadieron al roadmap los hitos nuevos (ver `03-ROADMAP.md`): M2b, M3b, M6b, M6c, M6d, M9b.
- El objetivo de rendimiento pasa a ≤150 MB RAM y ~0 % CPU en reposo.

## 5. Enlaces
- nazimferragh/windows-dynamic-island — https://github.com/nazimferragh/windows-dynamic-island
- BillyNabil/nabyscape — https://github.com/BillyNabil/nabyscape
- MoFeng2223/BrimDeck — https://github.com/MoFeng2223/BrimDeck
- BochengYao/LyricIsland — https://github.com/BochengYao/LyricIsland
- lucacadalora/oyenza-studio — https://github.com/lucacadalora/oyenza-studio
- FlorianButz/DynamicWin — https://github.com/FlorianButz/DynamicWin
- ademerci/Holm — https://github.com/ademerci/Holm
- LRCLIB — https://lrclib.net/docs

## 6. Segunda ronda (2026-10-27): clones y cómo funcionan por dentro

| Proyecto | Licencia | Qué aprendemos |
|---|---|---|
| **TheBoredTeam/boring.notch** (clon libre de NotchNook, macOS, Swift) | GPL-3.0 → solo ideas | Separa el **estado del notch por pantalla** (abierto/cerrado, tamaño, hover) en un ViewModel propio; el "ahora suena" vive en un **proceso/adaptador aparte** (`mediaremote-adapter`, `XPCHelper`). Idea para nosotros: un `NotchStateViewModel` con estados `Cerrado / Hover / Abierto` y la fuente de medios detrás de una interfaz (`IMediaSource`) para poder cambiarla o simularla en pruebas. |
| **jayfunc/BetterLyrics** (C#, WinUI 3) | GPL-3.0 → solo ideas | Lee la música con **SMTC** a través de `Dubya.WindowsMediaController`; letras de LRCLIB y otras fuentes con un **sistema de plugins de proveedores**. Idea: `ILyricsProvider` con una lista ordenada de proveedores (LRCLIB → archivo `.lrc` local → caché). |
| **DubyaDude/WindowsMediaController** | **MIT** | Envoltorio de SMTC con eventos (`OnFocusedSessionChanged`, `OnAnyMediaPropertyChanged`, `OnAnyPlaybackStateChanged`, `OnAnyTimelinePropertyChanged`). Encaja con la regla "sin sondeo, solo eventos". **Pendiente de decisión:** usarlo como NuGet o escribir lo mismo directamente con `Windows.Media.Control` (plan actual, sin dependencia extra). |
| **GorvGoyl/Clone-Wars** | — | Lista de 100+ clones de apps famosas (casi todo web). Útil como método: los buenos clones copian **la experiencia** (flujo, animación, atajos) y escriben el código desde cero. Es justo nuestra regla 4. |

**Cómo funcionan casi todos los clones de "isla/notch":** ventana sin bordes, siempre encima, que no toma el foco → escucha eventos del sistema (medios, volumen, portapapeles) → un estado central decide el tamaño → animación solo durante la transición. Nada de esto requiere copiar código.
