using System.Runtime.CompilerServices;

namespace FUEngine.Help;

/// <summary>
/// Contenido de ayuda in-app. Convención por tema (<see cref="DocumentationTopic"/> + <see cref="DocumentationView"/>):
/// <list type="bullet">
/// <item><description><b>Para qué</b> (<see cref="DocumentationTopic.ParaQue"/>) — objetivo del usuario.</description></item>
/// <item><description><b>Por qué importa</b> (<see cref="DocumentationTopic.PorQueImporta"/>) — riesgo o contexto si se ignora.</description></item>
/// <item><description><b>En FUEngine</b> (<see cref="DocumentationTopic.EnMotor"/>) — dónde está en el editor o en el runtime y cómo encaja (menús, jerarquía, APIs).</description></item>
/// <item><description><b>Contenido</b> (<see cref="DocumentationTopic.Paragraphs"/>) — detalle: suele incluir explícitamente <b>Dónde</b>, <b>Cómo</b> y remisión a <b>Ejemplos</b> cuando aplique.</description></item>
/// <item><description><b>Puntos clave</b> (<see cref="DocumentationTopic.Bullets"/>) — recordatorios breves.</description></item>
/// <item><description><b>Código</b> (<see cref="DocumentationTopic.LuaExampleCode"/>) — solo en temas con snippet (p. ej. Ejemplos de scripts).</description></item>
/// </list>
/// </summary>
public static partial class EngineDocumentation
{
    public const string QuickStartTopicId = "quick-start";

    /// <summary>Tema inicial al elegir «Documentación completa»: flujo de juego (no repetir el mismo apartado que la guía rápida).</summary>
    public const string FullManualStartTopicId = "crear-juego";

    /// <summary>Índice de la pestaña «Lua — sintaxis y librería» (palabras reservadas + guías usadas con el motor).</summary>
    public const string LuaReferenceIntroTopicId = "lua-reference-intro";

    /// <summary>Índice de la pestaña «Ejemplos de scripts» (introducción).</summary>
    public const string ScriptExamplesIntroTopicId = "script-examples-intro";

    public static bool IsLuaReferenceSidebarTopic(string? id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return id == LuaReferenceIntroTopicId
               || id.StartsWith("lua-kw-", StringComparison.Ordinal)
               || id.StartsWith("lua-guide-", StringComparison.Ordinal);
    }

