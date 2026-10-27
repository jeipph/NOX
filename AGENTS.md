# AGENTS.md — instrucciones para modelos de IA

Lee este archivo completo antes de tocar nada.

## Proyecto
App de escritorio Windows 10/11 llamada provisionalmente **NotchBar**: pastilla minimalista arriba al centro de la pantalla, se expande al pasar el ratón y muestra widgets (música, letras, volumen, portapapeles, notas rápidas, reloj). Tema claro/oscuro/sistema. Documentación en `docs/`.

## Persona usuaria
- Nivel de programación **medio**, con dudas en pasos y términos. Idioma: **español**.
- Explica cada concepto nuevo en una o dos frases (ver `docs/05-GLOSSARY.md`) y añade términos nuevos allí.
- Un paso pequeño por vez. Muestra el comando exacto y qué resultado debe verse.
- Pregunta antes de decisiones que cambien la arquitectura.

## Reglas técnicas (obligatorias)
1. **Lenguaje y plataforma:** C# 14, **.NET 10 (LTS)**, WPF. TFM de los proyectos con Windows: `net10.0-windows10.0.19041.0`. (Se descartó .NET 8 porque su soporte termina en nov. 2026.)
2. **Patrón:** MVVM con `CommunityToolkit.Mvvm` e inyección de dependencias (`Microsoft.Extensions.DependencyInjection`).
3. **Separación de proyectos:**
   - `NotchBar.Core`: lógica pura y interfaces. **Sin referencias a WPF ni a APIs de Windows.** Debe poder probarse con xUnit.
   - `NotchBar.Windows`: implementaciones que usan APIs de Windows (SMTC, audio, portapapeles, P/Invoke).
   - `NotchBar.App`: WPF (ventanas, vistas, ViewModels, temas).
4. **Nada de código ajeno copiado.** Kissa tiene licencia que lo prohíbe. Nock y OrbitDrop son MIT: se pueden consultar como referencia, pero escribe código propio; si copias algo, conserva el aviso de copyright y anótalo en `THIRD-PARTY-NOTICES.md`.
5. **Privacidad:** sin telemetría, sin cuentas. La única red permitida es LRCLIB para letras (ver arquitectura). Todo lo demás es local.
6. **Rendimiento:** la app corre siempre; en reposo no debe usar CPU apreciable. Sin bucles de sondeo: usa eventos. Temporizadores solo mientras hay animación.
7. **Colores y tamaños** solo desde diccionarios de recursos (`Themes/*.xaml`). Prohibido poner colores fijos en las vistas.
8. **Dependencias NuGet:** solo las listadas en `docs/02-ARCHITECTURE.md`. Para añadir otra, pregunta y verifica que su licencia sea permisiva (MIT/Apache/BSD).
9. **Seguridad:** no registrar contenido del portapapeles en logs. Ignorar copias que vengan de gestores de contraseñas (formato `ExcludeClipboardContentFromMonitorProcessing`).
10. **Pruebas:** toda lógica nueva en Core lleva pruebas xUnit. Antes de dar un hito por terminado, `dotnet build` y `dotnet test` deben pasar sin errores.

## Flujo de trabajo
1. Abre `docs/03-ROADMAP.md`, busca el primer hito con casillas sin marcar.
2. Propón un plan de 3–6 pasos pequeños para ese hito y espera confirmación.
3. Implementa, compila, prueba. Cuando la persona confirme que funciona, marca las casillas `[x]` en el ROADMAP y haz commit.
4. Si descubres algo que contradice la documentación, **actualiza el documento** y dilo.
5. Al terminar la sesión, añade un resumen de 3–5 líneas en la sección "Registro de sesiones" al final de `docs/03-ROADMAP.md`.

## Git
- Un commit por hito o subtarea terminada. Mensajes en español, imperativo corto ("Añade lectura de sesión multimedia").
- No uses `--force`, ni borres historial.

## Estructura objetivo
Ver `docs/02-ARCHITECTURE.md`, sección "Estructura de carpetas".

## Fuera de alcance de la v1
Conversión de archivos, editor de notas completo, juegos, Bluetooth/Wi-Fi, soporte para varios monitores avanzado, tienda de Microsoft. Ver `docs/01-VISION.md`.
