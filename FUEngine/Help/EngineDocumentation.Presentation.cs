using System.Collections.Generic;

namespace FUEngine.Help;

/// <summary>Subtítulos y bloque «En FUEngine» para cada tema del manual general (no Lua).</summary>
public static partial class EngineDocumentation
{
    private const string FallbackSubtitle = "Guía del editor y runtime FUEngine";
    private const string FallbackEnMotor =
        "FUEngine no es un IDE Lua genérico ni Unity/Godot: el proyecto vive en una carpeta con JSON (escena, mapa, objetos, scripts registrados) y Play ejecuta NLua con las tablas documentadas (world, self, input, time, …). Si algo no coincide con tutoriales externos, prioriza este manual y las rutas de tu proyecto.";

    private static DocumentationTopic ApplyManualPresentation(DocumentationTopic t)
    {
        if (!ManualPresentation.TryGetValue(t.Id, out var meta))
            meta = (FallbackSubtitle, FallbackEnMotor);
        return new DocumentationTopic(
            t.Id,
            t.Title,
            t.ParaQue,
            t.PorQueImporta,
            t.Paragraphs,
            t.Bullets,
            meta.Subtitle,
            meta.EnMotor,
            t.LuaExampleCode,
            t.ExampleCategory,
            t.SuggestedExportFileName,
            t.ExampleSearchTags,
            t.ExampleDifficulty);
    }

