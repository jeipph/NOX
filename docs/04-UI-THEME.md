# 04 — Diseño visual y temas

## Estilo
Minimalista, blanco o negro, mucho espacio, tipografía limpia, sin degradados ni sombras exageradas. Esquinas muy redondeadas. Todo el aspecto se controla desde **un solo sitio**: los tokens de `Themes/Tokens.*.xaml`. Cambiar el estilo = cambiar esos valores (o añadir un archivo de tokens nuevo).

## Tokens (valores iniciales)

| Token | Oscuro | Claro | Uso |
|---|---|---|---|
| `Color.Background` | `#000000` | `#FFFFFF` | Fondo del notch |
| `Color.Surface` | `#141414` | `#F2F2F2` | Tarjetas, controles |
| `Color.SurfaceHover` | `#1F1F1F` | `#E8E8E8` | Hover |
| `Color.Border` | `#2A2A2A` | `#DADADA` | Líneas finas |
| `Color.Text` | `#F5F5F5` | `#111111` | Texto principal |
| `Color.TextMuted` | `#8A8A8A` | `#6B6B6B` | Texto secundario, letras no activas |
| `Color.Accent` | `#FFFFFF` | `#000000` | Botón principal, línea de letra activa, progreso |
| `Color.Danger` | `#FF5A5A` | `#D32F2F` | Borrar, error |

Regla: el acento es el mismo blanco/negro del texto (monocromo). Si más adelante se quiere un color, se cambia **solo** `Color.Accent`.

## Medidas

| Token | Valor |
|---|---|
| `Size.Collapsed` | 220 × 34 px |
| `Size.Expanded` | 560 × 200 px |
| `Radius.Notch` | 18 px (contraído), 26 px (expandido) |
| `Radius.Control` | 10 px |
| `Space.1/2/3/4` | 4 / 8 / 12 / 20 px |
| `Duration.Expand` | 220 ms (`CubicEase`, EaseOut) |
| `Duration.Fast` | 120 ms |
| `Hover.CollapseDelay` | 400 ms |

## Tipografía
- Principal: **Segoe UI Variable** (viene con Windows 11; en Windows 10 cae a Segoe UI).
- Tamaños: título de canción 15 px semibold; artista 12 px; letras activas 18 px; letras apagadas 14 px; microtexto 11 px.
- Opcional más adelante: incluir una fuente propia (ej. Inter, licencia OFL) dentro de `Assets/`.

## Estados del notch
1. **Contraído:** pastilla con (a) reloj o (b) texto de la canción en reproducción + puntitos de ecualizador (opcional). Nada más.
2. **Expandido:** barra de pestañas inferior con iconos: ♪ Ahora, 𝄞 Letras, ◎ Vinilo, 📋 Portapapeles, ✎ Notas, ⚙ Ajustes. Solo aparecen los módulos activos.
3. **Oculto:** cuando hay pantalla completa.

## Cómo se cambia el tema (técnica)
- `Themes/Tokens.Dark.xaml` y `Tokens.Light.xaml` definen **solo colores**, con los mismos nombres de recurso (ej. `Brush.Background`). Medidas, tiempos y tipografía están en `Tokens.Layout.xaml`, que es común a todos los temas.
- Estilos de la pastilla (M2b): **Notch** (`Size.Collapsed`, `Radius.Notch.*`), **Cápsula** (`Radius.Capsule.*`, `Margin.Capsule` = 8 px desde arriba) e **Indicador** (`Size.Indicator` = 60 × 6 px). En modo "solo al pasar el ratón" el fondo contraído es `Brush.HitArea` (alfa 1/255: invisible pero detecta el ratón).
- Las vistas usan **siempre** `{DynamicResource Brush.Background}` (DynamicResource, no StaticResource, para que cambie en vivo).
- `ThemeService` reemplaza el diccionario activo en `Application.Current.Resources.MergedDictionaries`.
- Tema "Sistema": leer `AppsUseLightTheme` del registro y escuchar `SystemEvents.UserPreferenceChanged`.

## Futuro: más estilos
Añadir un archivo `Tokens.<Nombre>.xaml` y una entrada en el selector de Ajustes. Ideas: "Papel" (marfil + tinta), "Alto contraste", "Cristal" (transparencia, requiere cuidado con el rendimiento).

## Accesibilidad básica
- Contraste mínimo 4.5:1 entre `Text` y `Background` (los valores iniciales lo cumplen).
- Todo botón con `AutomationProperties.Name`.
- Tamaño de clic mínimo 28×28 px.
