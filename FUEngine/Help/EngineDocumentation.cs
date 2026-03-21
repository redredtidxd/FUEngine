namespace FUEngine.Help;

/// <summary>Contenido de ayuda in-app: qué es cada parte del motor, para qué sirve y por qué existe.</summary>
public static class EngineDocumentation
{
    public const string QuickStartTopicId = "quick-start";

    /// <summary>Tema inicial al elegir «Documentación completa»: flujo de juego (no repetir el mismo apartado que la guía rápida).</summary>
    public const string FullManualStartTopicId = "crear-juego";

    public static IReadOnlyList<DocumentationTopic> Topics { get; } = BuildTopics();

    private static IReadOnlyList<DocumentationTopic> BuildTopics()
    {
        return new List<DocumentationTopic>
    {
        new(
            id: QuickStartTopicId,
            title: "Inicio rápido",
            paraQue: "Tener un camino mínimo desde cero hasta ver algo en Play.",
            porQueImporta: "Sin este orden es fácil perderse entre archivos JSON, pestañas y el mapa infinito.",
            paragraphs: new[]
            {
                "Abre o crea un proyecto desde la pantalla de inicio. Cada proyecto es una carpeta con configuración y escenas.",
                "Abre una escena (Archivo → Abrir escena o pestañas Scene). La escena enlaza mapa, objetos, triggers, scripts y UI.",
                "En la pestaña Mapa pinta tiles (capa activa en la barra lateral), coloca objetos con la herramienta Colocar o desde la jerarquía.",
                "Guarda con Ctrl+S o Guardar todo. Prueba el juego con Proyecto → Iniciar juego o el panel de Play / pestaña Juego.",
                "Los scripts Lua viven en archivos .lua registrados en el proyecto; se asignan a objetos en el Inspector (lista Scripts)."
            },
            bullets: new[]
            {
                "Explorador de archivos = disco del proyecto. Jerarquía del mapa = nodos lógicos de la escena (capas, objetos, UI).",
                "Si algo no se ve: revisa capa visible, herramienta activa y pestaña correcta (Mapa vs Scripts vs Juego).",
                "Con el proyecto abierto: menú Ayuda (documentación rápida / completa) o menú Proyecto → mismas guías sin salir del flujo de trabajo."
            }),

        new(
            id: "crear-juego",
            title: "Cómo funciona el motor y cómo hacer un juego",
            paraQue: "Tener una mentalidad clara: qué partes del FUEngine son el «nivel» y qué partes son la «lógica» del juego.",
            porQueImporta: "Un juego aquí es datos (mapa, objetos, audio, UI) + scripts Lua en Play; el editor solo prepara esos datos.",
            paragraphs: new[]
            {
                "FUEngine es un editor 2D + un runtime: en el editor dibujas y colocas; en Play el runtime ejecuta Lua, física, render Vulkan y audio según el proyecto.",
                "Flujo típico de trabajo: (1) Crear o abrir proyecto. (2) Definir escenas: cada escena tiene mapa, objetos, triggers, referencias a scripts y UI. (3) Pintar el nivel en capas (suelo, paredes/sólido, decoración). (4) Colocar objetos (jugador, enemigos, props) y asignar sprites/colisiones en el Inspector. (5) Escribir o enlazar scripts .lua registrados en el proyecto; asignarlos a objetos o triggers. (6) Probar con Proyecto → Iniciar juego o la pestaña Juego. (7) Ajustar audio (manifiesto), UI (canvas) y animaciones. (8) Exportar build cuando quieras distribuir.",
                "Los scripts leen entrada (input), mueven el mundo (world.setPosition, self.x/y), consultan colisión (world.raycast / physics), disparan audio (audio.play) y cambian escenas (game.loadScene). El mapa puede cambiar en runtime con world.setTile en capas de catálogo.",
                "No necesitas programar todo desde cero: usa seeds (prefabs) para reutilizar enemigos/objetos; usa triggers para zonas de evento sin código de distancia manual.",
                "Guarda a menudo (Ctrl+S / Guardar todo). La escena principal y las rutas de archivos se configuran en el proyecto; si Play no ves cambios, revisa que guardaste y que el script está en el registro de scripts del proyecto."
            },
            bullets: new[]
            {
                "Menú Ayuda → Documentación rápida: primeros pasos. Documentación completa: índice de todos los temas (empieza en este capítulo).",
                "Menú Proyecto → entradas de guía: mismo contenido, pensado para cuando ya tienes un proyecto abierto."
            }),

        new(
            id: "arquitectura",
            title: "Arquitectura del motor (ensamblados)",
            paraQue: "Entender qué código hace qué cuando algo falla o quieres ampliar el motor.",
            porQueImporta: "FUEngine separa datos, editor, ventana de juego y gráficos para poder iterar sin mezclar todo.",
            paragraphs: new[]
            {
                "FUEngine.Core: entidades de dominio (mapa por chunks y capas, GameObject, componentes, triggers, UI runtime, física de escena, etc.).",
                "FUEngine.Editor: serialización JSON (mapas, objetos, proyecto, scripts, audio, biblioteca), DTOs y guardado coherente con el Core.",
                "FUEngine (WPF): ventanas, paneles, herramientas de mapa, undo/redo, Play embebido, exportación.",
                "FUEngine.Runtime: bucle de juego, Lua (NLua), cámara, APIs inyectadas en scripts (world, input, time, …), generación procedural opcional.",
                "FUEngine.Graphics.Vulkan: dispositivo Vulkan (Silk.NET + GLFW) para la ventana de ejecución del juego."
            },
            bullets: new[]
            {
                "El editor dibuja el mapa con WPF; el juego en ejecución usa Vulkan salvo modos especiales.",
                "Los scripts solo ven lo que el Runtime expone: no asumas APIs de Unity; usa las tablas documentadas abajo."
            }),

        new(
            id: "layout-editor",
            title: "Layout del editor (paneles)",
            paraQue: "Saber dónde mirar: mapa, capas, inspector, consola y juego embebido.",
            porQueImporta: "El flujo de trabajo es mapa + selección + inspector; sin eso no editas propiedades finas.",
            paragraphs: new[]
            {
                "Centro: lienzo del mapa (pestaña Mapa) o contenido de otras pestañas (Scripts, Tiles, Juego, etc.).",
                "Izquierda: Explorador de proyecto (árbol de carpetas) y Jerarquía del mapa (estructura de la escena: Map, capas, objetos, triggers, UI).",
                "Derecha superior: lista de capas del mapa y visibilidad. Derecha inferior: Inspector según lo seleccionado.",
                "Ver → puedes mostrar u ocultar Jerarquía, Inspector, Consola y pestaña Juego (Play embebido).",
                "Barra de estado: coordenadas, chunk, herramienta activa; atajo de teclado recordatorio."
            }),

        new(
            id: "pestañas",
            title: "Pestañas del editor (+ botón +)",
            paraQue: "Abrir herramientas sin cerrar el mapa: scripts, biblioteca de tiles, animaciones, audio, debug, Creative Suite.",
            porQueImporta: "Cada pestaña concentra un flujo (editar Lua, pintar atlas, escuchar audio) sin mezclar UI.",
            paragraphs: new[]
            {
                "Mapa: edición del tilemap y objetos sobre el canvas. Consola: salida de logs del editor.",
                "Scripts: edición de scripts del proyecto. Explorador: vista enfocada del árbol de proyecto (según implementación actual).",
                "Tiles / Animaciones / Seeds: trabajo con catálogo, animaciones y prefabs (seeds comparten UI con objetos en parte del flujo).",
                "Juego: vista Play embebida (cuando está disponible). Debug: herramientas de depuración. Audio: manifiesto y escucha.",
                "Creative Suite: TileCreator, TileEditor, PaintCreator, PaintEditor, CollisionsEditor, ScriptableTile — flujos de arte y colisiones sobre assets."
            },
            bullets: new[]
            {
                "El botón «+» en las pestañas añade pestañas según contexto (no todas las combinaciones existen para todos los modos).",
                "El estado de pestañas abiertas puede persistir en el proyecto (layout del editor)."
            }),

        new(
            id: "menus-principales",
            title: "Menús principales (Archivo, Editar, Proyecto, …)",
            paraQue: "Operaciones globales: guardar, deshacer, exportar, configuración y simulación.",
            porQueImporta: "Muchas acciones no tienen botón dedicado y solo están en menú.",
            paragraphs: new[]
            {
                "Archivo: nuevo/abrir proyecto, crear/abrir/duplicar/eliminar/importar escenas, guardar mapa y guardar todo, exportar parcial, salir.",
                "Editar: deshacer/rehacer, copiar/pegar/duplicar zonas de tiles, snapshots del mapa, borrar selección.",
                "Proyecto: configuración del proyecto, propiedades del mapa, tamaño de tiles, integridad, limpieza de huérfanos, snapshots de proyecto, simulación, export build, iniciar/detener/pausar juego.",
                "Assets: biblioteca global para copiar recursos al proyecto.",
                "Ver: visibilidad de paneles y submenú de escenas. Herramientas: mismo conjunto que la barra de herramientas del mapa.",
                "Ayuda: documentación rápida/completa, atajos, acerca de. Configuración: preferencias del motor."
            }),

        new(
            id: "mapa-herramientas",
            title: "Mapa, capas y herramientas de dibujo",
            paraQue: "Construir el nivel: pintar, seleccionar, medir y editar a nivel píxel.",
            porQueImporta: "Las capas separan fondo, colisión, objetos y primer plano; la herramienta equivocada edita la capa equivocada.",
            paragraphs: new[]
            {
                "Tipos de capa (Core): Background (sin colisión por ocupación de celda), Solid (cualquier tile en la celda = muro), Objects, Foreground (orden visual / encima del jugador según flags).",
                "Cada capa tiene GUID propio en el JSON para que los chunks sigan a la capa aunque reordenes.",
                "Tiles: modo clásico (tipo + imagen) o catálogo (ID en atlas vía tileset JSON). En Lua: world.getTile / world.setTile usan nombre de capa e ID de catálogo.",
                "Zoom: rueda del ratón. Pan: clic central arrastrando. WASD desplazan la vista en el editor.",
                "Brocha: tamaño y rotación desde el botón Transform en la barra. Relleno: herramienta Relleno; Shift+clic según flujo de cubo.",
                "Selección rectangular de tiles: rotación 90°/180°, volteos, rellenar, copiar/pegar/duplicar (también desde menú Editar).",
                "Bucket fill tiene límite de celdas (protección anti-congelado) en el servicio de pintura."
            },
            bullets: new[]
            {
                "Herramientas: Pincel, Rectángulo, Línea, Relleno, Goma, Cuentagotas, Stamp, Seleccionar, Colocar objeto, Zona, Medir, Pixel.",
                "Visual → área visible: marco con la resolución lógica de cámara. Ir a 0,0 centra el origen del mundo."
            }),

        new(
            id: "jerarquia-explorador",
            title: "Jerarquía del mapa y Explorador de proyecto",
            paraQue: "La jerarquía organiza la escena; el explorador refleja carpetas y archivos reales.",
            porQueImporta: "world.find y rutas Lua usan nombres/jerarquía; el explorador es la fuente de rutas de assets.",
            paragraphs: new[]
            {
                "Jerarquía: nodos Scene → Map (capas de tiles) y fuera del mapa Objects, grupos, triggers, UI (Canvas y elementos).",
                "Desde la jerarquía puedes crear capas, objetos, triggers, grupos de píxeles y UI; duplicar, renombrar, propiedades.",
                "Explorador: árbol de carpetas del proyecto. Clic derecho en carpeta: Nuevo (Tile Layer, Object Layer, Trigger, Objeto, TileSet JSON, Seed, Script JSON, .lua, Sonido importado o grabado, etc.).",
                "Sobre un archivo: Abrir, favoritos, fijar, duplicar, eliminar, renombrar, mostrar en carpeta, propiedades, copiar ruta.",
                "Imágenes: «Editar colisiones…» abre el editor de colisiones. .lua: «Abrir en Tile por script» si aplica."
            }),

        new(
            id: "menus-contextuales",
            title: "Menús contextuales (clic derecho)",
            paraQue: "Acciones rápidas sin buscar en la barra de menús.",
            porQueImporta: "Es la forma más rápida de crear nodos o duplicar entidades.",
            paragraphs: new[]
            {
                "Jerarquía — vacío o raíz: Crear → Tile Layer, Object Layer, Grupo de pixeles/tiles, Trigger Zone, Objeto, UI → Canvas.",
                "Jerarquía — capa de tiles: Duplicar, Eliminar, Renombrar, Propiedades (según tipo).",
                "Jerarquía — objeto: Nuevo objeto, Duplicar, Eliminar, Renombrar, Propiedades.",
                "Jerarquía — trigger: Nuevo trigger, Duplicar, Eliminar, Renombrar, Propiedades.",
                "Jerarquía — UI Canvas: Crear hijos Button/Text/Image/Panel; Abrir en tab UI. Elementos UI: crear hijos y Propiedades.",
                "Panel de capas (lista lateral): Renombrar, Mover arriba/abajo, Eliminar capa.",
                "Explorador: ver lista detallada arriba; incluye Nuevo en carpetas y acciones sobre archivos."
            }),

        new(
            id: "inspector",
            title: "Inspector (qué muestra según la selección)",
            paraQue: "Editar propiedades finas de lo que tienes seleccionado.",
            porQueImporta: "Un mismo panel cambia de contenido: objeto, trigger, capa, tile, animación, UI o vista resumen.",
            paragraphs: new[]
            {
                "Nada o contexto general: vista resumen del mapa (conteos, herramienta, capa) — Overview.",
                "Un objeto: ObjectInspector — Instance ID, Definition ID, nombre, posición X/Y, rotación (combo o grados), escala X/Y, orden de render, visible, tamaño, vista previa de sprite.",
                "Interacción: colisión, tipo de colisión, interactivo, destructible, tags separados por comas.",
                "Juego: «Habilitar dibujo en juego» (CanvasController.lua) para permitir pintar sobre el objeto en Play según límites de textura.",
                "Scripts: lista de scripts asignados, añadir/quitar, validación si falta entrada en scripts.json.",
                "Propiedades del script: variables globales detectadas en la raíz del .lua (heurística por líneas); botón añadir propiedad manual.",
                "Varios objetos seleccionados: MultiObjectInspector. Trigger: TriggerZoneInspector. Capa: LayerInspector. Tile bajo cursor: TileInspector. Animación: AnimationInspector. UI: UIElementInspector. Archivo en explorador: panel rápido de propiedades de asset."
            }),

        new(
            id: "objetos-componentes",
            title: "Objetos, componentes y GameObject",
            paraQue: "Representar entidades con sprite, colisión, scripts y luces.",
            porQueImporta: "El modelo es orientado a objetos con componentes (no ECS): encaja con el Inspector y con Lua.",
            paragraphs: new[]
            {
                "GameObject tiene Transform, nombre, tags, hijos/padre, orden de render y componentes (Sprite, Collider, Script, Light, …).",
                "Colocar objeto en el mapa crea una instancia con posición; puedes convertir a seed desde acciones del inspector.",
                "Los componentes se gestionan en datos del proyecto; en Lua, self:getComponent(\"Nombre\") devuelve un proxy con invoke().",
                "addComponent desde Lua está reservado/factory en el host — no asumas que todo tipo se puede añadir en runtime sin soporte."
            }),

        new(
            id: "triggers",
            title: "Triggers (zonas)",
            paraQue: "Rectángulos en el mapa que ejecutan scripts al entrar, salir o cada frame.",
            porQueImporta: "Evitas lógica de proximidad manual para portales, diálogos o checkpoints.",
            paragraphs: new[]
            {
                "Cada zona tiene tamaño en celdas, capa asociada, y scripts por evento (enter/exit/tick) referenciados por ID del registro de scripts.",
                "Se guardan en el JSON de triggers del proyecto; la jerarquía lista las zonas para seleccionarlas y abrir el inspector dedicado."
            }),

        new(
            id: "seeds",
            title: "Seeds (prefabs)",
            paraQue: "Reutilizar definiciones de objetos e instanciarlas por nombre desde Lua o el editor.",
            porQueImporta: "Separar «molde» de «instancia en escena» acelera iteración y mantiene coherencia.",
            paragraphs: new[]
            {
                "Un seed es un archivo/asset de definición; world.instantiate(\"nombre\", x, y) y self:instantiate(...) crean instancias en runtime.",
                "Variantes: el último parámetro opcional de instantiate selecciona variantes de seed con convención de nombres (prefab_variant).",
                "El proyecto de ejemplo puede incluir seeds demo enlazados a scripts de prueba."
            }),

        new(
            id: "scripting-lua",
            title: "Scripting Lua — referencia completa de APIs",
            paraQue: "Programar comportamiento en Play: entrada, movimiento, audio, UI y consultas.",
            porQueImporta: "Todo lo que el juego hace en runtime pasa por estas tablas inyectadas en el entorno NLua.",
            paragraphs: new[]
            {
                "Convención: scripts pueden definir onStart() y onUpdate(dt). El motor crea self como proxy del objeto dueño del ScriptComponent.",
                "Constantes: Key.* (W, A, S, D, Space, …) y Mouse.* (Left=0, Right=1) para pasar a input.isKeyDown / isMouseDown.",
                "self (SelfProxy): id, name, tag, tags[], hasTag, x, y, rotation, scale, visible, active, renderOrder; move, rotate, destroy; find, findInHierarchy, getParent, getChildren, setParent; getComponent, removeComponent; setSpriteTexture, addSpriteFrame, clearSpriteFrames, spriteFrame, setSpriteAnimationFps, setSpriteSortOffset, setSpriteDisplaySize; playAnimation / stopAnimation (enlace futuro); instantiate(prefab, x, y, rot?, variant?).",
                "world: findObject / getObjectByName, findByTag / getObjectByTag / getObjects / getAllObjects, findByPath, findNearestByTag, spawn/instantiate, destroy, setPosition, getPlayer, getTile(x,y,layerName), setTile(x,y,layerName,catalogId), raycast(origen, dir, maxDist, ignore?), raycastTiles, raycastCombined.",
                "input: isKeyDown, isKeyPressed, isMouseDown, mouseX, mouseY.",
                "time: delta, time, seconds, frame, scale.",
                "audio: play(id), play(id, volume), playMusic(id), playMusic(id, loop), playSfx, stopMusic(fade), setVolume(bus, 0..1), stop, stopAll, setMasterVolume.",
                "physics (Play suele usar PlayScenePhysicsApi): raycast(x1,y1,x2,y2), overlapCircle(cx, cy, radius) → lista de proxies.",
                "ui: show/hide/setFocus(canvasId), pushState/popState, get(canvasId, elementId), bind(canvasId, elementId, eventName, callback) — eventos click, hover, pressed, released; solo el canvas con foco recibe input.",
                "game: loadScene(name), quit(), setRandomSeed, randomInt, randomDouble.",
                "Debug (si está inyectado): drawLine, drawCircle — colores RGBA 0–255; útil para depurar en la vista de juego.",
                "ComponentProxy: typeName, invoke(method, ...), invokeWithResult(...). Para ScriptComponent llama métodos expuestos en la instancia Lua."
            },
            bullets: new[]
            {
                "world.raycast alinea con colliders sólidos del host; physics.raycast es el segmento contra colliders de la escena física (API distinta).",
                "world.raycastCombined elige el impacto más cercano entre tiles y objetos.",
                "getPlayer devuelve el primer objeto llamado «Player» (comparación case-insensitive)."
            }),

        new(
            id: "play-runtime",
            title: "Play, runtime y ventana de juego",
            paraQue: "Probar la escena con Vulkan, Lua y audio sin salir del flujo del editor.",
            porQueImporta: "El runtime no es el mismo que el canvas WPF: diferencias de rendimiento y de pipeline.",
            paragraphs: new[]
            {
                "Proyecto → Iniciar juego (escena actual o principal) arranca el bucle de Runtime con la escena cargada.",
                "La pestaña Juego puede mostrar el play embebido; Debug.draw* superpone primitivas en unidades de mundo del transform.",
                "Pausa / detener desde barra o menú cuando esté habilitado."
            }),

        new(
            id: "fisica",
            title: "Física y consultas",
            paraQue: "Evitar traslapes, detectar suelo o disparos.",
            porQueImporta: "Hay dos mundos: colisión de tiles + AABB/triggers en el paso de Play y consultas explícitas en APIs.",
            paragraphs: new[]
            {
                "En Play, el motor resuelve física de escena (tiles + cuerpos) y triggers.",
                "world.* raycast usa la implementación del host alineada con colliders sólidos; physics.* usa la API de colliders de escena.",
                "overlapCircle devuelve proxies en la zona (incluye triggers según implementación)."
            }),

        new(
            id: "chunks-streaming",
            title: "Chunks, streaming y área visible",
            paraQue: "Trabajar mapas grandes sin cargar todo en memoria a la vez.",
            porQueImporta: "Los chunks acotan I/O y actualización; el streaming descarga lejos del jugador.",
            paragraphs: new[]
            {
                "Tamaño de chunk y radio de carga se configuran en el proyecto (8–64 típico).",
                "Opciones: descargar chunks lejanos, guardar por chunk, dormir entidades lejanas, streaming de contenido.",
                "Mostrar límites de chunk dibuja la cuadrícula de depuración en el editor.",
                "setTile/getTile en runtime marcan chunks para no perder estado al evictar vacíos."
            }),

        new(
            id: "iluminacion-audio-ui",
            title: "Iluminación, audio y UI runtime",
            paraQue: "Ambientar el nivel, sonido por manifiesto y menús con canvas declarativo.",
            porQueImporta: "Luces afectan tinte en editor y pipeline básico en Vulkan; audio y UI se enlazan desde datos JSON + Lua.",
            paragraphs: new[]
            {
                "Luces: LightComponent y LightingManager; en editor el visor puede muestrear brillo/tinte en tiles y sprites.",
                "audio.json: IDs usados por audio.play* en Lua; el editor puede usar NAudio para preescucha según configuración.",
                "UI: canvas serializado; ui.* en Lua controla visibilidad, foco y eventos sobre elementos."
            }),

        new(
            id: "animaciones-export",
            title: "Animaciones y exportación (build)",
            paraQue: "Definir clips y distribuir el juego fuera del editor.",
            porQueImporta: "Animaciones viven en datos dedicados; el build empaqueta ejecutable + Data.",
            paragraphs: new[]
            {
                "Pestaña Animaciones y AnimationInspector editan datos de animación del proyecto.",
                "Proyecto → Exportar build del juego genera ejecutable y carpeta de datos para distribución (según ProjectBuildService / export helpers)."
            }),

        new(
            id: "archivos-json",
            title: "Archivos JSON típicos del proyecto",
            paraQue: "Saber qué tocar o qué romper al copiar carpetas.",
            porQueImporta: "El editor persiste casi todo en JSON con convenciones camelCase compartidas.",
            paragraphs: new[]
            {
                "Mapa: capas + chunks con tiles (referencia a LayerId por GUID).",
                "Objetos: capa de instancias. Triggers: zonas. Scripts: registro con rutas. Seeds: definiciones.",
                "UI: canvas. Audio: manifiesto. Proyecto: configuración global (resolución, FPS, chunk, rutas principales).",
                "Carga parcial: muchos loaders devuelven vacío si falta el archivo; el proyecto principal suele exigir archivo."
            }),

        new(
            id: "limites-consejos",
            title: "Límites, undo y buenas prácticas",
            paraQue: "Evitar sorpresas: rellenos enormes, historial corto o parsers heurísticos.",
            porQueImporta: "El editor prioriza seguridad y tiempo de respuesta sobre operaciones destructivas.",
            paragraphs: new[]
            {
                "Historial de deshacer limitado (orden de decenas de pasos) — guarda a menudo.",
                "Parser de variables de script en el inspector es heurístico (no analiza Lua completo): variables globales al inicio sin local funcionan mejor.",
                "Biblioteca global y assets: mantén rutas relativas coherentes al mover carpetas.",
                "Si Play no refleja cambios: guarda escena, revisa script asignado y consola de errores Lua."
            })
        };
    }
}

public sealed class DocumentationTopic
{
    public DocumentationTopic(
        string id,
        string title,
        string? paraQue,
        string? porQueImporta,
        IReadOnlyList<string> paragraphs,
        IReadOnlyList<string>? bullets = null)
    {
        Id = id;
        Title = title;
        ParaQue = paraQue;
        PorQueImporta = porQueImporta;
        Paragraphs = paragraphs;
        Bullets = bullets;
    }

    public string Id { get; }
    public string Title { get; }
    public string? ParaQue { get; }
    public string? PorQueImporta { get; }
    public IReadOnlyList<string> Paragraphs { get; }
    public IReadOnlyList<string>? Bullets { get; }
}