    /// <summary>Temas de la pestaña «Ejemplos de scripts» (intro + <c>script-ex-*</c>).</summary>
    public static bool IsScriptExamplesSidebarTopic(string? id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return id == ScriptExamplesIntroTopicId
               || id.StartsWith("script-ex-", StringComparison.Ordinal);
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
                "Abre o crea un proyecto desde la pantalla de inicio (Hub). Cada proyecto es una carpeta con configuración y escenas. El Hub incluye biblioteca global, scripts Lua globales, FUEngine Spotlight (Ctrl+P) y opcionalmente Rich Presence con Discord: **solo estético** (estado en el perfil); si el RPC falla, revisa la consola (categoría Discord) — **no afecta al rendimiento del juego ni al guardado del proyecto**.",
                "Abre una escena. La escena enlaza mapa, objetos, triggers, scripts y UI guardada en esa escena.",
                "Pestaña **Mapa**: lienzo del tilemap; **Juego**: Play embebido — **misma lógica de simulación y Lua** que al exportar (build / ventana Vulkan), pero el **dibujo** del preview es **solo WPF**: un **Canvas** (`GameViewportCanvas`) donde `GameViewportRenderer` compone **sprites/tiles y depuración con elementos WPF** (no hay **surface Vulkan incrustado**, ni ventana GLFW superpuesta, ni el patrón típico de compartir framebuffer con `HwndHost`). La **build** y el juego en **ventana aparte** usan **FUEngine.Graphics.Vulkan** (GLFW + Silk). **Por eso FPS, carga y fidelidad visual pueden diferir** del ejecutable final. **Hot reload:** con Play en **Juego**, al **guardar** un `.lua` el runner suele **recargar** sin reiniciar todo Play; si el estado se desincroniza, **detén e inicia** de nuevo.",
                "Pinta tiles (capa activa a la derecha) y coloca objetos. Guarda (Ctrl+S / Guardar todo). Prueba con Proyecto → Iniciar juego o la pestaña Juego.",
                "En **Play** el runtime solo considera scripts que constan en **`scripts.json`** (id + ruta). **No hace falta editar ese JSON a mano:** al crear o importar un `.lua` por el **Explorador** (**Nuevo → Script Lua**, duplicar/importar carpetas, etc.) o desde **ejemplos embebidos**, el editor **sincroniza `scripts.json` solo**. Luego **asigna** el script al objeto en el **Inspector** (o capa). Pestaña **Scripts**: autocompletado (Ctrl+Espacio), línea en rojo si el chunk no compila. **`require`:** el motor **inyecta su propio `require`**; **no** amplía `package.path` como un Lua «de escritorio» ni convierte el **punto** en rutas de carpetas. Solo el separador **/** define subcarpetas bajo **Scripts/**: `require(\"Modulo\")` → `Scripts/Modulo.lua`; `require(\"Capa/Modulo\")` → `Scripts/Capa/Modulo.lua`. Si escribes `require(\"Capa.Modulo\")` busca **`Scripts/Capa.Modulo.lua`** (un solo nombre de archivo con punto en la raíz de Scripts), **no** `Scripts/Capa/Modulo.lua`. Prohibidos `..` y rutas absolutas; por segmento de ruta solo letras, dígitos, `_`, `-`, `.`.",
                "¿Ejemplos? Ayuda → pestaña **Ejemplos de scripts** (dificultad con **punto de color** en la lista: verde / ámbar / rojo). **Nuevo → Script Lua** en el explorador: plantilla con **@prop** y hooks **onStart, onUpdate, onDestroy** ya presentes (no hoja en blanco). **Ayuda en tres pestañas:** Manual del motor | Lua — sintaxis y librería | Ejemplos. **Desde un error:** en **Consola** o en el log de **Juego**, **doble clic** en la entrada cuando el mensaje incluye `algo.lua` y número de línea → abre la pestaña **Scripts** en esa **línea** (no abre solo el manual). Para ir al tema o API relacionados, usa **Spotlight (Ctrl+P)** o el menú **Ayuda** y navega al apartado que necesites."
            },
            bullets: new[]
            {
                "Ayuda: Manual del motor | Lua — sintaxis y librería | Ejemplos de scripts. Menú Ayuda: manual rápido, API Lua, logs.",
                "Explorador = disco. Jerarquía = escena (mapa, objetos, triggers, UI).",
                "Si falta algo en Play: capa visible, pestaña correcta, script **asignado** en Inspector, escena guardada; si metiste un `.lua` solo copiando al disco **fuera** del flujo del editor, revisa que aparezca en la lista de la pestaña **Scripts**.",
                "Temas útiles: Depuración, Hot reload, Escenas, Triggers, Exportación, FAQ, tipos de capa.",
                "Build del propio FUEngine desde fuente: **README.md** en la raíz del repo (instalador, dotnet publish)."
            }),

        new(
            id: "crear-juego",
            title: "Cómo funciona el motor y cómo hacer un juego",
            paraQue: "Tener una mentalidad clara: qué partes del FUEngine son el «nivel» y qué partes son la «lógica» del juego.",
            porQueImporta: "Separar editor (datos) y runtime (Play) acelera el aprendizaje; este tema va de lo básico a lo técnico en orden.",
            paragraphs: new[]
            {
                "**1. Qué es un juego aquí** — Un juego = **datos** (mapa por capas, objetos, escenas, UI por escena, audio…) guardados en JSON + **scripts Lua** que el **runtime** ejecuta. No hay otra «capa mágica»: si no está en datos o en Lua en Play, no ocurre.",
                "**2. Editor vs runtime** — **El editor no ejecuta la lógica del juego.** Sirve para crear y editar el proyecto (mapa, Inspector, explorador, pestaña Scripts). **Toda la lógica Lua corre en el runtime:** pestaña **Juego**, ventana **Iniciar juego** o **build** exportada. Ahí existen `world`, `self`, `input`, etc.",
                "**3. Flujo típico** — **1)** Crear o abrir proyecto desde el Hub. **2)** Abrir una escena: tienes mapa, objetos, triggers y **UI** (árbol UICanvas) **de esa escena**. **3)** Pintar tiles y ajustar colisión (tema «Tipos de capa del mapa»). **4)** Crear scripts: **Explorador → Nuevo → Script Lua** (el editor mantiene **`scripts.json`**; luego **asigna** el script en el **Inspector** al objeto o a la capa). **5)** **Probar** en pestaña **Juego** o **Proyecto → Iniciar juego**. **6)** **Exportar** cuando toque.",
                "**4. Escenas y UI** — La UI editable está **ligada a cada escena**. Al cambiar de escena con **`game.loadScene`** se carga el **UIRoot** de la escena destino. Para un HUD o inventario que «surja» en varias escenas suele bastar con **estado en Lua** y volver a montar o duplicar el mismo canvas donde haga falta; es **patrón de diseño tuyo**, no algo que el motor impone con un menú único.",
                "**5. Seeds vs instancias** — Una **seed** (`.seed`) es plantilla reutilizable. La **instancia** en el mapa es la entidad con transform y componentes que Play trata como objeto. Colocas instancias desde jerarquía o flujo de semillas.",
                "**6. Scripts y `scripts.json`** — En **Play** solo corren `.lua` que figuran en **`scripts.json`**. **En la práctica no editas ese archivo a mano:** crear o importar scripts desde el **Explorador** (y ejemplos desde Ayuda) **sincroniza el registro**. Falta habitual: script creado **solo copiando el archivo al disco** fuera del editor — entonces revisa la pestaña **Scripts** o vuelve a crearlo por el menú. **Arrastrar** un `.lua` al Inspector **solo** enlaza si ya existe esa ruta en el registro.",
                "**7. Renderizado: preview vs juego real** — El **preview** de la pestaña **Juego** dibuja con **WPF** (`GameViewportCanvas` / aproximación 2D). El **juego en ventana** (export o Play en ventana) usa **Vulkan** (**GLFW**). Mismo proyecto, **diferente** pipeline; FPS y aspecto pueden variar. Detalle: **«Vulkan y ventana de ejecución»**.",
                "**8. Física y mapa** — **Colisión en mapa:** la celda sólida depende del **tipo de capa** (p. ej. Solid) y **TileData** / **`IsCollisionAt`**, **no** del nombre decorativo de la capa. **Objetos:** colliders y paso de física propio del motor (cajas en escena). **`world.raycast`** vs **`physics`:** geometrías distintas — tema **«Física y raycast: mapa vs colliders»**.",
                "**9. Depuración** — **Consola** y log de la pestaña **Juego**; **doble clic** en línea con archivo abre **Scripts**. **Hot reload** al guardar `.lua` con Play: **«Hot reload de scripts .lua»**.",
                "**10. Detalle técnico (por debajo del capó)** — Editor en **C# / .NET 8** (WPF). Lua vía **NLua**. Audio en editor/proyecto puede usar **NAudio**. Física de objetos: integrada (AABB), no Box2D. Ventana gráfica: **Silk.NET** / Vulkan en el ensamblado gráfico. Si solo quieres **usar** el motor, basta con las secciones 1–9.",
                "**Cosas que no debes esperar de este motor** — No hay **UI global automática** entre escenas (lo diseñas con datos + Lua). No existe un nodo **`GlobalObjects`** ni equivalente con ese nombre. El **preview** del tab **Juego** **no** es Vulkan embebido en el panel WPF. **No** se ejecuta Lua «en el editor» sin iniciar Play.",
                "Trabajo fino: Inspector para componentes (sprite, collider, rigidbody, partículas…). **Guarda** a menudo. Si algo en Play no cuadra: script **asignado**, escena guardada; si el fallo es **paredes**, revisa tipo de capa y datos de celda."
            },
            bullets: new[]
            {
                "**Regla de oro:** editor = datos · runtime = lógica Lua (Play o build).",
                "**scripts.json:** lo rellena el editor al crear/importar scripts; tú enlazas en el Inspector.",
                "**Colisión mapa:** tipo de capa + TileData — ver «Tipos de capa del mapa».",
                "**Raycast vs colliders:** «Física y raycast: mapa vs colliders». **Render:** «Vulkan y ventana de ejecución».",
                "Más temas: Eventos Lua, Depuración, Seeds, Scripts de capa, Problemas frecuentes."
            },
            luaExampleCode:
                @"-- Ejemplo mínimo en un script asignado a un objeto (onUpdate).
-- Sustituye ""Suelo"" por el nombre de tu capa y el ""1"" por un id de catálogo válido en tu tileset.

function onUpdate(dt)
    if input.isKeyDown(Key.Space) then
        world.setTile(10, 5, ""Suelo"", 1)
    end
end"),

        new(
            id: "arquitectura",
            title: "Arquitectura del motor (ensamblados)",
            paraQue: "Entender qué código hace qué cuando algo falla o quieres ampliar el motor.",
            porQueImporta: "FUEngine separa datos, editor, ventana de juego y gráficos para poder iterar sin mezclar todo.",
            paragraphs: new[]
            {
                "**Jerarquía de ensamblados (qué referenciar):** **FUEngine.Core** — dominio puro (.NET 8; en el `.csproj` actual **sin NuGets**, solo BCL). **FUEngine.Service** — contratos compartidos, **referencia Core**. **FUEngine.Graphics.Vulkan** — **Core + Silk.NET** (Vulkan, GLFW, extensiones KHR): es el **único** ensamblado que enlaza Silk; el resto del motor **no** mezcla llamadas de bajo nivel a GPU/ventana en otros proyectos. Un backend futuro (p. ej. Direct3D 12, Metal) podría ser **otro** ensamblado gráfico alternativo **sin reescribir Core** ni el modelo de datos. **FUEngine.Runtime** — **Core + Graphics.Vulkan + NLua** (bucle Lua, `LuaScriptRuntime`, APIs inyectadas). **FUEngine.Editor** — **Core** (serialización JSON con BCL/código propio, comandos de edición). **FUEngine** (exe WPF) — **Core, Editor, Runtime, Service**; **Vulkan entra de forma transitiva vía Runtime** cuando se abre la ventana de juego; el **Play embebido** en el IDE dibuja con WPF, no referencia Vulkan directamente en el `.csproj`.",
                "FUEngine.Core: mapa por chunks y capas, GameObject, componentes, triggers, UIRoot, física de escena (AABB), etc. Editor y Runtime comparten estos modelos al serializar o simular.",
                "FUEngine.Editor: coherencia con Core al guardar mapas, objetos, proyecto, scripts, audio. **Deshacer/rehacer** en la capa de **comandos / historial**, no en controles sueltos.",
                "**Ciclo de vida datos ↔ Play:** el IDE edita **archivos JSON** y modelos en memoria del editor; con **Play** activo, el **PlayModeRunner** mantiene **GameObject** y Lua **en memoria**. Cambiar mapa u objetos en el editor **no** propaga todo al vuelo a esa copia en ejecución: normalmente **guardas** y **reinicias** Play para alinear con disco. **Excepción muy usada — hot reload de `.lua`:** el vigilante (`ScriptHotReloadWatcher`) notifica al runner → `PlayModeRunner.OnScriptSaved` → `LuaScriptRuntime.ReloadScript` (invalida caché, quita instancias NLua de ese `.lua` y las **recrea** sobre los mismos `GameObject` de Core). Eso **no** sustituye guardar escena en JSON.",
                "FUEngine (app WPF): ventanas, pestañas, **Canvas** del tab Juego conectado al mismo runner; ventana exportada usa **Graphics.Vulkan**.",
                "**No asumas APIs de Unity/Godot:** solo las tablas y firmas que documenta este motor. Para gráficos, **Silk.NET** está acotado al ensamblado **Graphics.Vulkan**."
            },
            bullets: new[]
            {
                "Play embebido = WPF (`GameViewportRenderer`); ventana de juego = Vulkan — ver «Vulkan y ventana de ejecución».",
                "Referencias: Core base; Runtime = Core + Vulkan + NLua; Editor = Core; app WPF suma Editor + Runtime + Service.",
                "Hot reload toca Runtime (Lua) desde el proceso del editor; mapa/objetos en Play suelen requerir reinicio tras guardar.",
                "Cambiar tipos en Core afecta a Editor y Runtime."
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
                "**Dónde:** la fila de pestañas está **debajo del menú** y **sobre** el área central. **Ver →** muestra u oculta **Juego** y **Consola**. **«+»** añade pestañas opcionales agrupadas por **Proyecto**, **Contenido**, **Multimedia** y **Debug**.",
                "**Mapa:** lienzo del tilemap, objetos colocados, triggers, herramientas de pintura y selección. Ctrl+rueda = zoom; WASD mueve la **cámara del editor**, no el personaje en juego. Aquí ajustas capas, colisión por tipo de capa y datos de celda.",
                "**Juego (Play embebido):** inicia o reanuda el **mismo runtime Lua y simulación** que la build; el **dibujo** es un viewport **WPF** (lienzo), no la ventana Vulkan. Incluye jerarquía/runtime del tab y **lista de log** propia: errores Lua con ruta; **doble clic** en una línea con archivo abre el .lua en **Scripts**.",
                "**Consola:** log global del editor (info, advertencias, errores, categoría Lua, Discord, etc.). Útil cuando Play no está en primer plano. También toasts para avisos breves.",
                "**Scripts:** vista del registro **`scripts.json`**, editor **AvalonEdit** con resaltado LuaFUE, **Ctrl+Espacio**, línea roja si el chunk no compila. Crear scripts con **Explorador → Nuevo → Script Lua** (actualiza el registro solo); desde aquí editas y revisas la lista.",
                "**Explorador:** árbol **real del disco** del proyecto (carpetas, .map, .lua, assets). Doble clic en `.lua` abre **Scripts**; en manifiesto de proyecto abre configuración según flujo del editor.",
                "**Tiles:** catálogo y atlas de tiles para pintar en **Mapa**; enlazado al sistema de capas y a **world.setTile** en runtime donde aplique.",
                "**Animaciones:** clips y datos que consumen objetos (animaciones.json, clips en Inspector).",
                "**Seeds:** biblioteca de archivos **.seed** (plantillas); colocar en mapa o jerarquía según flujo de «Semillas».",
                "**Audio:** edición de **audio.json**, ids de clips, preview; coherente con **audio.play** / **audio.playMusic** en Lua.",
                "**Debug:** panel de depuración e inspección ligada al Play del editor (según versión: trazas, contadores, enlace al runner del tab Juego).",
                "**Tile Creator / Tile Editor:** crear o editar definiciones de tiles y metadatos para el catálogo; barra de herramientas con iconos (símbolos) y **Ctrl+Z** en el lienzo para deshacer el último trazo o relleno en la capa activa.",
                "**Paint Creator / Paint Editor:** flujos de arte tipo «paint» integrados en Creative Suite; mismo deshacer con **Ctrl+Z** en el lienzo y pincel opaco en modo paint.",
                "**Editor de colisiones:** trabajo sobre máscaras / colisión de tiles o assets según el flujo del editor (menú «+», mismo nombre que en el editor).",
                "**Tile por script (ScriptableTile):** tiles con lógica asociada en el ecosistema Creative Suite.",
                "Flujos rápidos: tiles → **Mapa**; probar código → **Juego**; ver fallos → **Consola** o log del **Juego**; editar Lua → **Scripts**."
            },
            bullets: new[]
            {
                "«+» y orden interno de kinds: ver tema «Índice de pestañas del editor».",
                "Layout de pestañas puede persistir con el proyecto.",
                "GUI en Lua: **«GUI (Canvas) y Lua en Play»** y ejemplos `script-ex-ui-*`, `script-ex-gameplay-movimiento-wasd-flechas`."
            }),

        new(
            id: "pestanas-editor-catalogo",
            title: "Índice de pestañas del editor",
            paraQue: "Ver de un vistazo qué vistas existen y cómo se agrupan en el menú «+».",
            porQueImporta: "No todas las pestañas están visibles al inicio; algunas se añaden desde el botón «+».",
            paragraphs: new[]
            {
                "**Fila habitual:** Mapa | Consola | Juego (Play embebido) — se pueden ocultar desde **Ver →**.",
                "**Menú «+» (orden aproximado de tipos):** Scripts, Explorador, Tiles, Animaciones, Seeds, Tile Creator, Tile Editor, Paint Creator, Paint Editor, Editor de colisiones, Tile por script, Audio, Debug — más **Interfaz** (categoría propia): un ítem por cada **Canvas** de la escena (**Interfaz · nombre**), abre el editor visual de UI; **Crear Canvas en la escena** equivale al menú contextual de la jerarquía.",
                "**Categorías del «+»:** Proyecto (p. ej. Scripts, Explorador), Contenido (Tiles, Animaciones, Seeds, herramientas Creative Suite), **Interfaz** (Canvas de la escena, editor de botones/texto/paneles), Multimedia (Audio), Debug (Consola, Juego, Debug).",
                "**Anatomía mínima del proyecto (carpeta raíz):** **scripts.json** — registro que lee el runtime; el editor lo actualiza al crear/importar scripts por **Explorador** u otros flujos integrados. **Scripts/** — **.lua** (subcarpetas; **require(\"Carpeta/Modulo\")**). **Seeds/** o **.seed**. Escenas y mapas (**Maps**, **Project.FUE**). Detalle en «Archivos JSON del proyecto».",
                "Nombres mostrados al usuario (ejemplos): Juego como **«Juego (Play embebido)»**, Tile Creator como **«Tile Creator»**, etc. — alineado con `EditorWindow` (TabDisplayNames, OptionalTabKindsOrder)."
            },
            bullets: new[]
            {
                "Detalle de cada pestaña: tema «Pestañas del editor (+ botón +)».",
                "Código: `EditorWindow.xaml.cs` (TabDisplayNames, OptionalTabKindsOrder, TabCategory)."
            }),

        new(
            id: "componentes-catalogo",
            title: "Componentes en instancia (catálogo)",
            paraQue: "Saber qué piezas puede llevar un objeto en Play y cómo se nombran frente a Lua y JSON.",
            porQueImporta: "getComponent y objetos.json usan los mismos nombres que las clases del Core en la medida documentada.",
            paragraphs: new[]
            {
                "Sprite / animación: tinte, flip, orden de dibujo, clip por defecto, velocidad (SpriteComponent + campos en ObjectInstance).",
                "Luz puntual: LightComponent cuando PointLightEnabled; radio, intensidad, color hex.",
                "Collider: Box o Circle en datos; resolución física AABB en el paso actual.",
                "Rigidbody: masa, gravedad, drag, congelar rotación.",
                "CameraTarget: la cámara puede seguir este objeto en lugar del protagonista.",
                "ProximitySensor: rango y etiqueta objetivo; eventos tipo trigger por distancia.",
                "HealthComponent: máximo, actual, invulnerabilidad.",
                "AudioSourceComponent: clip por id, volumen, pitch, loop, spatial blend.",
                "ParticleEmitterComponent: textura, tasas, vida, gravedad del emisor.",
                "Scripts: lista de ids en instancia y scripts.json.",
                "Transform implícito: posición, rotación, escala, LayerOrder; orden de capa en sprites."
            },
            bullets: new[]
            {
                "Detalle JSON: «Componentes: inspector, objetos.json y Play», «Objetos: componentes y datos JSON».",
                "Lua: «ComponentProxy e invoke»."
            }),

        new(
            id: "manual-varios-temas",
            title: "Más temas (referencia breve)",
            paraQue: "Saltos rápidos sin reexplicar cada sistema.",
            porQueImporta: "Algunos apartados solo enlazan o listan; el detalle sigue en los temas enlazados.",
            paragraphs: new[]
            {
                "Exportación, integridad de proyecto, biblioteca global, streaming de chunks, ventana Vulkan, NAudio, seeds en runtime, medidores Fear/Danger, límites conocidos, patrones Lua, Discord RPC, configuración global del motor, deshacer/rehacer, asistente de proyecto.",
                "Usa el filtro de la lista de ayuda o Spotlight (Ctrl+P) con palabras clave del tema que busques."
            }),

        new(
            id: "lua-gui-canvas-play",
            title: "GUI (Canvas) y Lua en Play",
            paraQue: "Entender cómo se enlazan botones, paneles y texto en pantalla con **ui.*** durante la simulación.",
            porQueImporta: "Los Ids del Canvas y de cada control deben coincidir con **ui.bind** y **ui.show**; el foco decide qué canvas recibe clics.",
            paragraphs: new[]
            {
                "**Dónde (editor):** clic en la escena en la **Jerarquía** → **Crear / UI** o menú contextual → **UICanvas**. Renombra el **Id** del canvas (único). Añade **Button** u otros controles como hijos; anota sus **Ids**. El texto visible del botón y estilos se editan en el **Inspector** del control (no hace falta Lua solo para el aspecto).",
                "**Cómo (Lua en Play):** la tabla global **ui** ofrece **show(id)**, **hide(id)**, **setFocus(idCanvas)** y **bind(canvasId, elementId, \"click\", función)**. Registra el **click** en **onStart** del script; el callback recibe el evento cuando el usuario pulsa. Solo el canvas con **foco** recibe clics de ratón en la práctica — usa **setFocus** al abrir un menú.",
                "**Cómo (probar):** inicia **Play** (pestaña **Juego** o ventana Play). **input.isKeyDown** solo refleja el juego en simulación, no el foco del editor en Mapa.",
                "**Ejemplos (código en la ayuda):** ids `script-ex-escenas-tecla-o-boton-loadscene`, `script-ex-ui-hud-mostrar-ocultar-canvas`, `script-ex-ui-boton-hud-pausar-movimiento` — pestaña **Ejemplos de scripts**. Guía de callbacks: pestaña **Lua** → guía **ui.bind**.",
                "Referencias cruzadas: tema **«Iluminación, audio y UI»** (panorama); manual **«Scripting Lua — referencia completa de APIs»** para firmas de **ui.***."
            },
            bullets: new[]
            {
                "**game.loadScene** y **audio.play** usan nombres definidos en tu proyecto (escenas, ids de audio); revisa mayúsculas.",
                "Errores «nil» en **ui**: comprueba que el Canvas exista en la escena guardada y que Play esté iniciado."
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
                "Zoom del lienzo: Ctrl+rueda del ratón o botones +/− (el punto bajo el cursor permanece estable; con +/− se usa el centro del visor). Rueda sin Ctrl: desplaza el mapa (scroll del área). Pan: clic central arrastrando. WASD desplazan la vista (paso mayor y proporcional al zoom del lienzo).",
                "Mapa finito (Infinite desactivado): margen de un chunk alrededor del rectángulo de juego; borde azul del área jugable y botones «+ chunk» solo en la frontera (un botón por celda de chunk de tamaño ChunkSize×ChunkSize casillas). Cada clic crea un chunk vacío en esa celda y el rectángulo de juego (origen + MapWidth/Height en proyecto.json) se recalcula como la unión de todos los chunks, permitiendo formas no rectangulares. El scroll del editor se compensa para que lo que veías (incluido el marco azul de la cámara) no se desplace al cambiar el origen del lienzo; el centro del visor en casillas mundo no se recalcula al geométrico del mapa salvo que lo muevas tú (Alt+arrastrar, botones de la barra).",
                "Capa activa para pintar: elige la fila en el panel Capas (izquierda); no depende de la barra superior. Al elegir tipo de tile (combo o muestras de color) se activa el pincel para pintar enseguida. Brocha: tamaño y rotación en el menú Vista (▦). Relleno: herramienta Cubeta; con el pincel, Mayús+clic = cubo.",
                "Selección rectangular de tiles: rotación 90°/180°, volteos, rellenar, copiar/pegar/duplicar (también desde menú Editar).",
                "Bucket fill tiene límite de celdas (protección anti-congelado) en el servicio de pintura."
            },
            bullets: new[]
            {
                "Herramientas (barra): Pincel, Rectángulo, Línea, Cubeta, Goma, Cuentagotas, Pegar zona, Seleccionar, Colocar objeto, Zona, Medir, Píxeles.",
                "Visual → área visible: marco azul = rectángulo de la cámara/render (px y casillas mostrados en el propio marco). Alt+arrastrar mueve la cámara. Botones Centro mapa / Mundo 0,0 en la barra; scripts en Play usan el mismo rectángulo de vista que el visor (tamaño del canvas del tab Juego). Fuera del área jugable: celdas «+ chunk» en la frontera del conjunto de chunks. Al expandir con «+ chunk», la vista se mantiene alineada al mundo (sin saltar el marco por el solo hecho de crecer el mapa). Proyecto → Avanzado: color del lienzo y fondo de escena.",
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
                "Imágenes PNG/JPEG/BMP: «Editar colisiones…» abre el editor de colisiones; «Reescalar imagen…» abre un diálogo (vecino más cercano, sin blur) con tamaño manual, proporción, presets ×2 / ×4 / ÷2, ajuste a múltiplos del tile del proyecto y sobrescribir o guardar copia. .lua: «Abrir en Tile por script» si aplica."
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
                "Jerarquía — UI Canvas: Crear hijos Button/Text/Image/Panel; **Abrir en tab UI** o menú **+ → Interfaz → Interfaz · (canvas)**. Elementos UI: crear hijos y Propiedades; al editar texto en el inspector, la jerarquía conserva la selección (no se pierde un carácter por refresco).",
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
                "Scripts: lista y «Añadir componente…» (visibilidad, sprite avanzado, colisión, rigidbody, luz, animación, audio, proximidad, salud, cámara, partículas, foco en scripts). **Arrastrar** un `.lua` al Inspector solo lo enlaza si **ya** hay entrada en `scripts.json` para esa ruta (crear el archivo vía **Explorador** registra solo). Arrastrar un objeto desde la jerarquía sobre un campo tipo referencia rellena el InstanceId.",
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
                "Detalle de serialización: ver tema «Componentes: inspector, objetos.json y Play» y el código fuente del editor (ObjectInstance / PlayModeRunner)."
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
                "Si algo no se ve en Play: guarda la escena, definición con sprite, Enabled para scripts y script **asignado** (y registrado: al crear por Explorador suele estar ya en `scripts.json`)."
            },
            bullets: new[]
            {
                "JSON: propiedades camelCase (spriteColorTintHex, rigidbodyEnabled, proximitySensorEnabled, …).",
                "Física del proyecto: PhysicsGravity y PhysicsEnabled en configuración; el rigidbody usa escala de gravedad por objeto.",
                "Triggers de mapa (triggerZones.json): en Play el motor ejecuta los scripts por ID al entrar/salir o cada frame (tick) según el inspector; los triggers de objeto (Collider IsTrigger o ProximitySensor) usan overlap físico. Comparten nombres de eventos Lua pero son sistemas distintos.",
                "Para el mismo modelo JSON ↔ runtime, cruza con el tema «Objetos: componentes y datos JSON»."
            }),

        new(
            id: "triggers",
            title: "Triggers (zonas)",
            paraQue: "Rectángulos en el mapa que ejecutan scripts al entrar, salir o cada frame.",
            porQueImporta: "Evitas lógica de proximidad manual para portales, diálogos o checkpoints.",
            paragraphs: new[]
            {
                "Cada zona tiene tamaño en celdas, capa asociada, y scripts por evento (enter/exit/tick) referenciados por ID del registro de scripts.",
                "Se guardan en triggerZones.json en la raíz del proyecto (TriggerZonesPath en proyecto); la jerarquía lista las zonas para seleccionarlas y abrir el inspector dedicado. Desde el inspector general (sin selección) el botón de añadir trigger crea una zona y abre su inspector.",
                "En Play, el motor crea un GameObject host por script de zona: self apunta a ese host (centro de la zona en casillas). Entrada/salida disparan onZoneEnter / onZoneExit (tras onStart si existe); el tick mantiene onUpdate mientras el protagonista está dentro.",
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
                "game: loadScene(name), quit(), setRandomSeed, randomInt, randomDouble (el host valida tipos numéricos en los bordes).",
                "log: info(msg), warn(msg), error(msg) — en Play del editor se enrutan a la consola con nivel (info/warn/error).",
                "ads: loadInterstitial/loadRewarded (callbacks opcionales), showInterstitial/showRewarded (callbacks con bool de éxito), showBanner, isRewardedReady, setTestMode, setTagForChildDirectedTreatment. En modo Play del editor se usa un simulador (sin SDK real) que registra en consola.",
                "Debug (si está inyectado): drawLine, drawCircle — colores RGBA 0–255; útil para depurar en la vista de juego.",
                "debug.traceback está disponible en el entorno seguro (no todo el módulo debug) para mensajes de pila en scripts.",
                "ComponentProxy: typeName, invoke(method, ...), invokeWithResult(...). Para ScriptComponent llama métodos expuestos en la instancia Lua.",
                "Operadores y tipos Lua: == compara valores; tablas solo son iguales por identidad. Usa tonumber/tostring al mezclar números y cadenas desde UI o JSON.",
                "Iteración: for i = 1, #lista do con arrays densos; pairs para diccionarios; evita modificar la tabla que iteras salvo que sepas el comportamiento de Lua.",
                "Errores: un error en onUpdate puede detener la ejecución del script en ese frame; la consola muestra archivo y línea con formato uniforme (ruta:línea). pcall protege bloques experimentales.",
                "Extensiones: el trabajo pesado debe vivir en C# (Vulkan, física, APIs). NuGet en proyectos del motor y exposición fina a Lua mediante APIs [LuaVisible] en el runtime."
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
                "El editor usa resaltado de sintaxis Lua y JSON (definición Lua embebida en el motor, alineada al tema oscuro). El color base del texto no depende solo del tema de la ventana: el resaltado aplica tonos distintos a comentarios, cadenas y palabras clave. Tras un breve retraso al editar, el motor intenta compilar el chunk: si hay error de sintaxis (p. ej. falta un end), la línea se marca en rojo en el propio editor, además del mensaje en consola al ejecutar o recargar.",
                "Mientras escribes, aparecen sugerencias para identificadores y miembros tras escribir un punto (world., self., ads., …). Ctrl+Espacio fuerza el menú de completado; Escape lo cierra. Las entradas muestran iconos pequeños (tablas, métodos, ads); el menú usa **fondo oscuro** y texto claro; si el filtro no deja coincidencias, el popup **desaparece** (no se muestra vacío).",
                "Las palabras clave Lua vienen de LuaLanguageKeywords (misma lista que Spotlight). Cada una tiene tema propio en la pestaña «Lua — sintaxis y librería» del panel de documentación (Ayuda / Proyecto o enlace Lua (sintaxis) en el Hub); Spotlight (Ctrl+P) abre ese tema al confirmar la entrada.",
                "Los nombres tras «tabla.» se generan por reflexión desde clases [LuaVisible] en FUEngine.Runtime (jerarquía de tipos y campos públicos; incluye layer., log., component. tras getComponent, Key./Mouse., etc.); MergeDynamic sigue añadiendo globales extra.",
                "Puedes documentar intención con comentarios ---@param / ---@type (EmmyLua) en el propio .lua; el motor no los ejecuta, pero ayudan si usas LuaLS o revisión en equipo.",
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
                "Si un script no corre: asignado al objeto, Enabled activo, error de sintaxis en consola; si el `.lua` no pasó por el Explorador, confirma que aparece en la pestaña **Scripts** (registro).",
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
                "El build del juego es distinto de compilar el editor FUEngine desde el código (README.md: dotnet publish o instalador)."
            }),

        new(
            id: "troubleshooting-comun",
            title: "Problemas frecuentes (FAQ técnico)",
            paraQue: "Descartar causas típicas antes de depurar a fondo.",
            porQueImporta: "Muchos informes son «no guardé» o «script no registrado».",
            paragraphs: new[]
            {
                "No veo cambios en Play: guarda escena y mapa (Ctrl+S / Guardar todo); comprueba que Play use escena actual o principal según el botón.",
                "El script no hace nada: asignación en Inspector, Enabled, errores en consola; registro en **`scripts.json`** si el archivo lo añadiste fuera del flujo del editor.",
                "onCollision u onInteract no se ejecutan: además de registro y sintaxis, comprueba si tu versión del runtime dispara ese evento; mientras tanto usa onUpdate + overlap o raycast.",
                "El personaje no se mueve: activa UseNativeInput o implementa movimiento en Lua; revisa colisión con tilemap.",
                "No suena el audio: comprueba audio.json, rutas de archivos y volúmenes del proyecto.",
                "La cámara no sigue: ProtagonistInstanceId, UseNativeCameraFollow o un objeto con CameraTarget.",
                "Física rara: recuerda que el círculo en datos es AABB; overlapCircle consulta contra esos rectángulos."
            },
            bullets: new[]
            {
                "Para el flujo de trabajo del proyecto y rutas, revisa README en la raíz del repositorio (si el clon lo incluye)."
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
                "Atajos habituales (pueden variar con el preset): Ctrl+S guardar mapa; Ctrl+Shift+S guardar todo; Ctrl+Z deshacer y Ctrl+Y o Ctrl+Mayús+Z rehacer (también en la barra del mapa); Ctrl+P o Ctrl+Espacio Spotlight (Hub y editor); Ctrl+Espacio en el mini-IDE Lua fuerza autocompletado.",
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
                "**Dos conceptos:** **projectFormatVersion** (versión de esquema del manifiesto .FUE) y **engineVersion** (texto del build que guardó el proyecto). Pueden cambiar por separado: arreglos de motor sin tocar el esquema, o nuevo esquema con el mismo binario si se añaden migraciones.",
                "**Flujo al abrir (puente de migración):** si el .FUE trae **projectFormatVersion** menor que la del motor actual, aparece un diálogo. **Sí** — se aplican los pasos de **ProjectFormatMigration** (en el código: migraciones por versión; hoy son **aditivas**, y en el futuro un paso puede **transformar** datos, p. ej. tipos de coordenadas, no solo añadir campos). Antes de sobrescribir el .FUE, el editor intenta crear una copia **`TuProyecto.FUE.bak`** (mismo nombre + `.bak`); si el disco impide la copia, verás aviso y la migración puede guardarse igual. **No** — el archivo en disco **no** se toca; abres con formato viejo. **Cancelar** — no se abre el proyecto. Tras **No**, el editor **no** bloquea guardar ni impone solo lectura global: **tú** evitas mezclar «formato viejo en manifiesto + datos nuevos» sin criterio — la **consola** puede mostrar un aviso al iniciar la sesión.",
                "**Peligro del «No» sin plan:** este motor asume funciones actuales; si editas largo rato sin migrar y guardas mapas/manifiesto, puedes acercarte a un JSON **híbrido** o expectativas rotas. **Mitigación:** migra pronto (reabre y elige **Sí**), o trabaja en **copia** del proyecto hasta decidir.",
                "**Retrocompatibilidad (lectura):** la intención del diseño es que proyectos de versiones **anteriores** sigan pudiendo **abrirse** en releases posteriores mediante migración o carga tolerante — **no** es una garantía contractual infinita: revisa notas de versión al saltar varias generaciones.",
                "**Downgrade:** si guardas con un motor **nuevo** (campos o formato que el viejo no entiende), un ejecutable **anterior** puede **fallar al abrir** o ignorar datos. Guarda ramas y copias si necesitas volver atrás.",
                "**ProjectEngineCompatibilityChecker** avisa cuando **engineVersion** del archivo difiere del ejecutable (posibles diferencias de comportamiento). Antes de publicar un juego, abre y prueba con el **mismo** build que distribuirás."
            },
            bullets: new[]
            {
                "**Migración Sí:** copia de seguridad automática **`*.FUE.bak`** antes de escribir el .FUE migrado (si el archivo ya existía y la copia es posible).",
                "**Migración No:** riesgo de desalineación; el editor avisa en consola pero no deshabilita todo el guardado.",
                "Downgrade: proyecto guardado con formato/motor nuevo → ejecutable viejo puede no abrirlo.",
                "Objetivo: abrir proyectos antiguos en el motor actual; la vuelta atrás no está garantizada."
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
                "Ver LuaScriptVariableParser en el código fuente del motor y el tema «Variables de script: @prop».",
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
                "FUEngine.Graphics.Vulkan implementa IGraphicsDevice con Silk.NET Vulkan y GLFW para ventana nativa. **Todo el código que usa Silk.NET** del repo vive en este ensamblado; Core y Runtime permanecen libres de bindings de GPU salvo lo que consuman a través de esta capa.",
                "BeginFrame, Clear, EndFrame y el color de fondo son la API mínima expuesta al runtime.",
                "El tab Juego embebido **no** inserta Vulkan dentro del panel WPF: usa **solo** WPF (`GameViewportCanvas` + `GameViewportRenderer`), sin GLFW superpuesto ni intercambio habitual de texturas GPU→WPF. La ventana de ejecución es la que pasa por este backend.",
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
                "Si el código tiene error de sintaxis (por ejemplo falta un end), tras un breve retraso al escribir la línea afectada se marca en rojo en el propio editor (además de los mensajes de error al ejecutar o recargar).",
                "Ctrl+Espacio fuerza el menú de completado; world., self., ads., etc. vienen de reflexión y catálogo. El popup usa **tema oscuro** (texto legible sobre fondo gris) y **se cierra solo** si el filtro no deja ninguna entrada (no queda recuadro vacío).",
                "LuaEditorCompletionCatalog y clases marcadas con LuaVisible definen qué aparece en sugerencias.",
                "Desde el explorador, «Nuevo → Script Lua» usa DefaultLuaScriptTemplate (comentarios @prop y hooks). En Play, require(\"Modulo\") carga Scripts/Modulo.lua (LuaRequireSupport).",
                "Al añadir métodos públicos a las APIs, actualiza también la ayuda integrada del motor y el catálogo de autocompletado."
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
                "Comprueba en el código fuente (PlayModeRunner, arranque de escena) si tu versión ya ejecuta bootstrap; si no, no confíes en el campo hasta documentarse de nuevo."
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
                "Panel «Estado del motor»: último autoguardado detectado entre carpetas Autoguardados/Mapa de los proyectos recientes; conteo de la biblioteca global (texturas tileset/sprite/imagen/UI y .lua en el índice); versión del ejecutable con enlace a esta documentación integrada.",
                "Acciones rápidas: pestaña «Scripts globales» (lista + ScriptEditorControl con resaltado y autocompletado en SharedAssets/Scripts); «Biblioteca» lleva a la pestaña Assets; «No usados» abre el escáner; botón «Buscar en el motor…» abre el panel Spotlight en esta ventana (sin atajo de teclado).",
                "Tips rotativos y recordatorio de atajos; barra inferior: resumen de líneas [Error]/[Critical] en el log de sesión del día y uso aproximado de RAM del proceso; botones «Carpeta» (abre LocalApplicationData/FUEngine/logs) y «Limpiar» (vacía el .log de hoy en disco).",
                "Crear proyecto en el Hub muestra el asistente en un overlay (datos básicos, mapa y tiles: tamaño de tile y de chunk con mini vista previa; sin mapa infinito ni plantilla/paleta en el asistente — la paleta por defecto está en Configuración del motor → Motor). «Generar jerarquía estándar» y carpetas extra/temas: pestaña Explorador. **Archivo → Nuevo proyecto** en el editor cierra el proyecto actual, vuelve al Hub y abre el mismo asistente (evita crear un proyecto «dentro» de otro sin salir). Recientes en AppData: Storage/project_history.json (migración desde recent.json). Miniaturas del Hub: ProjectThumbs/*.png al usar Guardar todo. Al borrar o refrescar, el scroll de las listas vuelve arriba.",
                "FUEngine Spotlight (Ctrl+P o Ctrl+Espacio): mismo buscador integrado en el Hub o en el editor (overlay en la ventana actual, no una ventana extra). Búsqueda unificada con totales del índice, coincidencias por categoría y lista agrupada; incluye manual integrado, Lua (API por reflexión con textos «para qué sirve», hooks KnownEvents, palabras clave con tema dedicado en la pestaña «Lua — sintaxis y librería», biblioteca estándar; el juego en ejecución solo expone parte de la biblioteca Lua — ver LuaEnvironment), archivos .lua/.map/.seed, objetos en escena; en el Hub también proyectos recientes. Confirmar una palabra clave Lua abre su tema concreto en esa pestaña; hooks y API enlazan al manual general. Búsqueda «changelog» o «historial» puede abrir CHANGELOG.md del repo si existe junto al ejecutable.",
                "Pulsa un resultado de documentación para abrir el tema en el panel de ayuda sin salir del flujo: el manual in-app y la referencia Lua comparten el mismo host (tres pestañas: manual, Lua, ejemplos). En el overlay, **Ventana aparte** abre la misma vista en **Documentación FUEngine** (ventana independiente).",
                "Desde Spotlight puedes saltar a un hook concreto (onUpdate, onLayerUpdate…) y leer el párrafo asociado; combínalo con el tema «Eventos y hooks Lua» en el manual completo."
            },
            bullets: new[]
            {
                "Pestaña Assets: mismo índice de biblioteca global que alimenta el resumen del Hub.",
                "Historial de versiones: docs/CHANGELOG.md en la raíz del repositorio (Spotlight: «changelog» o «historial» si el archivo está accesible).",
                "Discord: estado del motor en el perfil — tema «Discord (Rich Presence)»."
            }),

        new(
            id: "asistente-nuevo-proyecto-jerarquia",
            title: "Crear proyecto: Explorador (carpetas) y asistente",
            paraQue: "Controlar qué carpetas extra aparecen en la raíz y qué valores por defecto lleva el proyecto nuevo.",
            porQueImporta: "Config/user_preferences.json (antes settings.json en la raíz de AppData) guarda orden estándar, extras y tema; el asistente valida nombre y ruta antes de crear.",
            paragraphs: new[]
            {
                "En el asistente (desde el Hub o tras **Archivo → Nuevo proyecto**, que te lleva al Hub) marca «Generar jerarquía estándar» para crear en la raíz el orden Sprites, Maps, Scripts, Audio, Seeds (o tu lista personalizada si eliges Personalizado en Configuración del motor → Explorador).",
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
                "La documentación in-app (este manual) y el código fuente del motor son la referencia para comportamiento descriptivo."
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
                "Si el render de partículas evoluciona, actualiza este tema y el inspector de instancia (objetos.json)."
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
                "Detalle de geometría: temas «Física» y «Componentes: inspector, objetos.json y Play»; código en PlayScenePhysicsApi / world.raycast."
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
                "El identificador de la aplicación en el Developer Portal no se publica como número en claro en el código fuente del motor.",
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
                "La documentación in-app se embebe en el ejecutable; si compilas el motor desde fuente, reconstruye para ver textos nuevos."
            },
            bullets: new[]
            {
                "Proyecto abierto: Proyecto → Editor avanzado del proyecto es distinto (resolución, física, FPS, escenas).",
                "README.md del repo describe dotnet publish o installer\\build-installer.ps1 para generar FUEngine desde fuente."
            }),

        new(
            id: "scripts-conectar-motor",
            title: "Conectar scripts con el motor (flujo de trabajo)",
            paraQue: "Pasos claros: registrar el .lua, asignarlo al objeto, exponer parámetros y usar APIs en hooks.",
            porQueImporta: "Play solo ejecuta `.lua` que tienen id en `scripts.json` y están en la lista del objeto; el editor rellena el registro al crear scripts por Explorador o ejemplos.",
            paragraphs: new[]
            {
                "1) Registro: el `.lua` bajo **Scripts/** debe constar en **`scripts.json`** (id + path). **No hace falta editar el JSON:** **Explorador → Nuevo → Script Lua** (y otros flujos de importar/duplicar) **actualizan `scripts.json` automáticamente**. La pestaña Scripts sirve para ver y editar el código y la lista.",
                "2) Asignación: objeto en la jerarquía → Inspector → «Scripts (Lua)» → **Agregar** el id. Puedes tener varios scripts por instancia; el orden importa si comparten estado.",
                "3) Código: hooks (onStart, onUpdate(dt), …). El motor inyecta self, world, input, time… No uses local function onStart — funciones globales del chunk.",
                "4) Datos por instancia: «Variables de script (@prop / globales)» desde `-- @prop` o heurística en la raíz del archivo.",
                "5) Play: Guardar todo. Errores Lua en consola con ruta.",
                "**Arrastrar** un `.lua` desde fuera al **Inspector** solo lo añade si **ya** existe esa ruta en `scripts.json`. Si arrastras un archivo que metiste a mano en el disco sin registrar, créalo o impórtalo por el **Explorador** primero."
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
        topics.AddRange(ScriptExamplesDocumentation.BuildTopics());
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
        string? enMotor = null,
        string? luaExampleCode = null,
        string? exampleCategory = null,
        string? suggestedExportFileName = null,
        string? exampleSearchTags = null,
        string? exampleDifficulty = null)
    {
        Id = id;
        Title = title;
        ParaQue = paraQue;
        PorQueImporta = porQueImporta;
        Paragraphs = paragraphs;
        Bullets = bullets;
        Subtitle = subtitle;
        EnMotor = enMotor;
        LuaExampleCode = luaExampleCode;
        ExampleCategory = exampleCategory;
        SuggestedExportFileName = suggestedExportFileName;
        ExampleSearchTags = exampleSearchTags;
        ExampleDifficulty = exampleDifficulty;
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
    /// <summary>Solo pestaña «Ejemplos de scripts»: cuerpo Lua para el panel derecho.</summary>
    public string? LuaExampleCode { get; }
    /// <summary>Categoría del ejemplo (lista lateral y Spotlight).</summary>
    public string? ExampleCategory { get; }
    /// <summary>Nombre de archivo sugerido al crear <c>.lua</c> desde el ejemplo (solo nombre, p. ej. <c>ejemplo_movimiento.lua</c>).</summary>
    public string? SuggestedExportFileName { get; }
    /// <summary>Palabras extra para filtro de la pestaña y Spotlight (p. ej. «lava fuego tile»).</summary>
    public string? ExampleSearchTags { get; }
    /// <summary>Etiqueta de dificultad: Básico, Intermedio o Avanzado (lista y detalle en ejemplos).</summary>
    public string? ExampleDifficulty { get; }
}
