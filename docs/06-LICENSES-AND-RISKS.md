# 06 — Licencias y riesgos

## Qué se puede reutilizar (verificado el 2026-10-26 en los repositorios)

| Proyecto | Licencia | ¿Copiar código? | ¿Mirar como referencia? |
|---|---|---|---|
| Nock | MIT | Sí, conservando el aviso de copyright | Sí |
| OrbitDrop | MIT | Sí, conservando el aviso | Sí |
| Morrow | Apache 2.0 | Sí, con aviso y nota de cambios | Sí |
| Vasta | AGPL-3.0 | No (obliga a publicar tu código si lo ofreces en red) | Solo ideas |
| **Kissa** | **Propietaria ("Kissa Licence")** desde la v4.1.0 | **No.** Prohíbe copiar, modificar y redistribuir | Solo la *idea* |

**Política del proyecto:** escribir código propio apoyándose en la documentación oficial de Microsoft. Si alguna vez se copia algo MIT/Apache, anotarlo en `THIRD-PARTY-NOTICES.md` (archivo, origen, licencia).

Los nombres, logos e iconos de esas apps **no** se usan. La app tiene su propio nombre y diseño.

## Dependencias y sus licencias
- CommunityToolkit.Mvvm — MIT
- Microsoft.Extensions.DependencyInjection — MIT
- NAudio — MIT
- H.NotifyIcon.Wpf — MIT
- xUnit — Apache 2.0
- LRCLIB (servicio web) — gratuito; revisar sus condiciones de uso actuales antes de publicar la app.

## Riesgos y plan B

| Riesgo | Probabilidad | Efecto | Plan B |
|---|---|---|---|
| Ventana transparente de WPF con bajo rendimiento | Media | Animación con tirones | Ventana opaca con esquinas por `SetWindowRgn`; o reducir el área transparente |
| YouTube Music en navegador no expone la sesión multimedia | Baja | No se ve la canción | Verificar en M3; Chrome/Edge suelen exponer SMTC. Alternativa futura: extensión de navegador (fuera de la v1) |
| Algunos reproductores no permiten saltar a una posición | Media | Barra de progreso solo de lectura | Desactivar el arrastre si `TryChangePlaybackPosition` falla |
| LRCLIB sin letra o con otra versión | Alta | Sin letras | Mostrar "Sin letra sincronizada"; validación de duración; importar `.lrc` (futuro) |
| Portapapeles bloqueado por otra app | Media | Fallo al leer | Reintentos con pausa corta (3 intentos) |
| Falsos positivos de antivirus / SmartScreen en el instalador sin firma | Alta | Aviso al instalar | Normal en proyectos personales; firma de código es opcional y de pago |
| Pantalla completa mal detectada | Media | Notch visible sobre un juego | Ajuste "No mostrar sobre estas apps" (futuro) |
| Contenido sensible del portapapeles | Media | Privacidad | Ignorar formatos de gestores de contraseñas; no registrar contenido; solo fijados en disco; opción de pausar el historial |
| Alcance demasiado grande | Alta | Proyecto sin terminar | Seguir el MVP: M0–M4 primero |
| Cambiar la salida de audio usa `IPolicyConfig`, interfaz **no documentada** de Windows (M6d) | Baja (la usan Windows, EarTrumpet y SoundSwitch desde Windows 7) | El botón de salida deja de funcionar | Si Windows la rechaza, NOX muestra un aviso y la salida se cambia desde el icono de sonido de la barra de tareas; el resto del mezclador sigue funcionando |

## Privacidad
- Sin telemetría, sin cuentas, sin anuncios.
- Única conexión de red: LRCLIB (artista, título, álbum, duración). Desactivable.
- Todo lo demás en `%APPDATA%` / `%LOCALAPPDATA%` / Documentos.
