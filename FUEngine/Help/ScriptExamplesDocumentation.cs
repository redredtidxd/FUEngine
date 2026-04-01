namespace FUEngine.Help;

/// <summary>Ejemplos de scripts Lua listos para copiar (pestaña «Ejemplos de scripts»).</summary>
public static class ScriptExamplesDocumentation
{
    public static IReadOnlyList<DocumentationTopic> BuildTopics()
    {
        return new List<DocumentationTopic>
        {
            new(
                id: EngineDocumentation.ScriptExamplesIntroTopicId,
                title: "Ejemplos de scripts",
                paraQue: "Ir más rápido que leyendo solo la referencia: código que encaja con el runtime y el editor.",
                porQueImporta: "Cada proyecto difiere en nombres de escena, IDs de UI y rutas; los ejemplos son plantillas, no copiar-pegar ciego.",
                paragraphs: new[]
                {
                    "Aquí solo hay ejemplos de Lua (la pestaña Manual del motor explica editor, mapa, JSON y Play; la pestaña Lua — sintaxis y librería es la referencia de lenguaje y patrones).",
                    "Copia al portapapeles o usa el botón «Crear script desde este ejemplo» (con proyecto abierto) para generar un .lua en Scripts/ y registrarlo en scripts.json.",
                    "Orden sugerido al aprender: primero los temas «Inspector, scripts y @prop» y «Variables expuestas (@prop)» (categoría Editor); luego gameplay, escenas, UI, mapa, require y notas avanzadas. Usa el filtro de la lista para saltar a un tema por palabra o categoría.",
                    "Los valores que edita el diseñador por instancia (vida, daño, velocidad…) suelen estar en el Inspector: componentes del objeto y el bloque «Variables de script (@prop / globales)» generado desde líneas -- @prop en el .lua.",
                    "Algunos ejemplos asumen protagonista nativo, triggerZones.json o Canvas con Ids concretos: sustituye nombres por los de tu proyecto.",
                    "Dificultad: 🟢 Básico, 🟡 Intermedio, 🔴 Avanzado. Filtra por palabras (lava, tile, @prop) o usa Spotlight (Ctrl+P).",
                    "Plantilla al crear .lua desde el explorador: @prop y hooks (onStart, onUpdate, onDestroy). Código compartido: require(\"Modulo\") con Scripts/Modulo.lua (ver ejemplos require y tabla local).",
                },
                bullets: new[]
                {
                    "«Crear script desde este ejemplo» (proyecto abierto) crea el archivo en Scripts/; «Copiar al portapapeles» no registra el script.",
                    "Si algo falla: consola del editor (Lua), referencia de APIs en el manual y pestaña Lua para sintaxis.",
                },
                subtitle: "Introducción",
                exampleSearchTags: "introducción inicio ayuda snippets copiar crear scripts pestaña dificultad buscar spotlight"),

            new(
                id: "script-ex-basic-movement",
                title: "Movimiento básico (protagonista nativo)",
                paraQue: "Mover al jugador con entrada nativa cuando el objeto usa UseNativeInput.",
                porQueImporta: "Evitas duplicar lógica de teclas si el motor ya controla al protagonista.",
                paragraphs: new[]
                {
                    "Este patrón asume un objeto con tag player y protagonista nativo activo en el proyecto. La posición suele actualizarse el solo; aquí solo reaccionas en onUpdate.",
                },
                bullets: new[]
                {
                    "Si no usas entrada nativa, usa input.isKeyDown / self.x, self.y según tu setup.",
                },
                subtitle: "Entrada y gameplay",
                luaExampleCode: @"-- Movimiento: reacción en frame (protagonista nativo)
-- Requiere: objeto con script asignado; proyecto con entrada nativa si aplica.

function onStart()
    -- self apunta al objeto del script
end

function onUpdate(dt)
    -- Ejemplo: registrar posición (debug mental)
    -- local x = self.x or 0
    -- local y = self.y or 0
    -- No fuerces self.x/y si UseNativeInput mueve al protagonista por otro canal.
end
",
                exampleCategory: "Gameplay",
                suggestedExportFileName: "ejemplo_movimiento_basico.lua",
                exampleSearchTags: "movimiento jugador entrada teclado onUpdate player",
                exampleDifficulty: "Básico"),

            new(
                id: "script-ex-water-fx",
                title: "Cascada / agua (partículas y datos)",
                paraQue: "Recordar que el emisor de partículas se configura en el Inspector; Lua complementa la lógica.",
                porQueImporta: "El visor de partículas en Play puede ser limitado; los datos (textura, tasas) viven en objetos.json.",
                paragraphs: new[]
                {
                    "Habilita ParticleEmitter en el Inspector del objeto y asigna textura. En Lua puedes variar visibilidad, audio o @prop mientras el efecto está definido en datos.",
                },
                bullets: new[]
                {
                    "Para «agua» animada suele bastar sprite + capa; partículas son opcionales según tu build.",
                },
                subtitle: "Efectos",
                luaExampleCode: @"-- Cascada: datos de partículas en Inspector + lógica opcional en Lua
-- @prop intensidad: float = 1

function onUpdate(dt)
    if (self.active == false) then return end
    -- intensidad viene del Inspector si usas @prop
    -- audio.play(""agua_loop"", { loop = true, volume = 0.35 * (intensidad or 1) })
end

function onDestroy()
    -- audio.stop(""agua_loop"")
end
",
                exampleCategory: "Efectos",
                suggestedExportFileName: "ejemplo_cascada_agua.lua",
                exampleSearchTags: "agua cascada partículas lava fuego fluido efecto",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-scene-zone",
                title: "Cambiar escena al entrar en zona (triggerZones)",
                paraQue: "Cargar otra escena cuando el jugador entra en un rectángulo del mapa.",
                porQueImporta: "Las zonas en triggerZones.json ejecutan ScriptIdOnEnter en Play; el script debe estar en scripts.json.",
                paragraphs: new[]
                {
                    "Configura la zona en triggerZones.json y pon este ID en ScriptIdOnEnter. Ajusta el nombre de escena al que tenga tu proyecto (game.loadScene).",
                },
                bullets: new[]
                {
                    "Guarda el mapa y triggerZones antes de probar Play.",
                },
                subtitle: "Escenas",
                luaExampleCode: @"-- Zona de mapa: asignar este script por ID en ScriptIdOnEnter de la zona
-- El host pasa el jugador a onZoneEnter(other)

function onZoneEnter(other)
    if game and game.loadScene then
        game.loadScene(""MiEscenaSiguiente"")
    end
end
",
                exampleCategory: "Escenas",
                suggestedExportFileName: "ejemplo_zona_cambio_escena.lua",
                exampleSearchTags: "zona trigger escena loadScene mapa portal",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-health-simple",
                title: "Salud simple (Health en datos + daño en Lua)",
                paraQue: "Reducir vida desde colisión o trigger usando HealthComponent si está en el objeto.",
                porQueImporta: "El componente se crea desde el Inspector; invoke expone métodos marcados en el motor.",
                paragraphs: new[]
                {
                    "Activa Health en «Sprite, física y gameplay». El proxy getComponent devuelve métodos si el tipo los expone al Lua.",
                },
                bullets: new[]
                {
                    "Si invoke no está disponible, lleva la vida en variables @prop como alternativa.",
                },
                subtitle: "Combate / estado",
                luaExampleCode: @"-- Salud simple con @prop (portable entre versiones del motor)
-- @prop vida: float = 100
-- @prop maxVida: float = 100

function onStart()
    if maxVida and maxVida > 0 and (not vida or vida <= 0) then
        vida = maxVida
    end
end

function onCollision(other)
    if other == nil then return end
    if not vida then return end
    vida = vida - 10
    if vida <= 0 then
        self.active = false
    end
end
",
                exampleCategory: "Gameplay",
                suggestedExportFileName: "ejemplo_salud_simple.lua",
                exampleSearchTags: "vida salud daño combate health colisión",
                exampleDifficulty: "Básico"),

            new(
                id: "script-ex-inspector-props",
                title: "Inspector, scripts y @prop (dónde se edita cada cosa)",
                paraQue: "Saber en qué panel del editor tocar vida, daño, música o parámetros de Lua sin perderse.",
                porQueImporta: "El mismo .lua puede servir para lava, enemigos o UI; lo que cambia es la instancia y el Inspector.",
                enMotor:
                    "1) En la jerarquía, selecciona el objeto (jugador, lava, enemigo, etc.). 2) Inspector: identidad y transform. 3) «Sprite, física y gameplay»: collider (sólido vs trigger), Health si lo activas, partículas, UseNativeInput, etc. — ahí están muchas «vidas» y flags del motor. 4) «Scripts (Lua)»: añade entradas con el ScriptId que figura en scripts.json. 5) Debajo, «Variables de script (@prop / globales)»: aquí aparecen -- @prop nombre: tipo = valor del .lua; el diseñador cambia números por instancia (cuánto quita la lava, cooldown, nombre de clip de audio lógico, etc.). No uses nombres de hooks reservados (onUpdate…) como @prop.",
                paragraphs: new[]
                {
                    "Ejemplo mental «lava»: el daño por segundo o el total puede ser un @prop danio en el script del área; el collider IsTrigger y la etiqueta del jugador se configuran en el Inspector del objeto lava, no dentro del archivo .lua.",
                    "La música de fondo suele ir en datos de escena/audio del proyecto; un script puede llamar audio.playMusic con un id que hayas definido en el manifiesto — revisa tu proyecto.",
                },
                bullets: new[]
                {
                    "Componente Health + getComponent(\"Health\"):invoke(\"takeDamage\", n) si tu build expone ese método; si no, @prop vida como en el ejemplo de salud simple.",
                    "Multiselección: el inspector puede ocultar @prop; edita una instancia a la vez para valores distintos.",
                },
                subtitle: "Editor",
                luaExampleCode: @"-- Las líneas -- @prop generan filas en «Variables de script» del Inspector
-- @prop danioAlContacto: float = 15
-- @prop etiquetaJugador: string = ""player""

function onTriggerEnter(other)
    if other == nil or etiquetaJugador == nil then return end
    if other.hasTag and not other:hasTag(etiquetaJugador) then return end
    local health = other:getComponent(""Health"")
    if health and health.invoke then
        health:invoke(""takeDamage"", danioAlContacto or 10)
    end
end
",
                exampleCategory: "Editor",
                suggestedExportFileName: "ejemplo_inspector_danio_trigger.lua",
                exampleSearchTags: "inspector propiedades @prop variables daño lava fuego",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-scene-key-or-button",
                title: "Cambiar de escena con tecla o con botón UI",
                paraQue: "Ir a otra escena desde Play con input o al pulsar un botón del Canvas.",
                porQueImporta: "game.loadScene pide al host cargar otra escena; el nombre debe coincidir con el de tu proyecto.",
                enMotor:
                    "Tecla: un script en un objeto que esté activo en Play (p. ej. controlador global o el jugador) con onUpdate e input.isKeyDown(Key). Botón: crea un Canvas en la jerarquía de la escena, añade un Button con un Id estable (Inspector del elemento UI). En onStart del script registra ui.bind(canvasId, elementId, \"click\", function …) con los mismos ids. Solo el canvas con focus recibe el clic; usa ui.setFocus si tienes varios.",
                paragraphs: new[]
                {
                    "Sustituye NombreEscenaDestino por el nombre real de la escena en tu proyecto (como aparece al guardar / en la lista de escenas).",
                    "Sustituye MapGui y BtnJugar por el Id del Canvas (propiedad Id del canvas) y el Id del botón en el árbol UI.",
                },
                bullets: new[]
                {
                    "Si loadScene no hace nada, comprueba que Play Mode del editor tenga el callback OnLoadScene configurado (versión del motor).",
                    "ui.bind recibe en Lua una función; el runtime puede pasar (canvasId, elementId, eventName, x, y) al callback.",
                },
                subtitle: "Escenas / UI",
                luaExampleCode: @"-- A) Tecla (p. ej. script en objeto siempre activo en la escena)
function onUpdate(dt)
    if input.isKeyDown(Key.Enter) and game and game.loadScene then
        game.loadScene(""NombreEscenaDestino"")
    end
end

-- B) Botón en Canvas (ids = los del Inspector: Canvas.Id y Button.Id)
function onStart()
    if ui and ui.bind then
        ui.bind(""MapGui"", ""BtnJugar"", ""click"", function(cid, eid, ev, x, y)
            if game and game.loadScene then
                game.loadScene(""NombreEscenaDestino"")
            end
        end)
        if ui.setFocus then ui.setFocus(""MapGui"") end
    end
end
",
                exampleCategory: "Escenas",
                suggestedExportFileName: "ejemplo_escena_tecla_o_boton.lua",
                exampleSearchTags: "escena tecla botón UI loadScene enter",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-ui-canvas-switch",
                title: "Cambiar el MapGui (mostrar u ocultar canvas)",
                paraQue: "Dejar de ver un HUD y mostrar otro (menú, diálogo, minimapa) sin cambiar de escena.",
                porQueImporta: "ui.show / ui.hide / ui.setFocus operan sobre el Id de cada UICanvas en la escena.",
                enMotor:
                    "Cada Canvas en la jerarquía tiene un Id único (p. ej. MapGui, MenuPausa). En Play, el backend de UI marca visibles los canvas por defecto; al llamar ui.hide(\"MapGui\") dejas de dibujar ese overlay. ui.setFocus decide qué canvas recibe clics. pushState/popState guardan y restauran visibilidad para menús anidados.",
                paragraphs: new[]
                {
                    "Ejemplo: al abrir pausa, ocultas el HUD de juego y muestras MenuPausa; al cerrar, al revés.",
                },
                bullets: new[]
                {
                    "Los ids son sensibles a mayúsculas según cómo los guardes; mantén el mismo texto en Lua y en el JSON/UI.",
                },
                subtitle: "UI",
                luaExampleCode: @"-- Cambiar qué canvas se ve (Ids = UICanvas.Id en el Inspector)
function mostrarSoloMenuPausa()
    if not ui then return end
    ui.hide(""MapGui"")
    ui.show(""MenuPausa"")
    ui.setFocus(""MenuPausa"")
end

function volverAlHud()
    if not ui then return end
    ui.hide(""MenuPausa"")
    ui.show(""MapGui"")
    ui.setFocus(""MapGui"")
end

-- Atajo: guardar estado antes de un popup
function onStart()
    -- ui.pushState() antes de abrir un submenú; ui.popState() al cerrarlo
end
",
                exampleCategory: "UI",
                suggestedExportFileName: "ejemplo_ui_canvas_switch.lua",
                exampleSearchTags: "canvas HUD menú pausa UI show hide focus",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-map-tiles-or-scene",
                title: "Cambiar el mapa: tiles en runtime u otra escena",
                paraQue: "Modificar el terreno en Play o cargar un .map distinto de forma controlada.",
                porQueImporta: "En la API actual no hay world.loadMap desde Lua; el mapa completo suele ir ligado a la escena.",
                enMotor:
                    "Para rellenar o borrar celdas en la capa activa en memoria usa world.getTile / world.setTile con el nombre de capa y el id de catálogo del tileset. Para «otro mapa entero» (otro archivo .map, otra disposición de capas), lo habitual es tener una segunda escena y llamar game.loadScene — al cargarla el host monta el TileMap de esa escena. Asegúrate de que la capa exista y tenga tileset en el proyecto.",
                paragraphs: new[]
                {
                    "world.setTile(tx, ty, \"NombreCapa\", catalogId) usa coordenadas en casillas; catalogId 0 borra la celda.",
                    "Si solo necesitas una «sala nueva», a menudo es más simple una escena nueva que mutar todo el tilemap a mano.",
                },
                bullets: new[]
                {
                    "El nombre de capa debe coincidir con el de Map / Layers en el editor.",
                },
                subtitle: "Mapa / escena",
                luaExampleCode: @"-- Romper un tile en la capa ""Solid"" (ajusta nombre e id de catálogo)
function onInteract(player)
    if not world or not world.setTile then return end
    local tx = math.floor((self.x or 0) + 1)
    local ty = math.floor((self.y or 0))
    world.setTile(tx, ty, ""Solid"", 0) -- 0 = vaciar celda
end

-- «Cambiar de mapa» completo: otra escena con otro .map
function irAlMundo2()
    if game and game.loadScene then
        game.loadScene(""Mundo2"")
    end
end
",
                exampleCategory: "Mapa",
                suggestedExportFileName: "ejemplo_mapa_tiles_o_escena.lua",
                exampleSearchTags: "mapa tiles setTile world escena loadScene lava fuego",
                exampleDifficulty: "Avanzado"),

            new(
                id: "script-ex-break-tile-on-hit",
                title: "Romper bloque al tocarlo (tile con world.setTile)",
                paraQue: "Borrar una celda de la capa cuando el jugador choca con un bloque (objeto con collider).",
                porQueImporta: "world.setTile con id 0 vacía la casilla; útil para ladrillos, tesoros o terreno destructible.",
                paragraphs: new[]
                {
                    "Asigna este script a un objeto «bloque» con collider sólido y etiqueta que reconozcas. Ajusta nombreCapa al de tu capa de tiles y tileSize al tamaño en píxeles del proyecto (Project / tile size).",
                    "Las coordenadas de world.setTile son en casillas; convierte self.x/self.y usando tileSize.",
                },
                bullets: new[]
                {
                    "Si el jugador no tiene tag player, cambia la comprobación hasTag.",
                },
                subtitle: "Mapa / gameplay",
                luaExampleCode: @"-- @prop nombreCapa: string = ""Solid""
-- @prop tileSize: float = 32

function onCollision(other)
    if other == nil or not other.hasTag then return end
    if not other:hasTag(""player"") then return end
    if not world or not world.setTile then return end
    local ts = tileSize or 32
    local tx = math.floor((self.x or 0) / ts)
    local ty = math.floor((self.y or 0) / ts)
    world.setTile(tx, ty, nombreCapa or ""Solid"", 0)
    self.active = false
end
",
                exampleCategory: "Mapa",
                suggestedExportFileName: "ejemplo_romper_bloque_tile.lua",
                exampleSearchTags: "romper bloque tile setTile colisión mapa casilla destruir lava fuego ladrillo",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-ui-sign-near-e",
                title: "Cartel de texto (Canvas + tecla E cerca del cartel)",
                paraQue: "Mostrar un aviso en pantalla cuando el jugador está cerca y pulsa interactuar.",
                porQueImporta: "La API ui no sustituye aún a un «showText» mágico: el mensaje visible se define en el elemento Text del Canvas en el editor; Lua solo muestra u oculta el canvas.",
                enMotor:
                    "Crea un UICanvas (p. ej. Id CartelAviso) con un Text cuyo contenido ya diga «¡Cuidado con la lava!» en el Inspector de UI. Deja el canvas oculto al inicio (o muéstralo solo desde Lua). Este script va en un objeto trigger o marcador cerca del cartel: calcula distancia al jugador (world.findByTag(\"player\")) y si está cerca y input.isKeyDown(Key.E), llama ui.show(\"CartelAviso\") y ui.setFocus si hace falta.",
                paragraphs: new[]
                {
                    "Sustituye CartelAviso por el Id real de tu canvas. Ajusta radioPx si tus unidades no coinciden con píxeles de mundo.",
                },
                bullets: new[]
                {
                    "Si findByTag devuelve una lista/tabla, el primer jugador suele ser el índice 1 en NLua.",
                },
                subtitle: "UI / interacción",
                luaExampleCode: @"-- @prop radioPx: float = 96

function onUpdate(dt)
    if not world or not input or not ui then return end
    local players = world.findByTag(""player"")
    if not players then return end
    local p = players[1]
    if p == nil then return end
    local dx = (p.x or 0) - (self.x or 0)
    local dy = (p.y or 0) - (self.y or 0)
    local r = radioPx or 96
    if (dx * dx + dy * dy) > (r * r) then return end
    if input.isKeyDown(Key.E) then
        ui.show(""CartelAviso"")
        if ui.setFocus then ui.setFocus(""CartelAviso"") end
    end
end
",
                exampleCategory: "UI",
                suggestedExportFileName: "ejemplo_cartel_texto_e.lua",
                exampleSearchTags: "cartel diálogo mensaje texto UI tecla E interactuar aviso lava fuego HUD",
                exampleDifficulty: "Básico"),

            new(
                id: "script-ex-enemy-chase",
                title: "Enemigo que te sigue (IA de persecución simple)",
                paraQue: "Mover un enemigo hacia el jugador comparando posiciones en onUpdate.",
                porQueImporta: "Patrón base antes de pathfinding; sirve para slimes, murciélagos o guardias en salas pequeñas.",
                paragraphs: new[]
                {
                    "Asigna el script a un objeto enemigo (sin protagonista nativo). El jugador debe tener tag player. Normaliza el vector (dx,dy) para velocidad constante.",
                },
                bullets: new[]
                {
                    "Si hay varios jugadores, elige el [1] o el más cercano.",
                },
                subtitle: "Gameplay / IA",
                luaExampleCode: @"-- @prop velocidad: float = 70

function onUpdate(dt)
    if not world then return end
    local players = world.findByTag(""player"")
    if not players then return end
    local p = players[1]
    if p == nil then return end
    local dx = (p.x or 0) - (self.x or 0)
    local dy = (p.y or 0) - (self.y or 0)
    local len = math.sqrt(dx * dx + dy * dy)
    if len < 0.01 then return end
    local sp = (velocidad or 70) * dt
    self.x = (self.x or 0) + (dx / len) * sp
    self.y = (self.y or 0) + (dy / len) * sp
end
",
                exampleCategory: "Gameplay",
                suggestedExportFileName: "ejemplo_enemigo_sigue.lua",
                exampleSearchTags: "enemigo IA persecución seguir jugador movimiento enemigo chase",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-prop-speed-inspector",
                title: "Variables expuestas (@prop): velocidad desde el Inspector",
                paraQue: "Cambiar números y textos sin editar el .lua: el diseñador ajusta en «Variables de script».",
                porQueImporta: "El motor genera campos en el Inspector a partir de líneas -- @prop tipo nombre: tipo = valor por defecto.",
                paragraphs: new[]
                {
                    "Tras guardar el script, selecciona el objeto en la jerarquía: bajo Scripts verás speed y etiqueta como filas editables por instancia.",
                    "No uses nombres de hooks (onUpdate, onStart…) como nombre de @prop.",
                },
                bullets: new[]
                {
                    "Tipos habituales: float, int, string, bool — según soporte del parser de variables en tu versión del motor.",
                },
                subtitle: "Editor / @prop",
                luaExampleCode: @"-- El diseñador cambia ""speed"" y ""etiqueta"" por objeto, sin tocar el código.
-- @prop speed: float = 200
-- @prop etiqueta: string = ""En movimiento""

function onUpdate(dt)
    local s = speed or 120
    self.x = (self.x or 0) + s * dt
    -- print(etiqueta) -- depuración opcional
end
",
                exampleCategory: "Editor",
                suggestedExportFileName: "ejemplo_prop_velocidad_inspector.lua",
                exampleSearchTags: "@prop inspector variables velocidad speed diseñador instancia expuesto",
                exampleDifficulty: "Básico"),

            new(
                id: "script-ex-require-module",
                title: "require: módulo en Scripts/ (reutilizar código)",
                paraQue: "Dividir helpers en `Scripts/MiModulo.lua` y cargarlos con `require` desde el script del objeto.",
                porQueImporta: "El motor resuelve solo rutas bajo `Scripts/`; la caché se limpia al recargar el script.",
                paragraphs: new[]
                {
                    "Crea un segundo archivo en la carpeta Scripts del proyecto, p. ej. `Scripts/MiHelpers.lua`, que termine devolviendo una tabla.",
                    "En el script del objeto: `local H = require(\"MiHelpers\")` (sin `.lua`). No uses `..` ni rutas absolutas en el nombre.",
                },
                bullets: new[]
                {
                    "El módulo comparte el mismo entorno de APIs (`world`, `self`, …) al ejecutarse.",
                },
                subtitle: "Módulos",
                luaExampleCode: @"-- En Scripts/MiHelpers.lua (otro archivo):
-- return {
--   clamp = function(x, a, b) if x < a then return a end if x > b then return b end return x end
-- }

local H = require(""MiHelpers"")

function onUpdate(dt)
    if H and H.clamp then
        local _ = H.clamp(self.x or 0, 0, 100)
    end
end
",
                exampleCategory: "Arquitectura",
                suggestedExportFileName: "ejemplo_require_modulo.lua",
                exampleSearchTags: "require módulo module package Scripts librería reutilizar",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-table-helpers-same-file",
                title: "Helpers en un solo archivo (tabla local, sin require)",
                paraQue: "Organizar funciones reutilizables al inicio del mismo .lua cuando no quieras múltiples archivos.",
                porQueImporta: "Funciona en cualquier build; no depende de rutas ni de `require`.",
                paragraphs: new[]
                {
                    "Define `local MiLib = {}` y funciones como `MiLib.danio = function … end` antes de los hooks; llama `MiLib.danio(...)` desde onCollision u onUpdate.",
                },
                bullets: new[]
                {
                    "Evita globales sueltas con nombres genéricos (`x`, `tmp`) para no chocar con @prop.",
                },
                subtitle: "Arquitectura",
                luaExampleCode: @"local Lib = {}

function Lib.sqrDist(ax, ay, bx, by)
    local dx, dy = ax - bx, ay - by
    return dx * dx + dy * dy
end

function onUpdate(dt)
    local p = world and world.findNearestByTag and world.findNearestByTag(""player"", self.x or 0, self.y or 0)
    if p == nil then return end
    local d2 = Lib.sqrDist(self.x or 0, self.y or 0, p.x or 0, p.y or 0)
    if d2 < 64 * 64 then
        -- cerca del jugador
    end
end
",
                exampleCategory: "Arquitectura",
                suggestedExportFileName: "ejemplo_tabla_helpers.lua",
                exampleSearchTags: "tabla helper librería local módulo mismo archivo sin require",
                exampleDifficulty: "Básico"),

            new(
                id: "script-ex-external-lua-libraries",
                title: "Librerías .lua externas (copiar, pegar, adaptar)",
                paraQue: "Aprovechar utilidades de internet (p. ej. funciones de tablas o interpolación) dentro del sandbox del motor.",
                porQueImporta: "Muchas librerías usan `io`, `os` o `require` de luarocks: aquí no están disponibles tal cual.",
                paragraphs: new[]
                {
                    "Si el código solo usa `math`, `string`, `table` y funciones puras, suele bastar con copiar las funciones que necesitas al final de tu script o a un `Scripts/MiLib.lua` y cargarlo con `require`.",
                    "Si la librería abre archivos o red, tendrás que reescribir esa parte o no usarla en Play.",
                },
                bullets: new[]
                {
                    "Revisa la licencia del snippet antes de publicar tu juego.",
                },
                subtitle: "Arquitectura",
                luaExampleCode: @"-- Patrón: copiar solo las funciones necesarias (ej. mezclar una tabla)
local function shuffle(t)
    for i = #t, 2, -1 do
        local j = math.random(i)
        t[i], t[j] = t[j], t[i]
    end
end

function onStart()
    local lista = { 1, 2, 3, 4, 5 }
    shuffle(lista)
end
",
                exampleCategory: "Arquitectura",
                suggestedExportFileName: "ejemplo_lib_externa_adaptada.lua",
                exampleSearchTags: "librería externa copiar lume inspect luarocks sandbox",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-plugins-csharp-note",
                title: "Plugins C#, DLL y [LuaVisible] (expectativas realistas)",
                paraQue: "Entender qué puede ampliar un desarrollador C# frente a un usuario que solo escribe Lua.",
                porQueImporta: "Las APIs Lua del motor (`world`, `Debug`, …) salen de clases en FUEngine.Runtime marcadas con [LuaVisible]; no basta con copiar una DLL arbitraria a la carpeta Plugins.",
                enMotor:
                    "ProjectInfo.ProjectEnabledPlugins y PluginLoader en Core están pensados para extensiones del editor; LoadFromDirectory es un stub hasta que se defina el modelo. Ampliar Lua con ensamblados de terceros no está soportado como producto documentado: para nuevas APIs hace falta compilar contra el motor y exponer tipos de forma controlada.",
                paragraphs: new[]
                {
                    "Si contribuyes al repositorio del motor, añade métodos en las APIs existentes o nuevas clases [LuaVisible] en Runtime y el autocompletado las recogerá.",
                },
                bullets: new[]
                {
                    "Lee el tema del manual «Plugins del proyecto (estado)» para el alcance actual.",
                },
                subtitle: "Motor / extensión",
                luaExampleCode: @"-- No hay snippet único: Lua llama a APIs ya expuestas desde C#.
-- Ejemplo de uso desde Lua (según tu build):
-- local h = other:getComponent(""Health"")
-- if h and h.invoke then h:invoke(""takeDamage"", 5) end
",
                exampleCategory: "Motor",
                suggestedExportFileName: "ejemplo_plugins_nota.lua",
                exampleSearchTags: "C# plugin DLL LuaVisible extensión nativo ensamblado",
                exampleDifficulty: "Avanzado"),

            new(
                id: "script-ex-day-night-debug",
                title: "Idea día / noche (oscurecer con Debug, sin luz global en Lua)",
                paraQue: "Simular un ciclo visual rudimentario cuando aún no hay API de luz ambiental en Lua.",
                porQueImporta: "Debug dibuja sobre el viewport del tab Juego; es una aproximación, no reemplaza iluminación del renderer.",
                paragraphs: new[]
                {
                    "Usa `time.seconds` para una fase lenta y `Debug.drawCircle` con radio grande y color oscuro semitransparente centrado en el jugador o en la cámara lógica.",
                    "Para iluminación real de escena, el flujo suele pasar por datos del proyecto y componentes de luz en el editor, no solo por Lua.",
                },
                bullets: new[]
                {
                    "Demasiados comandos Debug por frame puede afectar rendimiento; úsalo como prueba.",
                },
                subtitle: "Gameplay / efectos",
                luaExampleCode: @"-- @prop radioOscuro: float = 24

function onUpdate(dt)
    if not Debug or not time then return end
    local t = time.seconds or 0
    local phase = (math.sin(t * 0.15) + 1) * 0.5
    local cx = self.x or 0
    local cy = self.y or 0
    local r = radioOscuro or 24
    local a = 40 + math.floor(80 * phase)
    Debug.drawCircle(cx, cy, r, 5, 5, 30, a)
end
",
                exampleCategory: "Gameplay",
                suggestedExportFileName: "ejemplo_dia_noche_debug.lua",
                exampleSearchTags: "día noche oscuridad tiempo iluminación debug atmósfera",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-shake-manual",
                title: "Sacudida manual (jitter en self, sin API camera.shake)",
                paraQue: "Dar feedback de impacto moviendo ligeramente el objeto (p. ej. cámara rig o sprite) unos frames.",
                porQueImporta: "No hay `camera.shake` en la API Lua actual; el efecto se aproxima con ruido en la posición.",
                paragraphs: new[]
                {
                    "Guarda la posición base al empezar la sacudida; cada frame añade un offset aleatorio pequeño y decrementa un temporizador.",
                },
                bullets: new[]
                {
                    "Si el protagonista lo mueve el motor con entrada nativa, prueba el script en un objeto «cámara» o overlay que tú controles.",
                },
                subtitle: "Gameplay",
                luaExampleCode: @"-- @prop shakeDuracion: float = 0.2
-- @prop shakeMagnitud: float = 3

local _baseX, _baseY
local _shakeT = 0

function onUpdate(dt)
    if _shakeT > 0 then
        _shakeT = _shakeT - (dt or 0)
        if _shakeT <= 0 then
            self.x = _baseX
            self.y = _baseY
            return
        end
        local m = shakeMagnitud or 3
        self.x = _baseX + (math.random() * 2 - 1) * m
        self.y = _baseY + (math.random() * 2 - 1) * m
        return
    end
    _baseX = self.x or 0
    _baseY = self.y or 0
end

function onCollision(other)
    if other == nil then return end
    _baseX = self.x or 0
    _baseY = self.y or 0
    _shakeT = shakeDuracion or 0.2
end
",
                exampleCategory: "Gameplay",
                suggestedExportFileName: "ejemplo_shake_manual.lua",
                exampleSearchTags: "shake sacudida cámara jitter impacto feedback",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-persistencia-nota",
                title: "Guardado de partida (estado del motor y limitaciones)",
                paraQue: "Saber por qué no hay `file.writeJSON` en Lua del sandbox y qué usar en su lugar.",
                porQueImporta: "Persistencia implica I/O y seguridad; el runtime no expone `io` ni escritura libre por defecto.",
                paragraphs: new[]
                {
                    "Para prototipos, anota valores con `print` o variables en el Inspector; para guardado real hace falta un canal definido por el proyecto/host (exportación, datos de juego, etc.).",
                    "Cualquier sistema nuevo de guardado debería documentarse en el manual general cuando exista API estable.",
                },
                bullets: new[]
                {
                    "No confíes en rutas absolutas ni en APIs de otros motores (`love.filesystem`, etc.).",
                },
                subtitle: "Datos",
                luaExampleCode: @"-- Pseudocódigo / recordatorio: no hay API genérica file.* en el sandbox descrito.
-- local estado = { nivel = 3, x = self.x }
-- ... persistencia vía sistema del juego cuando esté disponible ...

function onStart()
    -- print(""Posición inicial: "", self.x, self.y)
end
",
                exampleCategory: "Datos",
                suggestedExportFileName: "ejemplo_persistencia_nota.lua",
                exampleSearchTags: "guardar save json persistencia disco datos partida",
                exampleDifficulty: "Avanzado"),
        };
    }
}