    /// <summary>Clave = <see cref="DocumentationTopic.Id"/> del manual general.</summary>
    private static readonly Dictionary<string, (string Subtitle, string EnMotor)> ManualPresentation = new(StringComparer.Ordinal)
    {
        [QuickStartTopicId] = (
            "Primeros pasos: Hub, proyecto y Play",
            "Desde la pantalla de inicio abres o creas un proyecto; no hay un «solución» aparte: todo es la carpeta del proyecto + FUEngine.exe. Play embebido usa el mismo runtime que exportación; los scripts deben estar en scripts.json y enlazados en el inspector."),
        ["crear-juego"] = (
            "Datos, mapa y Lua en este repo",
            "Aquí un juego es escenas + mapa por chunks + objetos JSON + Lua en runtime; el editor WPF no ejecuta tu lógica hasta Play. world.setTile, self y física son APIs de este motor, no de otro entorno."),
        ["arquitectura"] = (
            "Ensamblados Core, Editor, Runtime, Vulkan",
            "Al depurar recuerda: serialización JSON en Editor, tilemap y entidades en Core, Lua y bucle en Runtime, Vulkan solo en la ventana de juego. No mezcles responsabilidades de otros motores."),
        ["compilar-desde-fuente"] = (
            "Build con publish y .NET 8",
            "El instalador o dotnet publish generan un FUEngine.exe autocontenido; no confundas con solo abrir un .exe ya construido. Quien compila el motor necesita SDK 8; quien instala el setup no."),
        ["layout-editor"] = (
            "Mapa, Inspector, consola y juego embebido",
            "Los paneles son fijos en este editor; Ver → muestra u oculta. No hay dock como VS Code: el flujo es mapa + selección + inspector documentado aquí."),
        ["pestañas"] = (
            "Mapa, Juego, Scripts y pestañas +",
            "Las pestañas centrales son el contrato de trabajo del motor; añadir Creative Suite o Tiles no cambia el modelo de escena JSON. Play usa la pestaña Juego o el menú Proyecto."),
        ["menus-principales"] = (
            "Archivo, Editar, Proyecto, Ayuda…",
            "Los menús disparan comandos sobre el proyecto actual; no hay proyecto global fuera de la carpeta .FUE. Ayuda y Proyecto enlazan la misma documentación integrada."),
        ["mapa-herramientas"] = (
            "Pintar tiles y herramientas de mapa",
            "Las herramientas escriben en el mapa serializado en disco; el runtime en Play lee los mismos chunks. TileData y catálogo son específicos de FUEngine."),
        ["jerarquia-explorador"] = (
            "Árbol de escena vs archivos en disco",
            "La jerarquía refleja nodos de la escena; el explorador muestra carpetas reales. Renombrar en un sitio no siempre equivale al otro: sigue las reglas del inspector."),
        ["menus-contextuales"] = (
            "Clic derecho en mapa y nodos",
            "Las acciones contextuales modifican datos del proyecto (JSON) y undo/redo del editor; no asumas menús equivalentes en otro IDE."),
        ["inspector"] = (
            "Propiedades del objeto o recurso seleccionado",
            "El inspector escribe en instancias y a veces en el proyecto; lo que ves en Play depende de lo guardado en objetos.json y escena."),
        ["objetos-componentes"] = (
            "GameObject, transform y lista de componentes",
            "Los componentes disponibles y los nombres en JSON son los de FUEngine.Core; no hay prefabs Unity: hay seeds y definiciones en este proyecto."),
        ["componentes-json-play"] = (
            "De objetos.json a componentes en Play",
            "Collider, ParticleEmitter, etc. se instancian en el runtime según el esquema del motor; no todas las propiedades son mutables por Lua (ver ComponentProxy)."),
        ["triggers"] = (
            "Zonas de evento en mapa y en objetos",
            "Los triggers se serializan en triggerZones.json o en el objeto; el runtime ejecuta hooks según este motor, no un sistema de señales genérico."),
        ["seeds"] = (
            "Prefabs reutilizables en disco",
            "Los seeds son JSON de instancia del proyecto; colocarlos en mapa usa el flujo de editor documentado, no un «prefab» de Unity."),
        ["scripting-lua"] = (
            "NLua, hooks y tablas globales expuestas",
            "Los scripts se cargan en orden definido por el proyecto; self, world, input, time son proxies de esta versión del runtime. Revisa el tema de APIs completo para firmas exactas."),
        ["editor-mini-ide-lua"] = (
            "AvalonEdit, resaltado y Ctrl+Espacio",
            "El mini-IDE solo conoce el catálogo de completions del motor y archivos del proyecto; no es Language Server Protocol completo."),
        ["play-runtime"] = (
            "Bucle, Lua y render Vulkan",
            "Play en editor usa el mismo runtime que el juego exportado; la consola de Lua y errores son los de FUEngine, no una consola externa."),
        ["fisica"] = (
            "Escena física en Play",
            "Raycast de mapa vs physics.raycast tienen geometrías distintas documentadas aquí; no copies comportamiento de Box2D/Unity sin leer."),
        ["chunks-streaming"] = (
            "Chunks, streaming y caché",
            "El streaming de chunks es por proyecto; flags de runtime y distancias son del motor, no de un plugin externo."),
        ["iluminacion-audio-ui"] = (
            "Luces WPF, audio NAudio, UI canvas",
            "El editor muestra iluminación aproximada en mapa; el juego usa Vulkan; audio pasa por manifiesto audio.json del proyecto."),
        ["animaciones-export"] = (
            "Animaciones y pipeline de exportación",
            "Las rutas y formatos son los que serializa FUEngine.Editor; la exportación build no es un export genérico de Unity."),
        ["archivos-json"] = (
            "JSON del proyecto y convenciones",
            "Los nombres de campos siguen camelCase del proyecto; no edites a mano sin conocer el esquema descrito en este manual."),
        ["render-pixel-art-filtros"] = (
            "Pixel art, filtros y Vulkan",
            "Las recomendaciones de muestreo aplican al pipeline de este motor; otro motor puede tener otros defaults de AA."),
        ["eventos-hooks-lua"] = (
            "onUpdate, onStart y callbacks",
            "Los nombres de hooks deben existir como globales para NLua; el orden de llamada es el del motor, no configurable como en un framework genérico."),
        ["depuracion-y-consola"] = (
            "Consola del editor y filtros",
            "Los niveles y categorías son la consola integrada; print() de Lua llega ahí en Play del editor, no a stdout de un IDE externo."),
        ["seeds-prefabs-runtime"] = (
            "Instanciar seeds en tiempo de ejecución",
            "game.instantiate y world siguen las reglas del motor; rutas y IDs son los del proyecto actual."),
        ["scripts-capa-layer"] = (
            "Scripts de capa y layer.*",
            "Las capas con LayerScriptId ejecutan Lua con tabla layer; no hay self. Es un modo distinto al script de objeto."),
        ["ads-simulado"] = (
            "Ads en Play y simulación",
            "La API ads es la inyectada en runtime; en editor usa implementación simulada según este proyecto."),
        ["input-raton-teclado"] = (
            "input.* y constantes Key",
            "Las constantes Key y el mapeo son los del motor; no asumas códigos SDL/GLFW crudos en Lua."),
        ["tiempo-y-delta"] = (
            "time.delta, frame y seconds",
            "time.delta es el del bucle de juego FUEngine; no sustituye un Time.timeScale global de Unity."),
        ["escenas-multiples"] = (
            "game.loadScene y flujo de escenas",
            "Las escenas son datos del proyecto; loadScene usa rutas y reglas de este runtime."),
        ["proyecto-json-avanzado"] = (
            "Campos avanzados de proyecto.json",
            "Campos como semillas RNG o rutas son consumidos solo por este editor y runtime; otro fork podría cambiarlos."),
        ["tilemap-catalogo"] = (
            "Catálogo de tiles y world.setTile",
            "Los IDs de catálogo y capas son los del mapa FUEngine; no confundas con Tiled sin conversión."),
        ["conocimiento-worldapi"] = (
            "Tabla world y APIs de escena",
            "world.* en tu build es el proxy documentado; si un tutorial usa otra firma, no aplica."),
        ["hot-reload-scripts"] = (
            "Recarga de .lua en Play",
            "Hot reload depende del editor y rutas de proyecto; no es un «file watcher» genérico."),
        ["autoguardado-editor"] = (
            "Autosave y recuperación",
            "La política de autosave es del editor WPF; no sustituye guardar con Ctrl+S antes de cerrar."),
        ["triggers-mapa-vs-objeto"] = (
            "Triggers de zona vs colisión de objeto",
            "Las dos familias de triggers se serializan en sitios distintos; el runtime los evalúa según este documento."),
        ["exportacion-build"] = (
            "Carpeta publish y ejecutable",
            "La exportación empaqueta datos y runtime del motor; no es un «build» de consola cruda."),
        ["troubleshooting-comun"] = (
            "Problemas típicos en este proyecto",
            "Las causas listadas son las de FUEngine (JSON, rutas, scripts); no uses diagnósticos de otros motores."),
        ["etiquetas-tags"] = (
            "Tags y búsquedas por etiqueta",
            "world.findByTag* usa el sistema de tags del proyecto; convención de nombres en datos y Lua."),
        ["ui-canvas-runtime"] = (
            "UI canvas y ui.* en Lua",
            "El binding de UI es el runtime del motor; no es XAML de WPF en juego."),
        ["biblioteca-global-assets"] = (
            "Biblioteca global y rutas de assets",
            "La biblioteca global es una carpeta gestionada por el Hub; rutas relativas son al proyecto."),
        ["integridad-proyecto"] = (
            "Comprobaciones de referencias rotas",
            "El checker usa el grafo de este proyecto; arregla referencias en este editor."),
        ["atajos-teclado"] = (
            "Atajos del editor FUEngine",
            "Los atajos son los del WPF host; no confundas con Visual Studio global."),
        ["creative-suite"] = (
            "Herramientas Creative Suite",
            "Las herramientas integradas escriben en formatos del proyecto; no son plugins de terceros."),
        ["copiar-pegar-mapa"] = (
            "Portapapeles y mapa",
            "Copiar/pegar tiles usa el formato interno del editor de mapa."),
        ["componentproxy-invoke"] = (
            "invoke y componentes desde Lua",
            "Solo ciertos componentes exponen métodos; invoke no es reflexión universal."),
        ["game-api-rng"] = (
            "Semilla y RNG de gameplay",
            "game.setRandomSeed y el runtime usan el generador del motor; reproducibilidad depende del proyecto."),
        ["tipos-de-capa"] = (
            "Solid, Objects, foreground…",
            "LayerType y colisión son los de FUEngine.Core; no mezcles con capas de otro motor."),
        ["multiseleccion-objetos"] = (
            "Varios objetos seleccionados",
            "La multiselección en inspector tiene limitaciones documentadas; no es el mismo flujo que Unity."),
        ["version-motor-compatibilidad"] = (
            "Versión del motor y proyectos",
            "La versión en proyecto.json se usa para avisos de compatibilidad; no es semver de paquetes NuGet genéricos."),
        ["consola-log-niveles"] = (
            "Niveles de log y categorías",
            "EditorLog y la consola del editor son la fuente de verdad; categorías son las de este código."),
        ["propiedades-script-at-prop"] = (
            "@prop y variables de script",
            "Las propiedades se inyectan como globales en Lua según el inspector; no son campos C# en el runtime."),
        ["native-protagonist-camera"] = (
            "Cámara, protagonista y nativo",
            "Comportamiento nativo del motor para seguir al jugador; flags en proyecto."),
        ["streaming-cache-play"] = (
            "Streaming y caché en Play",
            "Evicción y RuntimeTouched son conceptos del motor; no son cachés de disco genéricos."),
        ["vulkan-ventana-juego"] = (
            "Ventana Vulkan y ciclo de juego",
            "La ventana de juego usa Silk.NET Vulkan + GLFW en este repo; no es una ventana WPF."),
        ["naudio-audio-proyecto"] = (
            "NAudio en editor y manifiesto",
            "audio.json y el motor de audio del editor son NAudio; el juego exportado sigue el mismo manifiesto."),
        ["avalonedit-scripts-ide"] = (
            "AvalonEdit y Lua.xshd",
            "El resaltado usa el .xshd embebido; no es el mismo que VS Code con extensiones."),
        ["plugins-y-extensiones"] = (
            "Extensión del editor",
            "El modelo de plugins es el documentado; no hay Asset Store."),
        ["jerarquia-gameobject-lua"] = (
            "GameObject, transform y Lua",
            "Los proxies en Lua reflejan componentes del Core; nombres y límites son de este motor."),
        ["bootstrap-script"] = (
            "Script de arranque global",
            "El bootstrap del proyecto es una ruta en configuración; se ejecuta en el contexto documentado."),
        ["tiledata-collision-flags"] = (
            "TileData y flags de colisión",
            "Los flags de colisión son los del TileMap FUEngine; raycast de mapa los respeta."),
        ["deshacer-rehacer-editor"] = (
            "Undo/redo del editor",
            "El historial es el del editor WPF; no incluye todo el estado del disco externo."),
        ["pantalla-inicio-hub"] = (
            "Hub, proyectos recientes y Spotlight",
            "El Hub es la ventana de inicio de este ejecutable; Spotlight y documentación integrada viven aquí."),
        ["asistente-nuevo-proyecto-jerarquia"] = (
            "Asistente de proyecto y carpetas",
            "El asistente crea la estructura esperada por FUEngine; no la de otros motores."),
        ["idioma-tema-editor"] = (
            "Preferencias de idioma y tema",
            "Las preferencias se guardan en datos de usuario del editor; no en el proyecto."),
        ["medidores-gameplay-flags"] = (
            "Flags de gameplay y medidores",
            "Campos como medidores en proyecto son consumidos por el runtime según este manual."),
        ["lua-completion-catalogo"] = (
            "Catálogo de completions Lua",
            "LuaEditorCompletionCatalog define lo que sugiere Ctrl+Espacio; no es un LSP arbitrario."),
        ["particulas-render-estado"] = (
            "Partículas: datos vs render",
            "ParticleEmitter y el visor en editor pueden diferir; lo que cuenta en Play es el JSON."),
        ["fisica-raycast-dos-mundos"] = (
            "world.raycast vs physics.raycast",
            "Las dos APIs usan geometrías distintas documentadas; elige según colisión de mapa vs colliders."),
        ["limites-consejos"] = (
            "Límites conocidos del motor",
            "Los límites listados son de esta implementación; no son «bugs» de Lua en general."),
        ["discord-rich-presence"] = (
            "RPC de Discord y estado",
            "Rich Presence es opcional y usa la app FUEngine; fallos de red no deben tumbar el editor."),
        ["lua-patrones-snippets"] = (
            "Patrones Lua útiles en FUEngine",
            "Los patrones combinan APIs concretas (world, self, time); no son snippets de Love2D o Unity."),
        ["configuracion-motor-editor"] = (
            "Preferencias globales del motor",
            "Los ajustes globales viven fuera del proyecto; rutas por defecto y opciones de editor."),
        ["scripts-conectar-motor"] = (
            "Registrar scripts y asignar a objetos",
            "scripts.json + inspector es el flujo obligatorio; no hay «arrastrar .lua» a cualquier carpeta sin registro."),
        ["objeto-script-inspector-gameplay"] = (
            "Collider, partículas y @prop en instancia",
            "Los datos de instancia en objetos.json son los que Play lee; Lua no muta todos los componentes por igual."),
    };
}
