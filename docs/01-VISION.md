# 01 — Visión

## En una frase
Una pastilla negra (o blanca) fija arriba al centro de la pantalla que, al pasar el ratón, se despliega y muestra lo que suena con letras sincronizadas y controles del sistema.

## Inspiración y qué tomamos de cada una

| App | Idea que tomamos | Idea que NO tomamos |
|---|---|---|
| **Nock** | Notch nativo, ligero, que se expande; pestañas con widgets; ahorro de recursos con eventos | Juegos, buddy animado, escenas, Wi-Fi/Bluetooth (v1) |
| **Kissa** | Leer la sesión multimedia de Windows; carátula; vinilo; letras sincronizadas (LRCLIB) | Salvapantallas, modos de visualización múltiples, código (licencia cerrada) |
| **Morrow** | Notas en archivos `.md` simples | Editor completo, bases de datos, grafo, sincronización |

## Versión 1 (MVP) — lo mínimo que debe funcionar
1. Pastilla arriba al centro, siempre visible, sin icono en la barra de tareas, con icono en la bandeja.
2. Se expande al pasar el ratón y se contrae al salir (animación suave).
3. **Ahora suena:** título, artista, carátula, play/pausa/anterior/siguiente, barra de progreso.
4. **Letras sincronizadas** de LRCLIB, con ajuste manual de desfase.
5. **Tema** claro / oscuro / sistema.
6. Arranque automático opcional con Windows.
7. Se oculta cuando hay una app en pantalla completa.

## Versión 1.1 y 1.2 (después del MVP, en orden)
- Vista de **vinilo** girando.
- **Volumen** y **reloj**. (Batería descartada: PC de escritorio.)
- **Historial del portapapeles** (con fijados).
- **Notas rápidas** (archivos `.md`).
- Ventana de **ajustes** completa.

## Ideas futuras (no planificadas aún)
Brillo, calendario, varios monitores, atajos globales, extensiones propias, instalador, Microsoft Store.

## Principios de diseño
- **Pequeña y silenciosa:** no molesta, no pide cuenta, no envía datos.
- **Una cosa bien hecha** antes que muchas a medias.
- **Minimalista:** blanco y negro, tipografía clara, mucho espacio, sin adornos innecesarios.
- **Cambiable:** el estilo se define en un solo lugar (ver `04-UI-THEME.md`).

## Criterio de éxito de la v1
Con YouTube Music abierto en Chrome o Edge, el notch muestra la canción y su letra sincronizada, los botones controlan la reproducción, y la app consume menos de ~150 MB de RAM y ~0 % de CPU en reposo (medir en el Administrador de tareas).
