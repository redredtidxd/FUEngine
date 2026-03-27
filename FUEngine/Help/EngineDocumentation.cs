using System.Runtime.CompilerServices;

namespace FUEngine.Help;

/// <summary>Contenido de ayuda in-app: qué es cada parte del motor, para qué sirve y por qué existe.</summary>
public static partial class EngineDocumentation
{
    public const string QuickStartTopicId = "quick-start";

    /// <summary>Tema inicial al elegir «Documentación completa»: flujo de juego (no repetir el mismo apartado que la guía rápida).</summary>
    public const string FullManualStartTopicId = "crear-juego";

    /// <summary>Índice de la pestaña «Lua — sintaxis y librería» (palabras reservadas + guías usadas con el motor).</summary>
    public const string LuaReferenceIntroTopicId = "lua-reference-intro";

    public static bool IsLuaReferenceSidebarTopic(string? id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return id == LuaReferenceIntroTopicId
               || id.StartsWith("lua-kw-", StringComparison.Ordinal)
               || id.StartsWith("lua-guide-", StringComparison.Ordinal);
    }

    /// <summary>
    /// Inicialización diferida: el primer acceso ocurre después de todos los inicializadores de campos estáticos
    /// del tipo (p. ej. <c>ManualPresentation</c> en el otro partial), evitando NRE por orden de partials/CLR.
    /// </summary>
    private static IReadOnlyList<DocumentationTopic>? _topics;

    public static IReadOnlyList<DocumentationTopic> Topics =>
        LazyInitializer.EnsureInitialized(ref _topics, BuildTopics);

