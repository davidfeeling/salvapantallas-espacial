# Cosmos .NET — Salvapantallas Espacial 3D

Salvapantallas interactivo y procedural para Windows desarrollado en **.NET 9 (WPF + SkiaSharp)** con estética espacial moderna inspirada en la pantalla inicial y los viajes hiperespaciales de *No Man's Sky*.

---

## 🚀 Características Principales

* **Vuelo Espacial 3D en Tiempo Real**: Proyección matemática en perspectiva tridimensional hacia adelante, con balanceo suave de cabina y estelas de velocidad de hiperespacio (*Warp trails*).
* **Sistemas Solares Completos**: Soles brillantes con corona, rodeados de planetas con iluminación día/noche según su sol, gigantes gaseosos con anillos y lunas orbitando.
* **Agujeros Negros con Lente Gravitacional**: Deformación de la luz de fondo según la fórmula de Einstein, curvando y estirando en arcos de luz las estrellas y galaxias que pasan detrás.
* **Galaxias y Nebulosas Procedurales**: Galaxias espirales (con brazos logarítmicos y regiones de formación estelar H-II), galaxias elípticas ancestrales y nebulosas volumétricas con mezcla de luz aditiva.
* **Panel Holográfico de Cabina Sci-Fi**:
  * Reloj vectorial futurista de gran formato pensado para pantallas de salón y televisores.
  * Radar 3D de proximidad en vivo y telemetría de potencia Warp.
  * **Protección Anti-Burn-in**: Flota continuamente y se traslada automáticamente entre las cuatro esquinas de la pantalla cada 40 segundos para evitar quemados en paneles OLED y televisores.
* **Panel de Configuración Completo (`/c`)**:
  * Visor en vivo (*Live Preview*) interactivo a 60 FPS con botón para regenerar la ruta.
  * Toggles para activar/desactivar cada tipo de objeto de forma independiente.
  * Control total de densidades (estrellas, sistemas solares, galaxias, nebulosas, etc.).
  * Ajustes de velocidad de simulación, impulso Warp, resplandor óptico (*Glow/Bloom*) y 4 paletas de color (*Astrofísica Realista JWST*, *Cyberpunk Neón*, *Azul Abisal*, *Oro Solar*).
* **Instalador Integrado (`CosmosInstaller.exe`)**:
  * Instala y activa el protector de pantalla en Windows en un clic.
  * Configura el tiempo de espera en el registro del sistema y crea accesos directos.
  * Permite abrir el configurador o probar el salvapantallas a pantalla completa.

---

## 🛠️ Tecnologías

* **.NET 9.0 (`net9.0-windows`)**
* **C# 13**
* **WPF** para integración con ventanas Win32 (`HWND`), soporte multi-monitor y panel de configuración.
* **SkiaSharp** para el motor de renderizado acelerado con shaders radiales, blends aditivos y filtros ópticos.

---

## 📦 Compilación y Empaquetado

Para compilar y generar los binarios listos para usar (`CosmosScreensaver.scr` y `CosmosInstaller.exe` en la carpeta `dist/`):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1
```

---

## 💻 Protocolo de Salvapantallas de Windows

* `CosmosScreensaver.scr /s`: Ejecución a pantalla completa en todos los monitores detectados.
* `CosmosScreensaver.scr /c`: Abre la ventana de configuración con vista previa en tiempo real.
* `CosmosScreensaver.scr /p <HWND>`: Modo de vista previa en miniatura para el cuadro de diálogo clásico de Windows (`desk.cpl`).
