# NOX

Utilidad de escritorio para **Windows 10 (2004 o posterior) y Windows 11, de 64 bits**: una pastilla ("notch") minimalista en el borde superior de la pantalla que se abre al pasar el ratón. Hecha en **C# / .NET 10 / WPF**. Estilo minimalista en blanco o negro, con tema claro, oscuro o el de Windows.

Versión **0.2.0**.

## Qué hace

| Pestaña | Para qué |
|---|---|
| **Ahora suena** | Lo que suena en YouTube Music (navegador), Spotify, MusicBee… con carátula, ⏮ ⏯ ⏭, barra de progreso y volumen. Con varios reproductores abiertos, muestra el que suena (o el que fijes con la etiqueta de arriba a la derecha). El botón **Mezclador** abre el volumen de cada app y la salida de audio (con varias, un clic pasa a la siguiente). |
| **Letras** | Letra sincronizada de la canción (de lrclib.net), con ajuste de desfase. |
| **Vinilo** | La carátula como un disco que gira (solo mientras lo miras y suena música). |
| **Portapapeles** | Tus últimas 50 copias de texto; clic para volver a copiar, chinche para fijar. Ignora lo que copian los gestores de contraseñas. |
| **Notas** | Notas rápidas: Ctrl+Enter guarda un `.md` en `Documentos\NOX\notes`. |
| **Enfoque** | Temporizador de concentración (15 min – 1 h 30); avisa al terminar. |
| **Mascota** | Un gato gris de ojos amarillos en pixel art que juega, salta, se lame, se estira y duerme. Arriba eliges la mascota (gato gris, atigrado o beagle) y el juguete (ovillo, pelota o ratón). Pasa el ratón cerca y se gira hacia ti. |

**Descansos:** cada 40 minutos de uso, tu mascota cruza la parte de abajo de la pantalla para recordarte descansar («Ya descansé», «5 min más» o Esc). Se cambia en la bandeja → «Recordar descansar» (20, 40, 60 min o nunca). Si dejas el PC 5 minutos, cuenta como descanso.

Con el notch cerrado se ve el título de la canción, la cuenta atrás de la concentración o la hora. Se oculta solo cuando una app está en pantalla completa.

**Opciones** (clic derecho en el icono **N** de la bandeja): estilo (Notch, Cápsula, Indicador), visible solo al pasar el ratón, reservar la franja superior, buscar letras en internet, atajos de teclado, mantener el PC despierto, tema, carpeta de notas, iniciar con Windows.

**Atajos de teclado** (funcionan desde cualquier app; se cambian en «Atajos de teclado…»):

| Atajo | Hace |
|---|---|
| Ctrl+Alt+N | Abrir o cerrar el notch |
| Ctrl+Alt+P | Reproducir / pausar |
| Ctrl+Alt+, / Ctrl+Alt+. | Letra 0,5 s antes / después |
| Ctrl+Alt+0 | Letra sin desfase |

## Instalar

1. Ejecuta `NOX-0.2.0-instalador.exe`. No pide permisos de administrador: se instala solo para tu usuario.
2. Windows puede avisar "Windows protegió tu PC" porque el instalador no tiene firma digital (es normal en programas personales): pulsa **Más información → Ejecutar de todas formas**.
3. NOX aparece arriba al centro de la pantalla y en el menú Inicio. Para quitarlo: **Configuración → Aplicaciones → NOX → Desinstalar**.

## Privacidad

No hay cuentas ni telemetría. La única conexión a internet es a **lrclib.net** para las letras (envía título, artista, álbum y duración) y se puede desactivar. El historial del portapapeles vive en memoria; solo los textos fijados se guardan en disco.

Datos: `%APPDATA%\NOX` (ajustes y fijados), `%LOCALAPPDATA%\NOX` (caché de letras y registro de errores), `Documentos\NOX\notes` (notas).

## Para desarrollar

| Archivo | Para qué sirve |
|---|---|
| `AGENTS.md` | **Instrucciones para cualquier modelo de IA** que continúe el proyecto. Pásalo siempre primero. |
| `docs/01-VISION.md` | Qué hace la app y qué no. |
| `docs/02-ARCHITECTURE.md` | Tecnología, proyectos, carpetas, cómo funciona cada función por dentro. |
| `docs/03-ROADMAP.md` | Hitos con listas de tareas y registro de sesiones. |
| `docs/04-UI-THEME.md` | Diseño visual, colores, tamaños, cómo cambiar el estilo. |
| `docs/05-GLOSSARY.md` | Explicación de términos (XAML, MVVM, NuGet, SMTC...). |
| `docs/06-LICENSES-AND-RISKS.md` | Qué código se puede reutilizar y qué puede salir mal. |
| `docs/07-RESEARCH-SIMILAR-PROJECTS.md` | Proyectos parecidos y lecciones aprendidas. |
| `THIRD-PARTY-NOTICES.md` | Bibliotecas usadas y sus licencias. |

Comandos básicos (en `C:\Dev\NOX`):

```
dotnet build Nox.sln          # compilar
dotnet test Nox.sln           # pruebas
dotnet run --project src/Nox.App -- --fake   # probar con canción y letra de mentira
```

El instalador se genera con dos comandos: ver `installer/README.md`.

### Cómo retomarlo en otra sesión o con otro modelo

```
Vamos a continuar un proyecto en C# (.NET 10, WPF) para Windows. Lee primero
AGENTS.md y luego docs/03-ROADMAP.md. Dime en qué punto vamos y propón el
siguiente paso pequeño. Explica los términos nuevos con sencillez: tengo nivel medio.
```

NOX se llamó **NotchBar** hasta el 2026-10-27; al arrancar traslada solo los datos de esa versión.

## Programa 2

**MOON**, el lector de libros, cómics y manga, está en desarrollo en `../MOON/` (ver su README).