    private static IReadOnlyList<DocumentationTopic> BuildTopics()
    {
        var topics = new List<DocumentationTopic>
    {
        new(
            id: QuickStartTopicId,
            title: "Inicio rápido",
            paraQue: "Tener un camino mínimo desde cero hasta ver algo en Play.",
            porQueImporta: "Sin este orden es fácil perderse entre archivos JSON, pestañas y el lienzo del mapa.",
            paragraphs: new[]
            {
                "Abre o crea un proyecto desde la pantalla de inicio (Hub). Cada proyecto es una carpeta con configuración y escenas; el Hub también ofrece biblioteca global, Lua global, estado del motor y FUEngine Spotlight (Ctrl+P), un buscador integrado en la misma ventana para proyectos y la guía AI-ONBOARDING. Con Discord en ejecución, el estado del motor puede mostrarse en tu perfil (Rich Presence): pestañas del Hub, del editor, ayuda integrada, Spotlight y ventanas modales (configuración, exportación, etc.) actualizan el texto. El cliente envía un botón «Ver en GitHub» al repositorio público del motor (https://github.com/redredtidxd/FUEngine); suele verse al abrir el perfil completo o en la vista de actividad. Si no aparece, revisa en el Developer Portal de Discord (aplicación FUEngine) la configuración de Rich Presence y dominios o reglas de URLs para botones, y la consola del editor (categoría Discord) por errores del RPC. Si Discord no está abierto o falla la conexión, también puede haber avisos ahí.",
                "Abre una escena (Mapa → Abrir escena o pestañas Scene). La escena enlaza mapa, objetos, triggers, scripts y UI.",
                "Al abrir el editor, la primera pestaña central es Mapa (edición del tilemap); la pestaña Juego muestra Play embebido. Pinta tiles con la capa activa en el panel de capas a la derecha y coloca objetos con la herramienta Colocar o desde la jerarquía.",
                "Guarda con Ctrl+S o Guardar todo. Prueba el juego con Proyecto → Iniciar juego o el panel de Play / pestaña Juego.",
                "Los scripts Lua viven en archivos .lua registrados en el proyecto; se asignan a objetos en el Inspector (lista Scripts). En la pestaña Scripts el editor sugiere APIs mientras escribes (Ctrl+Espacio para forzar sugerencias)."
            },
            bullets: new[]
            {
                "Explorador de archivos = disco del proyecto. Jerarquía de la escena = nodos lógicos (Scene, Map/Layers, objetos, triggers, UI).",
                "Si algo no se ve: revisa capa visible, herramienta activa y pestaña correcta (Mapa vs Scripts vs Juego).",
                "Con el proyecto abierto: menú Ayuda (manual rápido / manual completo / API Lua).",
                "Más temas en el manual: Depuración, Hot reload, Escenas múltiples, Triggers mapa vs objeto, Exportación build, FAQ troubleshooting.",
                "Si clonaste el repositorio: ver el tema «Compilar desde el repositorio» y README.md en la raíz (tools\\publicar.bat abre la carpeta con FUEngine.exe)."
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
                "Inspector de objeto: además de scripts y colisión básica, el expander «Sprite, física y gameplay» y «Añadir componente…» permiten tinte/flip, clip de animación por defecto, collider Box/Circle, rigidbody, cámara que sigue al objeto, sensor por distancia, vida, audio y datos de partículas (todo en objetos.json).",
                "Guarda a menudo (Ctrl+S / Guardar todo). La escena principal y las rutas de archivos se configuran en el proyecto; si Play no ves cambios, revisa que guardaste y que el script está en el registro de scripts del proyecto."
            },
            bullets: new[]
            {
                "Menú Ayuda → Documentación rápida: primeros pasos. Documentación completa: índice de todos los temas (empieza en este capítulo).",
                "Menú Proyecto → entradas de guía: mismo contenido, pensado para cuando ya tienes un proyecto abierto.",
                "Temas relacionados: Eventos y hooks Lua, Depuración y consola, Seeds, Scripts de capa, Problemas frecuentes.",
                "También: Vulkan, NAudio, AvalonEdit, Plugins, Bootstrap, TileData, Hub de inicio, autocompletado Lua, partículas (datos vs render), raycast mapa vs physics."
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
                "FUEngine.Runtime: bucle de juego, Lua (NLua), cámara, APIs inyectadas en scripts (world, input, time, ads, …), generación procedural opcional.",
                "FUEngine.Graphics.Vulkan: dispositivo Vulkan (Silk.NET + GLFW) para la ventana de ejecución del juego."
            },
            bullets: new[]
            {
                "El editor dibuja el mapa con WPF; el juego en ejecución usa Vulkan salvo modos especiales.",
                "Los scripts solo ven lo que el Runtime expone: no asumas APIs de Unity; usa las tablas documentadas abajo."
            }),

        new(
            id: "compilar-desde-fuente",
            title: "Compilar desde el repositorio",
            paraQue: "Obtener FUEngine.exe si tienes el código fuente y no confundir con solo ejecutar el programa.",
            porQueImporta: "Abrir el ejecutable no compila; publicar.bat genera el build en una carpeta nueva y abre el Explorador.",
            paragraphs: new[]
            {
                "Instrucciones completas y tabla «ejecutar vs compilar» están en README.md en la raíz del repositorio (único README de entrada).",
                "Para generar el editor: entra en la carpeta tools del repo y ejecuta publicar.bat. Publica en Release (win-x64, autocontenido) y abre la carpeta de salida publish\\Release_fecha_hora con FUEngine.exe dentro.",
                "Ejecuta FUEngine.exe desde esa carpeta. Cada publicación crea una subcarpeta nueva para no bloquear si el editor sigue abierto.",
                "Desarrollo diario del motor: Visual Studio o Rider con FUEngine.sln, o dotnet run --project FUEngine\\FUEngine.csproj; es distinto del flujo publicar.bat (sin ejecutable autocontenido en publish\\)."
            },
            bullets: new[]
            {
                "Requisitos: Windows y .NET SDK 8 (detalle en README.md).",
                "limpiar.bat y release_publish.bat: ver README.md, sección scripts en tools."
            }),

        new(
            id: "layout-editor",
            title: "Layout del editor (paneles)",
            paraQue: "Saber dónde mirar: mapa, capas, inspector, consola y juego embebido.",
            porQueImporta: "El flujo de trabajo es mapa + selección + inspector; sin eso no editas propiedades finas.",
            paragraphs: new[]
            {
                "Centro: pestañas Mapa, Juego y Consola (por defecto se abre Mapa; Juego = Play embebido) y el botón «+» para Scripts, Tiles, Creative Suite, etc.",
                "Izquierda: Explorador de proyecto (árbol de carpetas) y Jerarquía de la escena (Scene, Map/Layers, objetos, triggers, UI).",
                "Derecha superior: panel de capas (lista con visibilidad, bloqueo, renombrar inline y botones de pie para añadir/quitar/reordenar). Derecha inferior: Inspector según lo seleccionado.",
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
                "Mapa: edición del tilemap y objetos sobre el canvas (scroll con rueda o pan). Juego: vista Play embebida. Consola: salida de logs del editor.",
                "Scripts: mini-IDE con lista de scripts registrados (scripts.json + .lua en Scripts/), resaltado Lua/JSON y autocompletado de APIs (ver tema «Mini-IDE de scripts»). Explorador: vista enfocada del árbol de proyecto (según implementación actual).",
                "Tiles / Animaciones / Seeds: trabajo con catálogo, animaciones y prefabs (seeds comparten UI con objetos en parte del flujo).",
                "Debug: herramientas de depuración. Audio: manifiesto y escucha.",
                "Creative Suite: TileCreator, TileEditor, PaintCreator, PaintEditor, CollisionsEditor, ScriptableTile — flujos de arte y colisiones sobre assets."
            },
            bullets: new[]
            {
                "El botón «+» en las pestañas añade pestañas según contexto (no todas las combinaciones existen para todos los modos).",
                "El estado de pestañas abiertas puede persistir en el proyecto (layout del editor)."
            }),

        new(
            id: "menus-principales",
            title: "Menús principales (Archivo, Editar, Proyecto, Mapa, …)",
            paraQue: "Operaciones globales: guardar, deshacer, exportar, configuración y simulación.",
            porQueImporta: "Muchas acciones no tienen botón dedicado y solo están en menú.",
            paragraphs: new[]
            {
                "Archivo: nuevo/abrir proyecto, guardar escena (mapa+objetos+UI), guardar escena como copia, guardar todo, importar assets a Assets/Sprites y Assets/Audio, exportar parcial, salir al Hub o salir del motor.",
                "Editar: deshacer/rehacer, copiar/pegar/duplicar zonas de tiles, snapshots del mapa, borrar selección, preferencias del editor (tema, idioma, AppData), atajos de teclado.",
                "Proyecto: ajustes del manifiesto .FUE (Inspector), editor avanzado (chunks, rutas), scripts, biblioteca global, integridad, huérfanos, limpiar caché (Vulkan/Cache en AppData), snapshots, simulación, export build, iniciar/detener/pausar juego.",
                "Mapa: crear/abrir/duplicar/eliminar/importar escenas, fondo del visor y propiedades del mapa, regenerar colisiones (flujo del editor de colisiones).",
                "Semillas: crear .seed en Seeds/ y abrir carpeta GlobalTemplates (AppData).",
                "Ventana: paneles (jerarquía, inspector), pestañas (Mapa, Explorador, Scripts, Consola, Juego), restablecer disposición de pestañas y paneles.",
                "Herramientas: mismo conjunto que la barra de herramientas del mapa.",
                "Ayuda: manual rápido/completo, API Lua, abrir carpeta de logs, reportar bug en GitHub, acerca de (versión y licencia). Preferencias del motor: Editar → Preferencias del editor."
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
                "Zoom del lienzo: Ctrl+rueda del ratón o botones +/−. Rueda sin Ctrl: desplaza el mapa (scroll del área). Pan: clic central arrastrando. WASD desplazan la vista (paso mayor y proporcional al zoom del lienzo).",
                "Mapa finito (Infinite desactivado): margen de un chunk alrededor del rectángulo de juego; borde azul del área jugable y botones «+ chunk» solo en la frontera (un botón por celda de chunk de tamaño ChunkSize×ChunkSize casillas). Cada clic crea un chunk vacío en esa celda y el rectángulo de juego (origen + MapWidth/Height en proyecto.json) se recalcula como la unión de todos los chunks, permitiendo formas no rectangulares. Si el marco azul estaba en el centro geométrico del rectángulo, tras expandir se recalcula ese centro.",
                "Brocha: tamaño y rotación desde el botón Transform en la barra. Relleno: herramienta Relleno; Shift+clic según flujo de cubo.",
                "Selección rectangular de tiles: rotación 90°/180°, volteos, rellenar, copiar/pegar/duplicar (también desde menú Editar).",
                "Bucket fill tiene límite de celdas (protección anti-congelado) en el servicio de pintura."
            },
            bullets: new[]
            {
                "Herramientas: Pincel, Rectángulo, Línea, Relleno, Goma, Cuentagotas, Stamp, Seleccionar, Colocar objeto, Zona, Medir, Pixel.",
                "Visual → área visible: marco azul = rectángulo de la cámara/render (px y casillas mostrados en el propio marco). Alt+arrastrar mueve la cámara. Botones Centro mapa / Mundo 0,0 en la barra; scripts en Play usan el mismo rectángulo de vista que el visor (tamaño del canvas del tab Juego). Fuera del área jugable: celdas «+ chunk» en la frontera del conjunto de chunks. Si el visor estaba en el centro del mapa y expandes, el centro se actualiza solo. Proyecto → Avanzado: color del lienzo y fondo de escena.",
                "Visual → Play activo también en pestaña Mapa: el sandbox del tab Juego no se pausa al volver al mapa (edición y play a la vez).",
                "Inspector de capa (`LayerInspectorPanel`): al final del bloque de propiedades, «Añadir componente…» abre el catálogo (script Lua de capa y entradas reservadas)."
            }),

        new(
            id: "jerarquia-explorador",
            title: "Jerarquía de la escena y Explorador de proyecto",
            paraQue: "La jerarquía organiza la escena; el explorador refleja carpetas y archivos reales.",
            porQueImporta: "world.find y rutas Lua usan nombres/jerarquía; el explorador es la fuente de rutas de assets.",
            paragraphs: new[]
            {
                "Jerarquía: raíz «Scene: nombre»; «Map · archivo» con Layers. Instancias de objetos, triggers y Canvas UI cuelgan directamente de la escena (sin carpetas Objetos/UI). La carpeta Groups agrupa píxeles/tiles.",
                "Desde la jerarquía: capas desde Map/Layers o el panel de capas; objetos, triggers y UI como hijos de la escena; duplicar, renombrar, propiedades. Arrastrar un .seed al nodo escena, a la capa Objetos o al canvas del mapa instancia en esa posición. Arrastrar un objeto al explorador guarda un .seed.",
                "Clic derecho en la raíz escena o en el fondo vacío: crear objeto, trigger, grupo, Nuevo Mapa (archivo .map nuevo), UI (Canvas, Text, Button, Panel, TabControl). Las capas solo desde Map/Layers o el panel de capas.",
                "Explorador: árbol de carpetas del proyecto; vistas lista y cuadrícula con carpeta actual y migas. Clic en el fondo deselecciona. Clic derecho en carpeta: Nuevo (Tile Layer, Object Layer, Trigger, Objeto, TileSet JSON, Seed, Script JSON, .lua, Sonido importado o grabado, etc.).",
                "Sobre un archivo: Abrir, favoritos, fijar, duplicar, eliminar, renombrar, mostrar en carpeta, propiedades, copiar ruta.",
                "Clic en el manifiesto del proyecto (Project.FUE o proyecto.json canónico): Inspector con panel de control del proyecto (resolución, tile, autoría, cámara, colores, guardar, exportar, integridad, carpeta). El manifiesto se resalta en el explorador. Doble clic en el .FUE del manifiesto abre la configuración avanzada del proyecto. En proyectos nuevos, los JSON de índice (scripts, seeds, animaciones, audio) suelen estar en la carpeta Data; el explorador puede ocultar Data en preferencias del motor. Clic en un .seed: Inspector con resumen del prefab y «Abrir script asociado» si aplica. Doble clic en .lua y .json abre la pestaña Scripts; doble clic en .seed abre el .lua del seed si está registrado.",
                "Imágenes: «Editar colisiones…» abre el editor de colisiones. .lua: «Abrir en Tile por script» si aplica."
            }),

        new(
            id: "menus-contextuales",
            title: "Menús contextuales (clic derecho)",
            paraQue: "Acciones rápidas sin buscar en la barra de menús.",
            porQueImporta: "Es la forma más rápida de crear nodos o duplicar entidades.",
            paragraphs: new[]
            {
                "Jerarquía — vacío, fondo o raíz escena: Crear → Objeto, Trigger, Grupo de píxeles, Nuevo Mapa, UI (Canvas, Text, Button, Panel, TabControl); sin capas del mapa aquí (Map/Layers o panel de capas).",
                "Jerarquía — nodo Map o Layers: Tile Layer, Object Layer, grupo de píxeles/tiles.",
                "Jerarquía — capa de tiles: Duplicar, Eliminar, Renombrar, Propiedades (según tipo).",
                "Jerarquía — objeto: Nuevo objeto, Duplicar, Eliminar, Renombrar, Propiedades.",
                "Jerarquía — trigger: Nuevo trigger, Duplicar, Eliminar, Renombrar, Propiedades.",
                "Jerarquía — UI Canvas: Crear hijos Button/Text/Image/Panel; Abrir en tab UI. Elementos UI: crear hijos y Propiedades.",
                "Panel de capas (lista lateral): Renombrar inline en la fila, visibilidad/bloqueo, «Añadir capa» / Quitar; reordenar desde el menú contextual si aplica. «Añadir componente…» está en el inspector de la capa seleccionada, no en la lista.",
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
                "Un objeto: ObjectInspector — cabecera (nombre, ID abreviado, activo en Play), bloque Identidad (GUID solo lectura, regenerar, tipo, etiquetas, capa Z, visible), Transform, luz puntual opcional, expander «Sprite, física y gameplay (Play)»: tinte/flip/sort, clip de animación por defecto, forma de colisión Box/Circle y tamaños, rigidbody, CameraTarget, ProximitySensor, Health, AudioSource, ParticleEmitter (persistidos en objetos.json). Interacción y scripts.",
                "Scripts: lista y «Añadir componente…» (visibilidad, sprite avanzado, colisión, rigidbody, luz, animación, audio, proximidad, salud, cámara, partículas, foco en scripts). Arrastrar un .lua al panel añade el script si está en scripts.json. Arrastrar un objeto desde la jerarquía sobre un campo tipo referencia rellena el InstanceId.",
                "Propiedades del script: líneas -- @prop nombre: tipo = valor (prioridad) o variables globales en la raíz del .lua; tipos incl. object (InstanceId). Con Play activo, sincronización hot y edición al perder foco. El expander «Variables de script (@prop / globales)» en el inspector es donde el diseñador cambia esos valores por instancia (ver temas «Conectar scripts con el motor» y «Objeto + script + Inspector: hitbox, partículas y @prop»).",
                "Varios objetos seleccionados: MultiObjectInspector. Trigger: TriggerZoneInspector. Capa: LayerInspector (incluye script de capa Lua y entradas futuras de componentes). Tile bajo cursor: TileInspector. Animación: AnimationInspector. UI: UIElementInspector. Archivo en explorador: panel rápido de propiedades de asset."
            }),

        new(
            id: "objetos-componentes",
            title: "Objetos, componentes y GameObject",
            paraQue: "Representar entidades con sprite, colisión, scripts y luces.",
            porQueImporta: "El modelo es orientado a objetos con componentes (no ECS): encaja con el Inspector y con Lua.",
            paragraphs: new[]
            {
                "GameObject tiene Transform, nombre, tags, hijos/padre, orden de render y componentes (Sprite, Collider, Script, Light, Rigidbody, Health, ProximitySensor, CameraTarget, AudioSource, ParticleEmitter, …).",
                "Colocar objeto en el mapa crea una instancia con posición; puedes convertir a seed desde acciones del inspector y, si la instancia procede de un seed, aplicar cambios de vuelta al archivo .seed.",
                "Los componentes se gestionan en datos del proyecto; en Lua, self:getComponent(\"Nombre\") devuelve un proxy con invoke().",
                "addComponent desde Lua está reservado/factory en el host — no asumas que todo tipo se puede añadir en runtime sin soporte.",
                "Cada instancia de objeto guarda en objetos.json los campos del inspector avanzado (tinte, flip, animación por defecto, forma de collider, rigidbody, luz, cámara, proximidad, salud, audio, partículas). No es un array JSON de componentes arbitrarios: son propiedades tipadas de la instancia que el motor interpreta al crear GameObjects en Play.",
                "SpriteComponent: textura y tamaño vienen de la definición del tipo; la instancia puede añadir tinte (#RRGGBB), Flip X/Y, sort offset y velocidad de animación. El visor del editor aplica luz puntual como multiplicador RGB y luego el tinte del sprite.",
                "ColliderComponent: un solo collider por objeto; Box o Circle en datos, pero la resolución de física usa AABB (el círculo es un cuadrado con mitades iguales). overlapCircle en Lua comprueba el círculo de consulta contra esos AABB.",
                "RigidbodyComponent: velocidad integrada con la gravedad del proyecto antes del paso de física; el protagonista con control nativo WASD no usa este paso para no duplicar movimiento.",
                "ProximitySensor: no usa segundo collider; calcula distancia al primer objeto con la etiqueta indicada y dispara onTriggerEnter / onTriggerExit como los triggers por collider.",
                "CameraTarget: si algún objeto lo tiene activo, la cámara nativa sigue ese objeto en lugar del protagonista por defecto (primero encontrado en la escena).",
                "HealthComponent y AudioSourceComponent almacenan estado o metadatos; el sonido se reproduce con la tabla audio y el manifiesto audio.json.",
                "ParticleEmitterComponent guarda rutas y parámetros para futuro render de partículas en el visor."
            },
            bullets: new[]
            {
                "Nombre de tipo en getComponent: igual que la clase C# (p. ej. HealthComponent, ColliderComponent).",
                "Eventos: onTriggerEnter / onTriggerExit (triggers AABB y proximidad); onUpdate / onLateUpdate para lógica por frame.",
                "Capa Z: LayerOrder en la instancia se copia a GameObject.RenderOrder; SortOffset en SpriteComponent desempata dentro del mismo orden.",
                "Documentación técnica extendida: docs/AI-ONBOARDING.md sección 8.2 (tabla instancia → JSON → runtime)."
            }),

        new(
            id: "componentes-json-play",
            title: "Componentes: inspector, objetos.json y Play",
            paraQue: "Saber qué campos existen, dónde se guardan y qué hace el motor con ellos.",
            porQueImporta: "Evita confundir datos del editor con comportamiento en runtime y acelera depurar colisiones, animaciones y cámara.",
            paragraphs: new[]
            {
                "El expander «Sprite, física y gameplay (Play)» del ObjectInspector edita propiedades de ObjectInstance que se serializan en la capa de objetos (objetos.json). Incluye: tinte y flip del sprite, clip de animación por defecto (animaciones.json), forma y tamaño de collider, rigidbody, marca de CameraTarget, sensor de proximidad (rango + etiqueta objetivo), vida, audio (ID de clip) y emisor de partículas (textura y tasas).",
                "Añadir componente… abre un catálogo: al elegir una entrada se activan los flags correspondientes y se expande el panel; no crea archivos nuevos: sigue siendo la misma instancia en JSON.",
                "En Play, PlayModeRunner.ObjectInstanceToGameObject crea los componentes Core según esos flags y valores. Los scripts Lua se enlazan después; el orden del frame es aproximadamente: onUpdate → movimiento rigidbody → física AABB → sensores de proximidad → onLateUpdate → seguimiento de cámara → avance de frames de sprite.",
                "Animaciones: defaultAnimationClipId + autoPlay aplican un clip al iniciar si hay SpriteComponent y el clip existe en animaciones.json. self.playAnimation(\"Nombre\") y stopAnimation modifican el SpriteComponent en caliente.",
                "Luces puntuales: pointLightEnabled y color en hex; LightComponent participa en el tinte del visor WPF del tab Juego.",
                "Tags: proximidad y world.findByTag usan la lista de etiquetas de la instancia (coma en el inspector). La etiqueta por defecto del sensor es «player».",
                "Si algo no se ve en Play: guarda la escena, revisa que el objeto tenga definición con sprite, que Enabled esté activo para scripts y que los scripts estén en scripts.json."
            },
            bullets: new[]
            {
                "JSON: propiedades camelCase (spriteColorTintHex, rigidbodyEnabled, proximitySensorEnabled, …).",
                "Física del proyecto: PhysicsGravity y PhysicsEnabled en configuración; el rigidbody usa escala de gravedad por objeto.",
                "Triggers de mapa (triggerZones.json) y triggers de objeto (Collider IsTrigger o ProximitySensor) comparten nombres de eventos Lua pero son sistemas distintos.",
                "Referencia detallada para IAs: docs/AI-ONBOARDING.md § 8.2."
            }),

        new(
            id: "triggers",
            title: "Triggers (zonas)",
            paraQue: "Rectángulos en el mapa que ejecutan scripts al entrar, salir o cada frame.",
            porQueImporta: "Evitas lógica de proximidad manual para portales, diálogos o checkpoints.",
            paragraphs: new[]
            {
                "Cada zona tiene tamaño en celdas, capa asociada, y scripts por evento (enter/exit/tick) referenciados por ID del registro de scripts.",
                "Se guardan en triggerZones.json en la raíz del proyecto (TriggerZonesPath en proyecto); la jerarquía lista las zonas para seleccionarlas y abrir el inspector dedicado.",
                "El contexto de ejecución es el de la zona (no hay self de objeto); los scripts deben estar registrados en scripts.json igual que los de objetos.",
                "No confundir con colliders IsTrigger ni con ProximitySensor en objetos: son tres sistemas distintos (ver tema «Triggers de mapa vs triggers de objeto»)."
            },
            bullets: new[]
            {
                "Útiles para áreas grandes (música ambiental, cinemática) sin depender de la física de objetos."
            }),

        new(
            id: "seeds",
            title: "Seeds (prefabs)",
            paraQue: "Reutilizar definiciones de objetos e instanciarlas por nombre desde Lua o el editor.",
            porQueImporta: "Separar «molde» de «instancia en escena» acelera iteración y mantiene coherencia.",
            paragraphs: new[]
            {
                "Un seed es un archivo .seed (JSON) más la entrada en seeds.json del proyecto; world.instantiate(\"nombre\", x, y) y self:instantiate(...) crean instancias en runtime.",
                "Variantes: el último parámetro opcional de instantiate selecciona variantes de seed con convención de nombres (prefab_variant).",
                "El proyecto de ejemplo puede incluir seeds demo enlazados a scripts de prueba.",
                "Los seeds registrados aparecen en seeds.json; al guardar un .seed desde el editor se fusiona el registro. Las instancias pueden llevar SourceSeedId y ruta del .seed para «Aplicar cambios al seed» desde el inspector.",
                "Spotlight en el editor indexa también archivos .seed; al confirmar uno se colocan instancias en el mapa (pestaña Mapa)."
            }),

        new(
            id: "scripting-lua",
            title: "Scripting Lua — referencia completa de APIs",
            paraQue: "Programar comportamiento en Play: entrada, movimiento, audio, UI y consultas.",
            porQueImporta: "Todo lo que el juego hace en runtime pasa por estas tablas inyectadas en el entorno NLua.",
            paragraphs: new[]
            {
                "Convención: scripts de objeto pueden definir onStart() y onUpdate(dt). El motor crea self como proxy del objeto dueño del ScriptComponent.",
                "Scripts de capa (mapa): se asignan en el Inspector de capa (campo de ruta o «Añadir componente…» al pie de ese inspector); ruta .lua relativa al proyecto. Tabla layer (offsetX, offsetY, parallaxX, parallaxY, opacity, …). Eventos: onAwake, onStart, onLayerUpdate(dt), onDestroy. Mismas APIs globales que un script de objeto salvo self.",
                "Constantes: Key.* (W, A, S, D, Space, …) y Mouse.* (Left=0, Right=1) para pasar a input.isKeyDown / isMouseDown.",
                "self (SelfProxy): id, name, tag, tags[], hasTag, x, y, rotation, scale, visible, active, renderOrder; move, rotate, destroy; find, findInHierarchy, getParent, getChildren, setParent; getComponent, removeComponent; setSpriteTexture, addSpriteFrame, clearSpriteFrames, spriteFrame, setSpriteAnimationFps, setSpriteSortOffset, setSpriteDisplaySize, setSpriteTint(r,g,b); playAnimation(clipId) / stopAnimation — aplican clips de animaciones.json al SpriteComponent en Play; instantiate(prefab, x, y, rot?, variant?).",
                "world: findObject / getObjectByName, findObjectByInstanceId(id), findByTag / getObjectByTag / getObjects / getAllObjects, findByPath, findNearestByTag, spawn/instantiate, destroy, setPosition, getPlayer, getTile(x,y,layerName), setTile(x,y,layerName,catalogId), raycast(origen, dir, maxDist, ignore?), raycastTiles, raycastCombined.",
                "input: isKeyDown, isKeyPressed, isMouseDown, mouseX, mouseY.",
                "time: delta, time, seconds, frame, scale.",
                "audio: play(id), play(id, volume), playMusic(id), playMusic(id, loop), playSfx, stopMusic(fade), setVolume(bus, 0..1), stop, stopAll, setMasterVolume.",
                "physics (Play suele usar PlayScenePhysicsApi): raycast(x1,y1,x2,y2), overlapCircle(cx, cy, radius) → lista de proxies.",
                "ui: show/hide/setFocus(canvasId), pushState/popState, get(canvasId, elementId), bind(canvasId, elementId, eventName, callback) — eventos click, hover, pressed, released; solo el canvas con foco recibe input.",
                "game: loadScene(name), quit(), setRandomSeed, randomInt, randomDouble.",
                "ads: loadInterstitial/loadRewarded (callbacks opcionales), showInterstitial/showRewarded (callbacks con bool de éxito), showBanner, isRewardedReady, setTestMode, setTagForChildDirectedTreatment. En modo Play del editor se usa un simulador (sin SDK real) que registra en consola.",
                "Debug (si está inyectado): drawLine, drawCircle — colores RGBA 0–255; útil para depurar en la vista de juego.",
                "ComponentProxy: typeName, invoke(method, ...), invokeWithResult(...). Para ScriptComponent llama métodos expuestos en la instancia Lua.",
                "Operadores y tipos Lua: == compara valores; tablas solo son iguales por identidad. Usa tonumber/tostring al mezclar números y cadenas desde UI o JSON.",
                "Iteración: for i = 1, #lista do con arrays densos; pairs para diccionarios; evita modificar la tabla que iteras salvo que sepas el comportamiento de Lua.",
                "Errores: un error en onUpdate puede detener la ejecución del script en ese frame; la consola muestra archivo y línea si el runtime lo reporta. pcall protege bloques experimentales."
            },
            bullets: new[]
            {
                "world.raycast alinea con colliders sólidos del host; physics.raycast es el segmento contra colliders de la escena física (API distinta).",
                "world.raycastCombined elige el impacto más cercano entre tiles y objetos.",
                "getPlayer devuelve el primer objeto llamado «Player» (comparación case-insensitive).",
                "Más detalle: tema «Física y raycast: mapa vs colliders en escena» (cuándo usar cada tabla).",
                "self.addComponent(typeName) puede devolver false si el host no tiene factory para ese tipo (añadir componentes en runtime no está garantizado).",
                "Sintaxis : vs . y patrones nil-safe: pestaña «Lua — sintaxis y librería» (guías colon-syntax, nil-safe-patterns, print-debug).",
                "Fragmentos de movimiento, cooldowns y dbggrid: tema «Lua: patrones y fragmentos útiles».",
                "Registrar script en el objeto, hitbox y partículas desde el inspector: «Conectar scripts con el motor» y «Objeto + script + Inspector: hitbox, partículas y @prop»."
            }),

        new(
            id: "editor-mini-ide-lua",
            title: "Mini-IDE de scripts (pestaña Scripts)",
            paraQue: "Editar Lua y JSON del registro con resaltado y sugerencias alineadas al runtime.",
            porQueImporta: "Reduce errores de tipeo y acelera recordar nombres de APIs sin salir del editor.",
            paragraphs: new[]
            {
                "La lista izquierda muestra scripts del registro del proyecto (scripts.json), el propio archivo scripts.json y otros .json bajo la carpeta Scripts/ cuando no están duplicados; el título incluye el nombre de archivo con extensión (p. ej. «Main · main.lua»).",
                "El editor usa resaltado de sintaxis Lua y JSON (definición Lua embebida en el motor, alineada al tema oscuro). El color base del texto no depende solo del tema de la ventana: el resaltado aplica tonos distintos a comentarios, cadenas y palabras clave. Mientras escribes, aparecen sugerencias para identificadores y miembros tras escribir un punto (world., self., ads., …). Ctrl+Espacio fuerza el menú de completado; Escape lo cierra. Las entradas muestran iconos pequeños (tablas, métodos, ads).",
                "Las palabras clave Lua vienen de LuaLanguageKeywords (misma lista que Spotlight). Cada una tiene tema propio en la pestaña «Lua — sintaxis y librería» del panel de documentación (Ayuda / Proyecto o enlace Lua (sintaxis) en el Hub); Spotlight (Ctrl+P) abre ese tema al confirmar la entrada.",
                "Los nombres tras «tabla.» se generan por reflexión desde clases [LuaVisible] en FUEngine.Runtime (jerarquía de tipos y campos públicos; incluye layer., component. tras getComponent, Key./Mouse., etc.); MergeDynamic sigue añadiendo globales extra.",
                "Menú contextual en la lista: abrir en el editor, abrir con aplicación externa o mostrar en el explorador de Windows.",
                "Snippets: escribe dbggrid y acepta la plantilla para insertar depuración de rejilla (Debug.drawGrid) en un onUpdate — útil para ver alineación mundo/tiles.",
                "Comillas y cadenas: usa comillas dobles o simples de forma consistente; escapa comillas internas si construyes rutas o ids en tiempo de ejecución."
            },
            bullets: new[]
            {
                "Si la lista aparece vacía o abre rutas incorrectas al elegir Main, el proyecto debe tener directorio asignado antes de cargar scripts (flujo normal al abrir un proyecto).",
                "Patrones Lua (movimiento, cooldowns): tema «Lua: patrones y fragmentos útiles»."
            }),

        new(
            id: "play-runtime",
            title: "Play, runtime y ventana de juego",
            paraQue: "Probar la escena con Vulkan, Lua y audio sin salir del flujo del editor.",
            porQueImporta: "El runtime no es el mismo que el canvas WPF: diferencias de rendimiento y de pipeline.",
            paragraphs: new[]
            {
                "Proyecto → Iniciar juego (escena actual o principal) arranca el bucle de Runtime con la escena cargada.",
                "La pestaña Juego puede mostrar el play embebido (jerarquía de runtime + viewport; sin inspector lateral — usa el Inspector en la pestaña Mapa). La jerarquía se actualiza con los objetos del runtime, incluidos los creados por scripts (p. ej. world.instantiate). Debug.draw* superpone primitivas en unidades de mundo del transform.",
                "Pausa / detener desde barra o menú cuando esté habilitado.",
                "Puede abrirse una ventana de juego nativa (Vulkan) además o en lugar del embebido según configuración; el píxel y el filtrado deben coincidir con proyecto.json en ambos casos cuando el backend lo respete.",
                "Si cambias de escena en el editor, guarda antes de Play para no probar datos viejos en disco."
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
                "overlapCircle devuelve proxies en la zona (incluye triggers según implementación). La geometría de cada objeto es un AABB derivado del ColliderComponent (incluido el modo Circle en datos, que usa mitades iguales).",
                "Collider dinámico vs estático: los cuerpos con tag player o dynamic participan como dinámicos; la masa en colisiones dinámico–dinámico viene del ColliderComponent.Mass (en instancias con rigidbody suele alinearse con la masa del rigidbody al crear el collider).",
                "Triggers: overlap entre un collider con IsTrigger y otro cuerpo genera onTriggerEnter y onTriggerExit en el script del objeto que lleva el trigger (el otro llega como argumento proxy)."
            },
            bullets: new[]
            {
                "physics.overlapCircle: círculo de consulta contra AABB de cada collider (punto más cercano del rectángulo al centro del círculo).",
                "Tiles sólidos: además de objetos, TryMoveDynamicAgainstTilemap empuja fuera de celdas con colisión del mapa."
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
                "Tinte de sprite por instancia (inspector): se multiplica después del tinte por luz en el visor WPF del tab Juego; color en hex #RRGGBB.",
                "audio.json: IDs usados por audio.play* en Lua; el editor puede usar NAudio para preescucha según configuración. AudioSource en el objeto guarda el clipId y volumen como metadatos; la reproducción sigue siendo audio.play desde el script.",
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
                "Proyecto → Exportar build del juego genera ejecutable y carpeta de datos para distribución (según ProjectBuildService / export helpers). En Data/ se escribe ads_export.json según AdsExportProvider del proyecto (simulado vs Google Mobile Ads planificado)."
            }),

        new(
            id: "archivos-json",
            title: "Archivos JSON típicos del proyecto",
            paraQue: "Saber qué tocar o qué romper al copiar carpetas.",
            porQueImporta: "El editor persiste casi todo en JSON con convenciones camelCase compartidas.",
            paragraphs: new[]
            {
                "Mapa: capas + chunks con tiles (referencia a LayerId por GUID).",
                "Objetos: capa de instancias (definitions + instances). Cada instancia incluye transform, scripts, tags, luz puntual y el bloque de componentes lógicos: spriteColorTintHex, spriteFlipX/Y, colliderShape, rigidbodyEnabled, proximitySensorEnabled, healthEnabled, cameraTargetEnabled, audioSourceEnabled, particleEmitterEnabled, etc. Ver tema «Componentes: inspector, objetos.json y Play».",
                "Triggers: zonas. Scripts: registro con rutas. Animaciones: clips para AnimationPlayer. Seeds: definiciones.",
                "UI: canvas (elementos y bindings). Audio: manifiesto. Proyecto: configuración global (resolución, FPS, chunk, rutas principales, PhysicsGravity).",
                "Carga parcial: muchos loaders devuelven vacío si falta el archivo; el proyecto principal suele exigir archivo.",
                "Temas relacionados en el manual: UI en runtime, Etiquetas, Integridad, Atajos, @prop, Streaming en Play."
            }),

        new(
            id: "render-pixel-art-filtros",
            title: "Render, pixel art y antialiasing",
            paraQue: "Elegir mentalidad visual: retro nítido vs suavizado, sin sorprenderte con bordes borrosos o parpadeo.",
            porQueImporta: "FXAA, MSAA y filtrado bilinear mejoran bordes en arte de alta resolución, pero suelen estropear el look de pixel art; la alternativa correcta es muestreo nearest + escala entera + resolución lógica fija.",
            paragraphs: new[]
            {
                "En estética pixel art lo habitual es no usar antialiasing de post-proceso (FXAA) ni multisample (MSAA) en el buffer principal: ambos mezclan píxeles vecinos y el resultado deja de ser «crisp».",
                "Lo que sí encaja: texturas y sprites con filtro de punto (nearest neighbor), cámara o viewport que alinee el mundo a la rejilla de píxeles (evitar subpíxeles de cámara si quieres cero shimmer), y una resolución interna fija (p. ej. 320×180) escalada a pantalla con enteros cuando sea posible.",
                "Movimiento en coordenadas decimales (Transform en float) es compatible con pixel art si el paso final redondea o snappea al raster como quieras; si notas parpadeo en scroll muy lento, suele ser mezcla de escala no entera o filtro bilinear, no falta de FXAA.",
                "MSAA a nivel de dispositivo (2×/4×/8×) ayuda a aristas geométricas en geometría vectorial o sprites rotados a alta resolución; para sprites de baja resolución en malla de píxeles fija, muchos equipos lo dejan desactivado.",
                "Un FXAA por capa (post en buffer de capa) sería coste bajo y opcional en un motor generalista, pero no es el valor por defecto para proyectos retro; si algún día se añade, habría que tratarlo como modo opcional explícito, no como sustituto de nearest + escala entera."
            },
            bullets: new[]
            {
                "Pixel art: prioridad a nearest, sin AA por defecto, escala entera.",
                "Arte HD o rotaciones suaves: ahí tienen sentido bilinear / MSAA / FXAA como opciones de proyecto o ventana.",
                "Proyecto → Editor avanzado del proyecto → pestaña Juego: Antialiasing global (ninguno / FXAA / MSAA reservado), muestras MSAA, filtrado de texturas (nearest vs bilinear). Se guardan en proyecto.json; el runtime Vulkan las aplicará cuando exista soporte.",
                "Inspector de capa → Añadir componente: catálogo con script Lua de capa (activo) y entradas reservadas (parallax automático, color grading, tilt-shift, clima, FXAA por capa, viento, agua, auto-tile, capa de daño)."
            }),

        new(
            id: "eventos-hooks-lua",
            title: "Eventos y hooks Lua (ciclo de vida)",
            paraQue: "Saber cuándo se ejecuta cada función y qué reservar como nombre de variable.",
            porQueImporta: "Confundir onAwake con onStart o spamear onUpdate cuesta rendimiento y bugs de orden.",
            paragraphs: new[]
            {
                "onAwake: se llama al crear la instancia del script, tras cargar el chunk y aplicar ScriptProperties; úsalo para inicializar referencias internas.",
                "onStart: una sola vez, antes del primer onUpdate; ideal para buscar otros objetos que ya existan en escena.",
                "onUpdate(dt): cada frame mientras el objeto esté activo (self.active) y no esté destruido. dt es el delta en segundos.",
                "onLateUpdate(dt): después de todos los onUpdate del mismo frame; útil para seguir al jugador o corregir tras física.",
                "onTriggerEnter / onTriggerExit: cuando un trigger (collider o sensor de proximidad) entra o sale de contacto; recibes el proxy del otro objeto.",
                "onDestroy: al final del tick en que el objeto se destruye (self.destroy o world.destroy).",
                "Scripts de capa del mapa: onLayerUpdate(dt) y tabla layer en lugar de self; mismas APIs globales salvo self.",
                "Otros nombres estándar (lista KnownEvents en Core): onInteract, onCollision, onFear, onSpawn, onRepair, onHack, onTrigger, onDayStart, onNightStart, onPlayerMove, onZoneEnter, onZoneExit, onChildAdded, onChildRemoved, onParentChanged. El editor y las plantillas de script los sugieren; muchos están pensados para juegos tipo FNaF o RPG. No asumas que todos se disparan solos en Play: depende de si el host del runtime conecta ese evento (p. ej. colisión sólida o interacción). Si tu gancho no se ejecuta, comprueba la versión del motor o implementa la lógica en onUpdate con consultas a physics/world.",
                "Los nombres onStart, onUpdate, etc. están reservados: no los uses como variables @prop ni globales editables en inspector para evitar conflictos."
            },
            bullets: new[]
            {
                "KnownEvents en FUEngine.Core enumera todos los hooks; IsReservedScriptVariableName evita usarlos como variables editables.",
                "Pausar lógica sin destruir: self.active = false."
            }),

        new(
            id: "depuracion-y-consola",
            title: "Depuración, consola y errores Lua",
            paraQue: "Encontrar fallos en scripts sin adivinar.",
            porQueImporta: "Los errores Lua suelen mostrar archivo y línea en la consola del editor.",
            paragraphs: new[]
            {
                "La consola del editor muestra mensajes del motor, del log de Lua (print) y errores con ruta cuando el runtime los reporta.",
                "Filtra por categoría (Info, advertencia, Lua, error) si el panel lo permite; los toasts resumen errores graves sin abrir el panel completo.",
                "Breakpoints: desde el flujo de Play puedes marcar líneas; el juego se pausa y puedes reanudar cuando el soporte esté activo.",
                "Debug.drawLine y Debug.drawCircle dibujan en coordenadas de mundo sobre la vista de juego embebida (RGBA 0–255).",
                "Si un script no corre: revisa que esté en scripts.json, asignado al objeto, que el objeto tenga Enabled activo y que no haya error de sintaxis en el chunk inicial.",
                "Hot reload al guardar .lua: la consola muestra si la recarga falló; entonces revisa el mensaje de error NLua.",
                "Stack de Lua a veces menciona [C#]; la línea útil suele ser la del archivo .lua indicada en el mensaje."
            },
            bullets: new[]
            {
                "Revisa también la pestaña Scripts: errores de carga aparecen al abrir o al guardar según configuración.",
                "ChunkEntitySleep: objetos fuera del radio de chunks pueden no recibir onUpdate (optimización).",
                "pcall en Lua: tema guía «pcall y xpcall» en la pestaña Lua.",
                "Discord: errores del RPC van a consola (categoría Discord) — ver tema «Discord (Rich Presence)»."
            }),

        new(
            id: "seeds-prefabs-runtime",
            title: "Seeds (prefabs) e instanciación",
            paraQue: "Clonar enemigos, cofres o efectos sin duplicar el mapa a mano.",
            porQueImporta: "Separar definición (seed) de instancia en escena acelera iteración.",
            paragraphs: new[]
            {
                "Un seed es un archivo de datos con objetos hijos y offsets; se registra en seeds.json del proyecto.",
                "Desde Lua: world.instantiate(\"nombre\", x, y, rotacion) o self:instantiate con variante opcional para convención nombre_variante.",
                "Los objetos creados en runtime reciben scripts al inicio del siguiente tick de simulación (cola de spawn), salvo anidación en onAwake.",
                "Los objetos colocados en el editor son instancias en objetos.json; no confundir con seeds, aunque compartan definiciones.",
                "En proyectos nuevos (Blank), el seed demo_square instancia un obj_default sin Lua propio; el rebote y los bordes del viewport están solo en Scripts/main.lua (script de capa Ground en la escena Start), registrado en seeds.json."
            },
            bullets: new[]
            {
                "Duplicar en el mapa: menú del inspector o jerarquía según versión del editor.",
                "Prefabs no son GameObject Unity: aquí son datos + PlayModeRunner."
            }),

        new(
            id: "scripts-capa-layer",
            title: "Scripts de capa del mapa (layer)",
            paraQue: "Mover parallax, niebla u offset de toda una capa sin pegar script a cada tile.",
            porQueImporta: "Un solo Lua por capa reduce carga y centraliza el comportamiento.",
            paragraphs: new[]
            {
                "En el inspector de la capa (o panel de capas) puedes asignar un .lua y propiedades como en objetos.",
                "El entorno Lua expone la tabla layer con offset, parallax, opacidad y campos que el motor rellene.",
                "Eventos típicos: onLayerUpdate(dt) cada frame; no hay self — el contexto es la capa.",
                "Proyecto nuevo por defecto: el demo del cuadrado que rebota en los bordes del área de juego vive solo en Scripts/main.lua en la capa Ground de la escena Start (world:instantiate del seed demo_square).",
                "Útil para scroll horizontal, oscurecer capas de fondo o sincronizar con música."
            },
            bullets: new[]
            {
                "Mismas APIs globales (world, input, time) que un script de objeto.",
                "Ruta del script relativa al directorio del proyecto."
            }),

        new(
            id: "ads-simulado",
            title: "Publicidad (tabla ads en Lua)",
            paraQue: "Probar flujo de anuncios recompensados o intersticiales sin SDK en el editor.",
            porQueImporta: "En Play embebido se usa un simulador; el build puede enlazar otro proveedor.",
            paragraphs: new[]
            {
                "La tabla ads ofrece loadInterstitial, showInterstitial, loadRewarded, showRewarded, showBanner y utilidades como setTestMode.",
                "En el editor las llamadas suelen registrarse en consola y ejecutar callbacks con éxito simulado.",
                "La exportación del proyecto puede generar ads_export.json para integrar con el runtime de distribución.",
                "No sustituye leer la documentación del proveedor real cuando publiques en tiendas."
            }),

        new(
            id: "input-raton-teclado",
            title: "Entrada: teclado y ratón",
            paraQue: "Leer WASD, espacio o clic en Play.",
            porQueImporta: "Las teclas disponibles y el ratón dependen del host (WPF) y del foco del visor.",
            paragraphs: new[]
            {
                "input.isKeyDown y isKeyPressed usan nombres como Key.W, Key.SPACE documentados en constantes.",
                "mouseX y mouseY están en píxeles del área del juego embebido, no en casillas del mundo.",
                "El proyecto puede activar movimiento nativo del protagonista (WASD) antes de Lua; tu script sigue recibiendo input.",
                "Espacio puede estar reservado para pausar el Play embebido en el editor según configuración."
            },
            bullets: new[]
            {
                "Para lógica por frame usa isKeyDown; para «un solo disparo» isKeyPressed."
            }),

        new(
            id: "tiempo-y-delta",
            title: "Tiempo: delta, escala y FPS del proyecto",
            paraQue: "Movimiento independiente del framerate.",
            porQueImporta: "Multiplicar velocidad por time.delta evita que el juego vaya más rápido en PCs potentes.",
            paragraphs: new[]
            {
                "time.delta es el delta en segundos del último tick; time.scale puede ralentizar o acelerar la simulación si el proyecto lo usa.",
                "El proyecto define Fps objetivo para el temporizador de Play en el editor.",
                "En onUpdate(dt) el argumento dt suele coincidir con el delta del motor para ese frame.",
                "Animaciones por FPS en datos de proyecto se combinan con multiplicadores por instancia en el inspector."
            }),

        new(
            id: "escenas-multiples",
            title: "Escenas y cambio de nivel",
            paraQue: "Organizar menú, nivel 1, nivel 2 con distintos mapas.",
            porQueImporta: "Cada escena apunta a archivos de mapa y objetos distintos.",
            paragraphs: new[]
            {
                "El proyecto puede listar varias SceneDefinition con rutas a mapa, objetos y UI.",
                "game.loadScene(name) en Lua pide al host cargar otra escena; el comportamiento exacto depende de la implementación del GameApi en el editor.",
                "Escena principal: campos MainMapPath y MainObjectsPath para iniciar desde menú sin abrir el editor.",
                "Guardar antes de cambiar de escena en el editor para no perder el mapa actual."
            },
            bullets: new[]
            {
                "SceneManager en Core es un modelo distinto al flujo multiescena del editor WPF."
            }),

        new(
            id: "proyecto-json-avanzado",
            title: "Proyecto: opciones que suelen olvidarse",
            paraQue: "Ajustar gravedad, chunk, RNG y cámara sin tocar código.",
            porQueImporta: "PhysicsGravity y ProtagonistInstanceId cambian mucho el feel del juego.",
            paragraphs: new[]
            {
                "Configuración del proyecto incluye resolución lógica, pixel perfect, filtros de textura y antialiasing reservado.",
                "PhysicsGravity y PhysicsEnabled: la gravedad del rigidbody usa el valor del proyecto multiplicado por el factor del objeto.",
                "RuntimeRandomSeed fija game.randomInt y randomDouble al iniciar para depuración reproducible.",
                "ProtagonistInstanceId identifica qué instancia en objetos.json es el jugador para input nativo y cámara.",
                "Volúmenes Master, Music y Sfx se aplican antes de reproducir audio en Play."
            }),

        new(
            id: "tilemap-catalogo",
            title: "Tiles: modo catálogo vs clásico",
            paraQue: "Elegir cómo pintar y qué poner en world.setTile.",
            porQueImporta: "El catálogo usa IDs de atlas; el modo clásico usa tipos e imágenes por celda.",
            paragraphs: new[]
            {
                "En catálogo, cada tile del mapa guarda un CatalogTileId que el tileset resuelve a rectángulo en textura.",
                "world.setTile(layerName, tx, ty, catalogId) modifica el mapa en runtime y puede marcar chunks para no perder cambios al hacer streaming.",
                "Las capas sólidas hacen que cualquier tile ocupe celda de colisión según reglas del TileMap.",
                "Auto-tiling puede aplicar variantes según vecinos (máscara de bits)."
            }),

        new(
            id: "conocimiento-worldapi",
            title: "World: buscar objetos y raycasts",
            paraQue: "Encontrar enemigos, pickups o el suelo bajo los pies.",
            porQueImporta: "world.raycast y physics.raycast no son idénticos: uno incluye criterio de host y tiles combinados.",
            paragraphs: new[]
            {
                "findByTag y getObjects devuelven proxies o listas según API; revisa la firma en autocompletado.",
                "world.raycastCombined devuelve el impacto más cercano entre tiles y objetos.",
                "world.getPlayer busca por nombre Player; el protagonista por proyecto puede ser otra instancia.",
                "destroy(objeto) marca destrucción al final del frame; el objeto deja de aparecer en consultas inmediatas salvo diseño del host."
            },
            bullets: new[]
            {
                "physics.overlapCircle solo ve colliders, no el tilemap.",
                "findNearestByTag es útil para IA que persigue al jugador."
            }),

        new(
            id: "hot-reload-scripts",
            title: "Hot reload de scripts .lua",
            paraQue: "Iterar código sin reiniciar Play cada vez.",
            porQueImporta: "El motor recrea instancias del script tocado y vuelve a ejecutar onAwake y onStart.",
            paragraphs: new[]
            {
                "Al guardar un .lua en disco, un vigilante de archivos notifica al PlayModeRunner.",
                "Se destruyen las instancias viejas del script y se crean nuevas con el código actualizado.",
                "Si el nuevo código falla al cargar, verás el error en consola y el comportamiento anterior puede no aplicarse.",
                "No sustituye guardar el proyecto: objetos.json y mapa siguen en disco solo cuando guardas desde el editor."
            }),

        new(
            id: "autoguardado-editor",
            title: "Autoguardado del editor",
            paraQue: "Recuperar trabajo tras cierre inesperado.",
            porQueImporta: "El motor puede escribir .tmp y backups según configuración global y del proyecto.",
            paragraphs: new[]
            {
                "EngineSettings y ProjectInfo tienen flags de intervalo y carpeta de autoguardado.",
                "Los mapas pueden guardarse por chunk o como archivo único según el proyecto.",
                "Revisa la consola si el autoguardado falla por permisos o disco lleno."
            }),

        new(
            id: "triggers-mapa-vs-objeto",
            title: "Triggers de mapa vs triggers de objeto",
            paraQue: "No mezclar rectángulos de triggerZones.json con colliders IsTrigger.",
            porQueImporta: "Son datos y pipelines distintos aunque ambos disparen scripts.",
            paragraphs: new[]
            {
                "Las zonas en triggerZones.json son rectángulos en celdas con scripts por entrar, salir o tick.",
                "Los colliders con IsTrigger en objetos generan onTriggerEnter cuando otro cuerpo se solapa en el paso de física.",
                "El ProximitySensor no usa segundo collider: mide distancia al objetivo con tag.",
                "Puedes usar ambos en el mismo nivel: zona grande para música y trigger pequeño en una puerta."
            }),

        new(
            id: "exportacion-build",
            title: "Exportar build del juego",
            paraQue: "Generar ejecutable y carpeta Data para distribuir.",
            porQueImporta: "El flujo copia assets y puede generar metadatos de anuncios u otros JSON.",
            paragraphs: new[]
            {
                "Desde el menú Proyecto, exportar build empaqueta según ProjectBuildService y ventanas asociadas.",
                "Incluye ejecutable, datos del proyecto y recursos referenciados cuando la ruta es correcta.",
                "ads_export.json u otros ficheros auxiliares pueden crearse en la carpeta de salida (integración anuncios según AdsExportProvider del proyecto).",
                "Prueba siempre el .exe generado fuera del editor en una carpeta limpia.",
                "Antes de distribuir: ejecuta integridad del proyecto y revisa que audio.json y rutas de assets apunten a archivos incluidos en el paquete.",
                "El build del juego es distinto de compilar el editor FUEngine desde el código (README.md: tools\\publicar.bat)."
            }),

        new(
            id: "troubleshooting-comun",
            title: "Problemas frecuentes (FAQ técnico)",
            paraQue: "Descartar causas típicas antes de depurar a fondo.",
            porQueImporta: "Muchos informes son «no guardé» o «script no registrado».",
            paragraphs: new[]
            {
                "No veo cambios en Play: guarda escena y mapa (Ctrl+S / Guardar todo); comprueba que Play use escena actual o principal según el botón.",
                "El script no hace nada: revisa scripts.json, asignación al objeto, Enabled del objeto y errores en consola.",
                "onCollision u onInteract no se ejecutan: además de registro y sintaxis, comprueba si tu versión del runtime dispara ese evento; mientras tanto usa onUpdate + overlap o raycast.",
                "El personaje no se mueve: activa UseNativeInput o implementa movimiento en Lua; revisa colisión con tilemap.",
                "No suena el audio: comprueba audio.json, rutas de archivos y volúmenes del proyecto.",
                "La cámara no sigue: ProtagonistInstanceId, UseNativeCameraFollow o un objeto con CameraTarget.",
                "Física rara: recuerda que el círculo en datos es AABB; overlapCircle consulta contra esos rectángulos."
            },
            bullets: new[]
            {
                "Documentación técnica extendida: docs/AI-ONBOARDING.md en la raíz del repositorio."
            }),

        new(
            id: "etiquetas-tags",
            title: "Etiquetas (tags) en objetos y Lua",
            paraQue: "Filtrar enemigos, el jugador o pickups sin buscar por nombre.",
            porQueImporta: "world.findByTag y el ProximitySensor dependen de tags bien asignados.",
            paragraphs: new[]
            {
                "En el inspector, el campo de etiquetas separa valores por comas; se guardan en la instancia del objeto.",
                "En Play, GameObject.Tags se copia desde ObjectInstance; self.tags devuelve un array y self.hasTag(\"nombre\") comprueba sin distinguir mayúsculas.",
                "world.findNearestByTag(x, y, \"tag\") es útil para IA; findByTag devuelve el primero que coincida.",
                "Los tags player y dynamic influyen en si el collider se trata como cuerpo dinámico en la física de escena.",
                "self.tag en Lua devuelve solo la primera etiqueta (compatibilidad); para varias usa self.tags."
            }),

        new(
            id: "ui-canvas-runtime",
            title: "UI en runtime (canvas y tabla ui)",
            paraQue: "Menús, HUD y botones con callbacks Lua.",
            porQueImporta: "Sin foco de canvas el input no llega a los elementos.",
            paragraphs: new[]
            {
                "Los canvas se definen en datos de UI del proyecto y se cargan en el backend de Play (UIRuntimeBackend).",
                "ui.show(canvasId) y ui.hide hacen visibles capas; ui.setFocus(canvasId) decide qué canvas recibe eventos de ratón y teclado.",
                "ui.pushState y popState apilan pantallas (menú sobre juego); evita profundidades excesivas.",
                "ui.bind(canvasId, elementId, \"click\", function() ... end) enlaza eventos; el elemento debe existir en el canvas.",
                "ui.get devuelve datos del elemento para leer texto o estado según implementación."
            },
            bullets: new[]
            {
                "Si un clic no hace nada: revisa foco, id de elemento y que el canvas esté visible."
            }),

        new(
            id: "biblioteca-global-assets",
            title: "Biblioteca global y assets",
            paraQue: "Importar recursos al proyecto sin copiar archivos a mano fuera del editor.",
            porQueImporta: "Centraliza rutas y evita duplicar gigabytes entre proyectos.",
            paragraphs: new[]
            {
                "El menú Assets y servicios como GlobalAssetLibrary permiten traer texturas, sonidos u otros tipos al directorio del proyecto.",
                "Las rutas guardadas en JSON suelen ser relativas al proyecto; mueve assets desde el explorador integrado cuando sea posible.",
                "La biblioteca global del motor es una capa aparte del disco: revisa permisos y espacio al importar lotes grandes."
            }),

        new(
            id: "integridad-proyecto",
            title: "Integridad del proyecto y limpieza",
            paraQue: "Encontrar referencias rotas o assets huérfanos antes del build.",
            porQueImporta: "Reduce errores en runtime y tamaño de exportación.",
            paragraphs: new[]
            {
                "Herramientas de integridad y escaneo de no usados recorren mapas, objetos, scripts y rutas de archivos.",
                "Los diálogos de limpieza pueden eliminar o listar huérfanos; confirma antes de borrar en masa.",
                "Tras mover carpetas fuera del editor, vuelve a abrir el proyecto y ejecuta comprobación si algo falla al cargar."
            }),

        new(
            id: "atajos-teclado",
            title: "Atajos de teclado del editor",
            paraQue: "Trabajar más rápido sin cazar menús.",
            porQueImporta: "Los atajos son configurables y pueden chocar con otras apps.",
            paragraphs: new[]
            {
                "EditorShortcutRegistry mapea acciones de guardar, deshacer, herramientas del mapa y paneles.",
                "Settings del motor incluye ShortcutBindings y presets (estilo Unity, Photoshop, etc.).",
                "La ventana de atajos (según menú Ayuda o Configuración del motor) lista combinaciones actuales.",
                "Atajos habituales (pueden variar con el preset): Ctrl+S guardar mapa; Ctrl+Shift+S guardar todo; Ctrl+Z / Ctrl+Y deshacer y rehacer; Ctrl+P o Ctrl+Espacio Spotlight (Hub y editor); Ctrl+Espacio en el mini-IDE Lua fuerza autocompletado.",
                "Los atajos no cambian las constantes Key.* en Lua: esas son solo para el juego en Play."
            }),

        new(
            id: "creative-suite",
            title: "Creative Suite (tiles, pintura, colisiones)",
            paraQue: "Crear o editar gráficos y datos de colisión ligados al flujo de tilesets.",
            porQueImporta: "Evita round-trip constante con Photoshop para tareas simples.",
            paragraphs: new[]
            {
                "Incluye TileCreator, TileEditor, PaintCreator, PaintEditor, CollisionsEditor y ScriptableTile según el menú del proyecto.",
                "Se integran con rutas de assets y catálogos de tiles; guarda desde cada herramienta antes de volver al mapa.",
                "Para audio complejo o música usa editores externos; el motor usa NAudio para escucha en editor, no un DAW integrado."
            }),

        new(
            id: "copiar-pegar-mapa",
            title: "Copiar, pegar y portapapeles en el mapa",
            paraQue: "Duplicar habitaciones o bloques de decoración.",
            porQueImporta: "Integra con undo/redo y debe respetar límites de tamaño.",
            paragraphs: new[]
            {
                "ZoneClipboard y servicios asociados copian regiones de tiles (y opcionalmente objetos según versión).",
                "Menú Editar y atajos suelen cubrir copiar, pegar y duplicar selección.",
                "Operaciones muy grandes pueden estar limitadas para no bloquear la UI.",
                "El bucket fill y el pintado masivo tienen topes de celdas para evitar colgar el editor."
            }),

        new(
            id: "componentproxy-invoke",
            title: "ComponentProxy y getComponent en Lua",
            paraQue: "Llamar métodos en componentes C# desde scripts.",
            porQueImporta: "No todos los componentes exponen métodos útiles sin trabajo extra en el motor.",
            paragraphs: new[]
            {
                "self:getComponent(\"HealthComponent\") devuelve un proxy con typeName y métodos invoke(\"takeDamage\", cantidad) si el tipo los expone al Lua.",
                "Si invoke falla, el componente puede no tener método público marcado o el nombre no coincide.",
                "ScriptComponent se enlaza con la instancia NLua para lógica de diálogo entre scripts.",
                "self.addComponent(\"Tipo\") suele devolver false hasta que exista factory en el host; no es un «AddComponent» estilo Unity completo.",
                "Para nuevos componentes con API Lua, el equipo de motor debe añadir puentes o usar patrones de datos + eventos."
            }),

        new(
            id: "game-api-rng",
            title: "game: RNG, escenas y salida",
            paraQue: "Aleatoriedad determinista y cambio de nivel.",
            porQueImporta: "Depurar bugs de RNG requiere semilla fija.",
            paragraphs: new[]
            {
                "game.setRandomSeed(n) fija el generador usado por randomInt y randomDouble.",
                "RuntimeRandomSeed en proyecto.json puede forzar semilla al iniciar Play para pruebas reproducibles.",
                "game.loadScene(nombre) pide cargar otra escena; la implementación concreta está en el host del editor.",
                "game.quit() termina la ejecución según el host (puede cerrar ventana o volver al editor)."
            }),

        new(
            id: "tipos-de-capa",
            title: "Tipos de capa del mapa (LayerType)",
            paraQue: "Elegir si una capa bloquea movimiento o solo pinta fondo.",
            porQueImporta: "Solid marca colisión por celda ocupada; Background no.",
            paragraphs: new[]
            {
                "Background: pintura sin usar la celda como muro automático en IsCollisionAt.",
                "Solid: cualquier tile en la celda cuenta como colisión para personajes y raycasts de tiles.",
                "Objects y Foreground: objetos y decoración delante; flags como RenderAbovePlayer controlan orden respecto al jugador.",
                "Parallax, opacidad y offsets en capa permiten profundidad falsa 2D.",
                "Cada capa tiene GUID en JSON para que los chunks sigan enlazados aunque reordenes la lista."
            }),

        new(
            id: "multiseleccion-objetos",
            title: "Multiselección de objetos",
            paraQue: "Editar o mover varias instancias a la vez.",
            porQueImporta: "El inspector cambia a modo resumido cuando hay más de uno seleccionado.",
            paragraphs: new[]
            {
                "MultiObjectInspector muestra propiedades comunes o limitadas según implementación.",
                "Para edición fina de un solo objeto, selecciona solo ese nodo en la jerarquía.",
                "Algunas acciones masivas pueden no estar disponibles en todas las versiones del editor."
            }),

        new(
            id: "version-motor-compatibilidad",
            title: "Versión del motor y compatibilidad",
            paraQue: "Saber por qué un proyecto antiguo muestra advertencias.",
            porQueImporta: "El motor compara el formato interno del JSON del proyecto con la versión actual.",
            paragraphs: new[]
            {
                "EngineVersion.Current en FUEngine.Core indica la versión del ejecutable del editor.",
                "El archivo del proyecto (Project.FUE) incluye projectFormatVersion (esquema interno) y engineVersion. Si abres un proyecto guardado con un formato anterior, el editor puede ofrecer actualizar el archivo de forma segura (solo campos añadidos; no se borran mapas ni assets). Puedes abrir sin guardar esa actualización y seguir trabajando; se volverá a preguntar al abrir.",
                "ProjectEngineCompatibilityChecker puede advertir si la versión del motor del proyecto difiere del ejecutable.",
                "No se garantiza compatibilidad de plugins de terceros entre versiones del motor."
            }),

        new(
            id: "consola-log-niveles",
            title: "Consola del editor y niveles de log",
            paraQue: "Filtrar ruido y ver solo errores Lua.",
            porQueImporta: "EditorLog y toasts ayudan sin abrir archivos de log en disco.",
            paragraphs: new[]
            {
                "Categorías típicas: información general, advertencias, errores Lua con ruta.",
                "Print desde Lua suele ir al nivel Lua o Info según configuración.",
                "Toasts muestran avisos breves sin abrir el panel completo.",
                "MaxEntries limita cuántas líneas se mantienen en memoria."
            }),

        new(
            id: "propiedades-script-at-prop",
            title: "Variables de script: @prop y heurística",
            paraQue: "Exponer números, bools y referencias a objetos en el inspector sin código adicional.",
            porQueImporta: "Sin @prop el parser adivina líneas globales en la raíz del archivo.",
            paragraphs: new[]
            {
                "La sintaxis recomendada es comentario en la primera línea del bloque: -- @prop nombre: tipo = valor.",
                "Tipos comunes: int, float, bool, string, object (InstanceId de otro objeto en escena).",
                "Los nombres de eventos reservados (onUpdate, onStart, …) no deben usarse como variables editables.",
                "En Play con depuración, el inspector puede sincronizar valores con Lua si la función está implementada (LiveVariables / hot variables según versión).",
                "MergeDynamic y otros mecanismos pueden añadir globales extra al entorno NLua.",
                "Ejemplo: -- @prop velocidad: float = 4.5 — aparece en el inspector con tipo acotado; object referencia otro InstanceId de la escena para enlazar entidades en datos.",
                "Variables con el mismo nombre que hooks reservados (onUpdate, onStart…) no deben usarse como @prop: el motor las ignora o entra en conflicto — ver KnownEvents."
            },
            bullets: new[]
            {
                "Ver LuaScriptVariableParser y documentación técnica en AI-ONBOARDING.md §11.",
                "Multiselección: el inspector de varios objetos puede no mostrar @prop; edita uno a la vez para valores por instancia.",
                "Flujo completo objeto + lista de scripts + hitbox/partículas: tema «Objeto + script + Inspector: hitbox, partículas y @prop»."
            }),

        new(
            id: "native-protagonist-camera",
            title: "Protagonista nativo, cámara y animación",
            paraQue: "Mover al jugador con teclado sin Lua y seguirlo con la cámara.",
            porQueImporta: "Opciones de proyecto compiten con lógica propia en scripts.",
            paragraphs: new[]
            {
                "UseNativeInput: WASD mueve al objeto protagonista antes de Lua en cada tick.",
                "UseNativeCameraFollow y NativeCameraSmoothing centran el visor en el jugador o en CameraTarget.",
                "ProtagonistInstanceId en proyecto apunta al InstanceId de la instancia en objetos.json.",
                "UseNativeAutoAnimation: clips IdleWalk desde animaciones.json según movimiento.",
                "AutoFlipSprite invierte escala X del sprite según dirección; puede interactuar con flip del inspector."
            },
            bullets: new[]
            {
                "Si quieres control total en Lua, desactiva input nativo y mueve self.x/y en onUpdate."
            }),

        new(
            id: "streaming-cache-play",
            title: "Streaming de chunks y caché en Play",
            paraQue: "Entender por qué el mapa cambia de disco al alejarse del jugador.",
            porQueImporta: "setTile en runtime marca chunks para no perder estado al descargar.",
            paragraphs: new[]
            {
                "ChunkLoadRadius y ChunkStreamEvictMargin definen qué zona permanece en memoria alrededor de la cámara.",
                "ChunkEntitySleep: objetos fuera del radio pueden no ejecutar onUpdate para ahorrar CPU.",
                "ChunkStreamSpillRuntimeEmpty puede volcar chunks vacíos modificados a caché en disco.",
                "La carpeta .fue_play_chunk_cache bajo el proyecto almacena datos temporales de streaming por sesión.",
                "Evitar fugas: no asumas que todo el mapa está cargado siempre en memoria."
            }),

        new(
            id: "vulkan-ventana-juego",
            title: "Vulkan y ventana de ejecución",
            paraQue: "Entender qué backend dibuja fuera del editor WPF.",
            porQueImporta: "IGraphicsDevice es distinto del Canvas del mapa.",
            paragraphs: new[]
            {
                "FUEngine.Graphics.Vulkan implementa IGraphicsDevice con Silk.NET Vulkan y GLFW para ventana nativa.",
                "BeginFrame, Clear, EndFrame y el color de fondo son la API mínima expuesta al runtime.",
                "El tab Juego embebido en el editor usa WPF y GameViewportRenderer; no es obligatoriamente el mismo código que la ventana Vulkan.",
                "En letterbox, las franjas y el rectángulo de resolución lógica usan DefaultFirstSceneBackgroundColor del proyecto (hex #RRGGBB; por defecto blanco en proyectos nuevos).",
                "GameLoop.Start puede crear VulkanGraphicsDevice cuando no hay otro dispositivo inyectado."
            },
            bullets: new[]
            {
                "Pixel art: coherencia con proyecto.json (filtro, resolución lógica) al implementar el backend."
            }),

        new(
            id: "naudio-audio-proyecto",
            title: "NAudio, manifiesto y buses de volumen",
            paraQue: "Configurar música y efectos sin saturar ni silenciar todo.",
            porQueImporta: "Master, Music y Sfx se encadenan en el motor de Play del editor.",
            paragraphs: new[]
            {
                "audio.json mapea IDs lógicos a rutas de archivo OGG/WAV según exportación del proyecto.",
                "Proyecto → MasterVolume, MusicVolume, SfxVolume: 0 a 1 típicamente.",
                "PlayNaudioAudioEngine carga el manifiesto al iniciar Play y aplica volúmenes.",
                "StartupMusicPath y StartupSoundPath pueden sonar al arrancar si existen y rutas son válidas."
            }),

        new(
            id: "avalonedit-scripts-ide",
            title: "Pestaña Scripts: AvalonEdit y autocompletado",
            paraQue: "Editar Lua con resaltado y sugerencias alineadas al runtime.",
            porQueImporta: "Lua.xshd y el catálogo de APIs deben mantenerse cuando cambies SelfProxy o WorldApi.",
            paragraphs: new[]
            {
                "AvalonEdit proporciona el editor de texto con resaltado definido por Lua.xshd.",
                "Ctrl+Espacio fuerza el menú de completado; world., self., ads., etc. vienen de reflexión y catálogo.",
                "LuaEditorCompletionCatalog y clases marcadas con LuaVisible definen qué aparece en sugerencias.",
                "Al añadir métodos públicos a las APIs, actualiza también la ayuda del motor y AI-ONBOARDING si es visible al usuario."
            }),

        new(
            id: "plugins-y-extensiones",
            title: "Plugins del proyecto (estado)",
            paraQue: "Saber qué esperar de ProjectEnabledPlugins.",
            porQueImporta: "La carga real de DLL puede estar incompleta o ser stub.",
            paragraphs: new[]
            {
                "ProjectInfo lista ProjectEnabledPlugins como nombres o ids de extensión.",
                "PluginLoader en el motor puede no cargar ensamblados externos en todas las builds.",
                "Antes de depender de un plugin, comprueba el código fuente y el comportamiento en depuración.",
                "Para lógica de juego portable, prioriza scripts Lua y datos en JSON."
            }),

        new(
            id: "jerarquia-gameobject-lua",
            title: "Jerarquía: padre, hijos y búsqueda",
            paraQue: "Organizar menús compuestos o enemigos con armas hijas.",
            porQueImporta: "setParent y findInHierarchy dependen de la implementación en SelfProxy.",
            paragraphs: new[]
            {
                "self:setParent(parentProxy) o nil para quitar padre; getParent y getChildren devuelven proxies.",
                "find(\"nombre\") busca en un nivel de hijos; findInHierarchy usa rutas con separador estilo jerárquico.",
                "Eventos onChildAdded, onParentChanged se disparan si el motor los conecta al cambiar jerarquía.",
                "El orden de render de hijos puede seguir el orden en la lista interna de Children."
            }),

        new(
            id: "bootstrap-script",
            title: "BootstrapScriptId (aviso)",
            paraQue: "No esperar un script global automático al pulsar Play.",
            porQueImporta: "El campo existe en proyecto pero el arranque de Play no siempre lo ejecuta.",
            paragraphs: new[]
            {
                "BootstrapScriptId en ProjectInfo puede apuntar a un script de inicialización deseado por diseño.",
                "En el flujo actual del editor, PlayModeRunner no carga ese script por defecto al iniciar.",
                "Usa game.loadScene, un objeto en escena con script de arranque, o amplía el motor si necesitas bootstrap explícito.",
                "Consulta docs técnicos AI-ONBOARDING §20 para limitaciones similares."
            }),

        new(
            id: "tiledata-collision-flags",
            title: "Tiles: datos por celda y colisión",
            paraQue: "Entender por qué una celda bloquea o no.",
            porQueImporta: "Solid marca colisión distinto que Background.",
            paragraphs: new[]
            {
                "TileData almacena tipo, catálogo, flags de colisión y metadatos según el modo clásico o catálogo.",
                "IsCollisionAt en TileMap usa LayerType: en Solid cualquier tile con tile en la celda suele bloquear.",
                "Background y otras capas de decoración no deben usarse como única barrera si no están marcadas como sólidas.",
                "Auto-tiling puede rellenar variantes según vecinos sin cambiar la capa de colisión."
            }),

        new(
            id: "deshacer-rehacer-editor",
            title: "Deshacer y rehacer en el editor",
            paraQue: "Recuperar tras un error de pintura sin cerrar el archivo.",
            porQueImporta: "El historial no es infinito.",
            paragraphs: new[]
            {
                "El editor mantiene una pila de comandos para mapa, objetos y otras vistas según implementación.",
                "Tras muchas operaciones, los pasos antiguos se descartan; guarda con Ctrl+S con frecuencia.",
                "Operaciones masivas pueden registrarse como un solo comando para no llenar la pila.",
                "No sustituye control de versiones: usa Git u otra copia de seguridad para proyectos largos."
            }),

        new(
            id: "pantalla-inicio-hub",
            title: "Pantalla de inicio y proyectos recientes",
            paraQue: "Abrir el trabajo rápido o crear plantilla sin perder contexto del motor.",
            porQueImporta: "StartupBehavior en settings decide si abrir Hub, último proyecto o proyecto nuevo.",
            paragraphs: new[]
            {
                "El Hub (primera pestaña) muestra proyectos fijados y recientes con miniatura del mapa (snapshot, primera escena o mapa.json), y un resumen «N escenas · M objetos» cuando puede calcularse.",
                "Panel «Estado del motor»: último autoguardado detectado entre carpetas Autoguardados/Mapa de los proyectos recientes; conteo de la biblioteca global (texturas tileset/sprite/imagen/UI y .lua en el índice); versión del ejecutable con enlaces a AI-ONBOARDING (docs) y a esta documentación.",
                "Acciones rápidas: pestaña «Scripts globales» (lista + ScriptEditorControl con resaltado y autocompletado en SharedAssets/Scripts); «Biblioteca» lleva a la pestaña Assets; «No usados» abre el escáner; botón «Buscar en el motor…» abre el panel Spotlight en esta ventana (sin atajo de teclado).",
                "Tips rotativos y recordatorio de atajos; barra inferior: resumen de líneas [Error]/[Critical] en el log de sesión del día y uso aproximado de RAM del proceso; botones «Carpeta» (abre LocalApplicationData/FUEngine/logs) y «Limpiar» (vacía el .log de hoy en disco).",
                "Crear proyecto en el Hub muestra el asistente en un overlay (datos básicos, mapa y tiles: tamaño de tile y de chunk con mini vista previa; sin mapa infinito ni plantilla/paleta en el asistente — la paleta por defecto está en Configuración del motor → Motor). «Generar jerarquía estándar» y carpetas extra/temas: pestaña Explorador. En el editor, Proyecto → Nuevo proyecto usa el mismo control. Recientes en AppData: Storage/project_history.json (migración desde recent.json). Miniaturas del Hub: ProjectThumbs/*.png al usar Guardar todo. Al borrar o refrescar, el scroll de las listas vuelve arriba.",
                "FUEngine Spotlight (Ctrl+P o Ctrl+Espacio): mismo buscador integrado en el Hub o en el editor (overlay en la ventana actual, no una ventana extra). Búsqueda unificada con totales del índice, coincidencias por categoría y lista agrupada; incluye manual integrado, Lua (API por reflexión con textos «para qué sirve» en LuaSpotlightDescriptions, hooks KnownEvents, palabras clave con tema dedicado en la pestaña «Lua — sintaxis y librería», biblioteca estándar; el juego en ejecución solo expone parte de la biblioteca Lua — ver LuaEnvironment), archivos .lua/.map/.seed, objetos en escena; en el Hub también proyectos recientes. Confirmar una palabra clave Lua abre su tema concreto en esa pestaña; hooks y API siguen enlazando al manual general. «Novedades» / onboarding abre docs/AI-ONBOARDING.md.",
                "Pulsa un resultado de documentación para abrir el tema en el panel de ayuda sin salir del flujo: el manual in-app y la referencia Lua comparten el mismo host (dos pestañas: manual general y Lua).",
                "Desde Spotlight puedes saltar a un hook concreto (onUpdate, onLayerUpdate…) y leer el párrafo asociado; combínalo con el tema «Eventos y hooks Lua» en el manual completo."
            },
            bullets: new[]
            {
                "Pestaña Assets: mismo índice de biblioteca global que alimenta el resumen del Hub.",
                "Referencia técnica del repo: docs/AI-ONBOARDING.md (enlace en Hub y Spotlight). docs/CHANGELOG.md opcional desde Spotlight si buscas «changelog».",
                "Discord: estado del motor en el perfil — tema «Discord (Rich Presence)»."
            }),

        new(
            id: "asistente-nuevo-proyecto-jerarquia",
            title: "Crear proyecto: Explorador (carpetas) y asistente",
            paraQue: "Controlar qué carpetas extra aparecen en la raíz y qué valores por defecto lleva el proyecto nuevo.",
            porQueImporta: "Config/user_preferences.json (antes settings.json en la raíz de AppData) guarda orden estándar, extras y tema; el asistente valida nombre y ruta antes de crear.",
            paragraphs: new[]
            {
                "En el asistente (Hub o Proyecto → Nuevo proyecto) marca «Generar jerarquía estándar» para crear en la raíz el orden Sprites, Maps, Scripts, Audio, Seeds (o tu lista personalizada si eliges Personalizado en Configuración del motor → Explorador).",
                "La pestaña Explorador añade carpetas que no sustituyen al motor: «Carpetas adicionales» (una por línea, p. ej. un nombre libre) y un «Tema» opcional (UI+Prefabs, Jam, Contenido…) que suma nombres predefinidos. Todo se fusiona sin duplicar nombres.",
                "El mapa nuevo es finito en el asistente: eliges tamaño de tile (px) y tamaño de chunk; por defecto el motor crea una cuadrícula 4×4 de chunks vacíos (16 chunks en el .map). La paleta del proyecto toma el valor por defecto de la pestaña Motor, no del asistente.",
                "Color de fondo de la primera escena (hex #RRGGBB) en Render y UI inicial; «Modo depuración» en opciones avanzadas puede heredar el valor por defecto de Explorador. Logs automáticos del motor (Avanzado): por defecto activados en settings.",
                "El botón «Crear proyecto» solo se activa con nombre de carpeta válido en Windows y ruta usable. Clic fuera del overlay cancela."
            }),

        new(
            id: "idioma-tema-editor",
            title: "Idioma, tema y escala de la UI del editor",
            paraQue: "Adaptar el IDE a tu pantalla y preferencias.",
            porQueImporta: "No cambia el idioma del juego exportado por sí solo.",
            paragraphs: new[]
            {
                "EngineSettings incluye Language, Theme y UiScalePercent para la aplicación WPF.",
                "Fuentes del editor y colores del tema son locales al IDE.",
                "El juego en Play usa textos de scripts y datos de UI serializados, no la traducción del editor.",
                "Para localizar el juego, mantén strings en Lua o JSON de UI por idioma."
            }),

        new(
            id: "medidores-gameplay-flags",
            title: "Fear, Danger y sombras (flags de proyecto)",
            paraQue: "Recordar flags de ProjectInfo para juegos de terror o tensión.",
            porQueImporta: "La lógica completa puede vivir en scripts o módulos específicos.",
            paragraphs: new[]
            {
                "FearMeterEnabled y DangerMeterEnabled activan o preparan sistemas de miedo o peligro según diseño del proyecto.",
                "LightShadowDefault puede influir en iluminación por defecto en pipelines que lo soporten.",
                "No asumas que la UI del editor muestra medidores completos si el proyecto no los usa.",
                "Combina con Lua en onUpdate para subir o bajar tensión según reglas de juego."
            }),

        new(
            id: "lua-completion-catalogo",
            title: "Autocompletado Lua y catálogo de APIs",
            paraQue: "Mantener sugerencias al día con el código.",
            porQueImporta: "MergeDynamic y reflexión pueden divergir si no se actualizan.",
            paragraphs: new[]
            {
                "LuaEditorCompletionCatalog y reflexión sobre clases LuaVisible alimentan el menú tras escribir un punto.",
                "layer. aparece en scripts de capa; self. y world. en scripts de objeto.",
                "Tras renombrar métodos en C#, revisa NLua y el catálogo para evitar sugerencias rotas.",
                "La documentación in-app (este manual) y AI-ONBOARDING.md son la fuente de verdad para comportamiento descriptivo."
            }),

        new(
            id: "particulas-render-estado",
            title: "Partículas y trail: datos guardados y render",
            paraQue: "Saber qué hace el motor hoy con el emisor de partículas en el Inspector.",
            porQueImporta: "Los campos existen en objetos.json y en Play; el dibujo en el visor puede ser mínimo o pendiente.",
            paragraphs: new[]
            {
                "ParticleEmitterComponent guarda textura, tasas, vida y parámetros de trail según el inspector; forman parte de la instancia como el resto de componentes.",
                "En Play el runtime crea el componente Core con esos datos para lógica futura o efectos; no asumas un sistema de partículas completo visible igual que en motores AAA.",
                "El tab Juego embebido prioriza sprites, luces y física; si no ves partículas, revisa que no confundas con el pipeline Vulkan en ventana separada.",
                "Para efectos visibles ya, muchos proyectos combinan sprites animados, seeds y scripts antes de depender solo del emisor."
            },
            bullets: new[]
            {
                "Si el render de partículas evoluciona, actualiza este tema y AI-ONBOARDING §8.2."
            }),

        new(
            id: "fisica-raycast-dos-mundos",
            title: "Física y raycast: mapa vs colliders en escena",
            paraQue: "Elegir la API correcta y no «atravesar» muros por error.",
            porQueImporta: "world.raycast y physics.raycast no consultan exactamente el mismo conjunto de geometría.",
            paragraphs: new[]
            {
                "world.raycast (y variantes como raycastTiles / raycastCombined) usa la geometría que el host expone para el mapa y la escena en Play: alineado con sólidos del tilemap y el flujo documentado del runtime.",
                "physics.raycast y physics.overlapCircle operan sobre PlayScenePhysicsApi: colliders de objetos en la escena de física, sin sustituir por sí solos la consulta de tiles.",
                "Para un personaje que choca con paredes del mapa y con cajas dinámicas, sueles combinar consultas o usar el mismo subsistema que ya usa el paso de física para movimiento.",
                "overlapCircle comprueba un círculo contra AABB de colliders; recuerda que Collider «Circle» en datos sigue resolviéndose como AABB con mitades iguales en el paso actual."
            },
            bullets: new[]
            {
                "Detalle técnico: docs/AI-ONBOARDING.md (núcleo técnico, física y §8.2)."
            }),

        new(
            id: "limites-consejos",
            title: "Límites, undo y buenas prácticas",
            paraQue: "Evitar sorpresas: rellenos enormes, historial corto o parsers heurísticos.",
            porQueImporta: "El editor prioriza seguridad y tiempo de respuesta sobre operaciones destructivas.",
            paragraphs: new[]
            {
                "Historial de deshacer limitado (orden de decenas de pasos) — guarda a menudo.",
                "Propiedades de script: preferir anotaciones -- @prop en el .lua; si no, el parser heurístico detecta asignaciones globales en la raíz (sin local).",
                "Biblioteca global y assets: mantén rutas relativas coherentes al mover carpetas.",
                "Si Play no refleja cambios: guarda escena, revisa script asignado y consola de errores Lua.",
                "Partículas/trail: los datos se guardan; el dibujo en el visor puede no coincidir con tus expectativas hasta que el render esté completo (ver tema dedicado).",
                "BootstrapScriptId: no confíes en ejecución automática al pulsar Play sin comprobar el tema «BootstrapScriptId (aviso)»."
            }),

        new(
            id: "discord-rich-presence",
            title: "Discord (Rich Presence)",
            paraQue: "Mostrar en Discord en qué parte del motor trabajas (Hub, editor, ayuda, modales).",
            porQueImporta: "El cliente solo actualiza el estado si Discord está abierto; no sustituye documentación ni soporte.",
            paragraphs: new[]
            {
                "Con la aplicación Discord en ejecución, FUEngine puede enviar actividad enriquecida: pestaña o ventana visible (Hub, Mapa, Scripts, Juego, Consola, documentación, Spotlight, diálogos de configuración o exportación, etc.).",
                "El perfil puede incluir un botón «Ver en GitHub» al repositorio público del motor (según configuración de la aplicación Discord y políticas de URL). Si no ves el botón, revisa el Developer Portal de Discord y la consola del editor (categoría Discord) por errores del RPC.",
                "La pestaña Juego con Play en marcha puede mostrar estado de «probando» o similar; al cambiar de pestaña, el texto suele reflejar la vista activa (p. ej. Consola cuando la ves).",
                "Si no usas Discord, desactivar o ignorar esta función no afecta al guardado de proyectos ni al runtime Lua."
            },
            bullets: new[]
            {
                "No envía contenido de tus scripts ni rutas de proyecto a Discord; solo metadatos de estado del editor.",
                "Problemas de conexión: mensajes en consola; el motor sigue funcionando sin Rich Presence."
            }),

        new(
            id: "lua-patrones-snippets",
            title: "Lua: patrones y fragmentos útiles",
            paraQue: "Copiar ideas probadas: movimiento, timers, depuración en pantalla.",
            porQueImporta: "Evita reinventar cada vez la misma estructura en onUpdate.",
            paragraphs: new[]
            {
                "Movimiento por segundo: local speed = 5; self:move(vx * time.delta * speed, vy * time.delta * speed) — vx/vy dirección normalizada o -1..1 según input.",
                "Cooldown sin coroutines: local last = 0; en onUpdate: if time.seconds - last > 2 then last = time.seconds; audio.play(\"sfx\") end.",
                "Snippet dbggrid (mini-IDE): inserta un onUpdate con Debug.drawGrid para ver la rejilla de casillas en la vista de juego; útil para alinear colisiones con tiles.",
                "Listar objetos: world.getObjects() / getAllObjects() devuelven proxies según el runtime; para un subconjunto suele ser más claro findByTag(\"enemigo\") o findNearestByTag(x, y, \"player\").",
                "Capas: en main.lua de capa, guarda referencias en onStart (world:instantiate, etc.) y en onLayerUpdate actualiza posiciones; no uses self.",
                "Depuración: Debug.drawLine(x1,y1,x2,y2,r,g,b,a) y drawCircle en coordenadas de mundo; colores 0–255. Limpia dibujos que no necesites cada frame para no saturar."
            },
            bullets: new[]
            {
                "Más sintaxis Lua: pestaña «Lua — sintaxis y librería» (palabras reservadas y guías pairs/pcall/…).",
                "APIs completas: tema «Scripting Lua — referencia completa de APIs»."
            }),

        new(
            id: "configuracion-motor-editor",
            title: "Configuración del motor (preferencias globales)",
            paraQue: "Ajustar Hub, explorador, autoguardado, atajos y comportamiento al arrancar sin tocar un proyecto concreto.",
            porQueImporta: "EngineSettings vive aparte del proyecto.FUE; no se empaqueta en el juego exportado.",
            paragraphs: new[]
            {
                "Menú Configuración o equivalente abre preferencias: idioma, tema, escala de UI, StartupBehavior (abrir Hub, último proyecto o asistente nuevo), paleta por defecto para proyectos nuevos.",
                "Pestaña Explorador: orden de carpetas estándar al crear proyecto, carpetas extra por línea, temas predefinidos (Jam, UI+Prefabs…), opciones de logs automáticos del motor.",
                "Atajos: presets (estilo Unity, Photoshop) y lista editable; conflicto con otra app = cambia el binding o el preset.",
                "Autoguardado del editor y carpeta de backups pueden configurarse aquí y en proyecto; revisa espacio en disco en proyectos grandes.",
                "La documentación in-app que lees se actualiza con el ejecutable; AI-ONBOARDING.md en el repo puede ser más reciente en desarrollo — enlaces desde Hub y Spotlight."
            },
            bullets: new[]
            {
                "Proyecto abierto: Proyecto → Editor avanzado del proyecto es distinto (resolución, física, FPS, escenas).",
                "README.md del repo exige tools\\publicar.bat para generar FUEngine.exe desde fuente."
            }),

        new(
            id: "scripts-conectar-motor",
            title: "Conectar scripts con el motor (flujo de trabajo)",
            paraQue: "Pasos claros: registrar el .lua, asignarlo al objeto, exponer parámetros y usar APIs en hooks.",
            porQueImporta: "Sin scripts.json y sin entrada en la lista del objeto, el archivo no se ejecuta en Play.",
            paragraphs: new[]
            {
                "1) Registro: el archivo .lua debe estar en Scripts/ y listado en scripts.json (id + path). La pestaña Scripts permite editar el registro.",
                "2) Asignación: selecciona el objeto en la jerarquía → Inspector → expander «Scripts (Lua)» → combo «Agregar» elige el id del script → Agregar. Puedes tener varios scripts por instancia; el orden importa si comparten estado.",
                "3) Código: en el .lua define hooks (onStart, onUpdate(dt), …). El motor inyecta self (proxy del objeto), world, input, time, etc. No uses local function onStart — deben ser funciones globales del chunk que NLua busca por nombre.",
                "4) Datos por instancia: las propiedades editables aparecen en «Variables de script (@prop / globales)» debajo de la lista de scripts. Se generan desde comentarios -- @prop en el código o desde heurística de asignaciones globales en la raíz del archivo.",
                "5) Play: guarda el mapa/objetos (Ctrl+S / Guardar todo). Los errores de Lua salen en la consola con ruta al .lua.",
                "Arrastrar un .lua desde el explorador al panel de scripts solo funciona si ese script ya está en scripts.json (el editor valida antes de añadir)."
            },
            bullets: new[]
            {
                "Capas del mapa: asigna el .lua en el inspector de la capa (Ground, etc.), no en la lista de objetos — ver «Scripts de capa del mapa».",
                "getComponent:invoke solo enruta métodos hacia otros ScriptComponent; los componentes C# (Collider, etc.) aún no exponen setters genéricos por invoke — ajusta hitbox y partículas en el Inspector o en datos.",
                "Tema relacionado: «Objeto + script + Inspector: hitbox, partículas y @prop»."
            }),

        new(
            id: "objeto-script-inspector-gameplay",
            title: "Objeto + script + Inspector: hitbox, partículas y @prop",
            paraQue: "Combinar datos del motor (collider, ParticleEmitter) con parámetros tunables del script en la misma instancia.",
            porQueImporta: "La jerarquía selecciona el objeto; el Inspector mezcla componentes de gameplay y variables del script.",
            paragraphs: new[]
            {
                "Jerarquía: clic en el objeto bajo «Scene: …» → el Inspector muestra identidad, transform, luz opcional, expander «Sprite, física y gameplay (Play)» y más abajo Interacción, Scripts (Lua) y Variables de script (@prop / globales).",
                "Hitbox (AABB en Play): en «Sprite, física y gameplay», sección Collider (Box / Circle): forma, ancho y alto en casillas para caja, radio para círculo, offsets X/Y. Esos valores se guardan en la instancia (objetos.json) y el runtime crea ColliderComponent al iniciar Play. Cambiarlos en el editor es la forma soportada; en Lua, ComponentProxy.invoke no modifica ColliderComponent (solo ScriptComponent). Para lógica dinámica usa triggers, raycast o rediseña el tamaño en datos antes de Play.",
                "Partículas: marca «Habilitar ParticleEmitter» y la ruta de textura en el mismo expander. El motor guarda también tasas y vida en la instancia (ParticleEmissionRate, ParticleLifeTime, ParticleGravityScale en datos); si tu build del editor solo muestra textura en la UI, el resto puede seguir existiendo en JSON o evolucionar en el inspector — en Play, PlayModeRunner crea ParticleEmitterComponent con textura, emisión, vida y gravedad.",
                "Script que «usa» partículas: opción A — confías en los datos del motor: activas el componente y ajustas cifras en inspector/JSON; tu Lua solo reacciona (p. ej. self.active = false al terminar). Opción B — parámetros de diseño en el script: define -- @prop emision: float = 12, tinteParticulas: string = \"#FFAA00\" y en onUpdate lees esas variables (inyectadas como globales) para decidir self:setSpriteTint, audio.play o ramas de lógica; el diseñador cambia emision y tinte en «Variables de script» sin tocar el código.",
                "Color del sprite (tinte visible): SpriteColorTintHex / setSpriteTint en Lua; no confundir con un «color de partícula» global si el visor de partículas aún es limitado — combina tinte de sprite + textura de partícula.",
                "«Añadir propiedad» bajo Variables de script añade entradas al ScriptPropertyEntry de la instancia aunque no estén en el .lua todavía; alinear nombre y tipo con @prop evita inconsistencias.",
                "Orden práctico: primero tipo de objeto y collider; luego scripts; luego @prop; por último prueba Play y revisa consola."
            },
            bullets: new[]
            {
                "Ejemplo mental: «Chispas» — ParticleEmitter habilitado + textura; script con @prop intensidad: float = 1 y en onUpdate if intensidad > 0 then … usando audio o visibilidad.",
                "Multiselección: varios objetos seleccionados no muestran bien las variables de script; edita uno a la vez.",
                "Referencias: «Variables de script: @prop», «Componentes: inspector, objetos.json y Play», «Partículas y trail: datos guardados»."
            })
        };
        for (var i = 0; i < topics.Count; i++)
            topics[i] = ApplyManualPresentation(topics[i]);
        topics.AddRange(LuaReferenceDocumentation.BuildTopics());
        return topics;
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
        IReadOnlyList<string>? bullets = null,
        string? subtitle = null,
        string? enMotor = null)
    {
        Id = id;
        Title = title;
        ParaQue = paraQue;
        PorQueImporta = porQueImporta;
        Paragraphs = paragraphs;
        Bullets = bullets;
        Subtitle = subtitle;
        EnMotor = enMotor;
    }

    public string Id { get; }
    public string Title { get; }
    public string? ParaQue { get; }
    public string? PorQueImporta { get; }
    public IReadOnlyList<string> Paragraphs { get; }
    public IReadOnlyList<string>? Bullets { get; }
    /// <summary>Línea bajo el título (contexto o tipo de tema).</summary>
    public string? Subtitle { get; }
    /// <summary>Cómo encaja en FUEngine frente a un Lua «genérico» u otro motor.</summary>
    public string? EnMotor { get; }
}
