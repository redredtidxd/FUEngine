namespace FUEngine.Help;

/// <summary>
/// Ejemplos de scripts Lua listos para copiar (pestaña «Ejemplos de scripts»).
/// Cada tema debe cubrir: <b>para qué</b> (<see cref="DocumentationTopic.ParaQue"/>), <b>por qué importa</b> (<see cref="DocumentationTopic.PorQueImporta"/>),
/// <b>dónde / cómo</b> en el bloque <b>En FUEngine</b> (<see cref="DocumentationTopic.EnMotor"/>) y en párrafos, y <b>ejemplo</b> en <see cref="DocumentationTopic.LuaExampleCode"/> + puntos clave.
/// </summary>
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
                    "En la ficha de cada tema, la UI de ayuda muestra en orden: **Para qué**, **Por qué importa**, el recuadro **En FUEngine** (dónde está la opción en el editor o qué API usar), **Contenido** (cómo paso a paso) y **Puntos clave**; el **código** va al final (copiar o «Crear script desde este ejemplo»). Mantén esa lógica al añadir temas nuevos.",
                    "Aquí solo hay ejemplos de Lua (la pestaña Manual del motor explica editor, mapa, JSON y Play; la pestaña Lua — sintaxis y librería es la referencia de lenguaje y patrones).",
                    "Copia al portapapeles o usa el botón «Crear script desde este ejemplo» (con proyecto abierto) para generar un .lua en Scripts/ y registrarlo en scripts.json.",
                    "Orden sugerido al aprender: primero los temas «Inspector, scripts y @prop» y «Variables expuestas (@prop)» (categoría Editor); luego el ejemplo **gameplay movimiento WASD** (id `script-ex-gameplay-movimiento-wasd-flechas`), más gameplay, escenas, UI, mapa, require y notas. **Buscar:** escribe en el filtro palabras del título (p. ej. `zona`, `loadScene`, `canvas`, `atprop`) o el id completo.",
                    "Los valores que edita el diseñador por instancia (vida, daño, velocidad…) suelen estar en el Inspector: componentes del objeto y el bloque «Variables de script (@prop / globales)» generado desde líneas -- @prop en el .lua.",
                    "Algunos ejemplos asumen protagonista nativo, triggerZones.json o Canvas con Ids concretos: sustituye nombres por los de tu proyecto.",
                    "Dificultad: 🟢 Básico, 🟡 Intermedio, 🔴 Avanzado. Filtra por palabras (lava, tile, @prop) o usa Spotlight (Ctrl+P).",
                    "Plantilla al crear .lua desde el explorador: @prop y hooks (onStart, onUpdate, onDestroy). Código compartido: require(\"Modulo\") con Scripts/Modulo.lua (ver ejemplos require y tabla local).",
                },
                bullets: new[]
                {
                    "Cada ejemplo debe dejar claro **para qué sirve**, **dónde** tocas el editor (Inspector, jerarquía, pestaña Juego…), **cómo** enlazar script ↔ objeto ↔ hooks, y **ejemplos** (bloque Lua + notas).",
                    "«Crear script desde este ejemplo» (proyecto abierto) crea el archivo en Scripts/; «Copiar al portapapeles» no registra el script.",
                    "Si algo falla: consola del editor (Lua), referencia de APIs en el manual y pestaña Lua para sintaxis.",
                },
                subtitle: "Introducción",
                exampleSearchTags: "introducción inicio ayuda snippets copiar crear scripts pestaña dificultad buscar spotlight script-ex lista filtros"),

            new(
                id: "script-ex-gameplay-movimiento-wasd-flechas",
                title: "Gameplay: movimiento con WASD y flechas (cuatro direcciones)",
                paraQue: "Mover cualquier objeto en Play con teclado: A/D o flechas izquierda/derecha, W/S o flechas arriba/abajo.",
                porQueImporta: "Es el patrón habitual para personajes, pruebas y NPCs que tú controlas con `self.x` / `self.y` e `input.isKeyDown`.",
                enMotor:
                    "Asigna el script al objeto en la **jerarquía** (cualquier instancia). Registra el .lua en **scripts.json** y el id en **Inspector → Scripts (Lua)**. En **Sprite, física y gameplay**, desactiva **UseNativeInput** / protagonista nativo en **este** objeto si el motor ya mueve otro personaje con el mismo input; si no, dos sistemas pueden pugnar por la posición. Prueba en la pestaña **Juego** o ventana Play (WASD en la pestaña **Mapa** solo mueve la cámara del editor). Teclas: tabla **Key** + `input.isKeyDown` en `onUpdate`.",
                paragraphs: new[]
                {
                    "**Qué es @prop y por qué está arriba:** la línea `-- @prop velocidad: float = 220` hace que el Inspector genere un campo editable por instancia en «Variables de script». Así, el mismo script sirve para muchos objetos con distinta velocidad sin tocar el .lua.",
                    "**Qué hace `onUpdate(dt)`:** FUEngine llama a `onUpdate` cada frame en Play. `dt` suele ser el delta del frame; si por versión/host no se pasa, el script usa `time.delta` (cuando existe) como fallback.",
                    "**Paso 1 (entrada → ejes):** calculamos `hx` y `hy` como −1, 0 o +1. Cada eje se obtiene como: (derecha/abajo) − (izquierda/arriba). Si pulsas ambas teclas a la vez, se anulan y el eje queda en 0.",
                    "**Paso 2 (movimiento solo si hay input):** si `hx` y `hy` son 0, no calculamos velocidad ni tocamos posición. Esto ahorra trabajo y evita mover el objeto cuando no hay teclas.",
                    "**Paso 3 (delta y velocidad):** `sp = velocidad * delta` convierte «unidades por segundo» a «unidades por frame». Es lo que hace el movimiento independiente de FPS.",
                    "**Paso 4 (aplicar):** sumamos `hx * sp` y `hy * sp` a `self.x`/`self.y`. Si te mueves en diagonal sin normalizar, la diagonal será más rápida; puedes normalizar si lo necesitas (ver comentario en el código).",
                },
                bullets: new[]
                {
                    "Si el objeto no se mueve: comprueba **scripts.json**, asignación al objeto, **Play** activo y consola Lua sin errores.",
                    "Protagonista con **UseNativeInput** activo: el motor puede mover al jugador antes que Lua; para control 100% script, desactiva esa opción en este objeto.",
                    "Si sale error de `nil`: estás ejecutando fuera de Play o falta alguna tabla global. Este ejemplo hace `if not input then return end` y protege `time.delta`.",
                    "Si quieres que la diagonal no sea más rápida: normaliza `(hx, hy)` cuando ambos no son 0 (o limita a 4 direcciones).",
                },
                subtitle: "Entrada y gameplay",
                luaExampleCode: @"-- @prop velocidad: float = 220

function onUpdate(dt)
    if not input then return end
    
    -- 1. Calculamos ejes directamente (Cero si no hay teclas o se anulan)
    local hx =
        (((input.isKeyDown(Key.D) or input.isKeyDown(Key.Right)) and 1 or 0) -
         ((input.isKeyDown(Key.A) or input.isKeyDown(Key.Left)) and 1 or 0))
               
    local hy =
        (((input.isKeyDown(Key.S) or input.isKeyDown(Key.Down)) and 1 or 0) -
         ((input.isKeyDown(Key.W) or input.isKeyDown(Key.Up)) and 1 or 0))

    -- 2. Aplicamos movimiento solo si hay entrada (ahorra cálculos)
    if hx ~= 0 or hy ~= 0 then
        local delta = dt or (time and time.delta) or 0
        local sp = (velocidad or 220) * delta
        
        -- Normalización simple opcional: evita que corra más en diagonal
        -- Si no te importa que sea más rápido en diagonal, deja hx y hy tal cual.
        --
        -- if hx ~= 0 and hy ~= 0 then
        --     local inv = 1 / math.sqrt(2)
        --     hx = hx * inv
        --     hy = hy * inv
        -- end
        
        self.x = (self.x or 0) + hx * sp
        self.y = (self.y or 0) + hy * sp
    end
end
",
                exampleCategory: "Gameplay",
                suggestedExportFileName: "ejemplo_gameplay_movimiento_wasd_flechas.lua",
                exampleSearchTags: "movimiento WASD flechas teclado jugador objeto self.x input onUpdate gameplay cuatro direcciones script-ex-gameplay-movimiento-wasd-flechas",
                exampleDifficulty: "Básico"),

            new(
                id: "script-ex-efectos-particulas-agua-cascada",
                title: "Efectos: agua / cascada con partículas (Inspector + Lua)",
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
                suggestedExportFileName: "ejemplo_efectos_particulas_agua_cascada.lua",
                exampleSearchTags: "agua cascada partículas particle emitter lava fluido efectos script-ex-efectos-particulas-agua-cascada",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-escenas-zona-trigger-portal",
                title: "Escenas: cambiar de escena al entrar en zona (triggerZones)",
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
                suggestedExportFileName: "ejemplo_escenas_zona_trigger_portal.lua",
                exampleSearchTags: "zona trigger triggerZones onZoneEnter escena loadScene portal mapa script-ex-escenas-zona-trigger-portal",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-gameplay-salud-vida-colision",
                title: "Gameplay: salud y daño simples (@prop y colisión)",
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
                suggestedExportFileName: "ejemplo_gameplay_salud_vida_colision.lua",
                exampleSearchTags: "vida salud daño onCollision health combate script-ex-gameplay-salud-vida-colision",
                exampleDifficulty: "Básico"),

            new(
                id: "script-ex-editor-inspector-atprop-danio-trigger",
                title: "Editor: Inspector, @prop y daño por trigger (lava, pinchos…)",
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
                suggestedExportFileName: "ejemplo_editor_inspector_atprop_danio_trigger.lua",
                exampleSearchTags: "inspector @prop variables daño trigger onTriggerEnter lava script-ex-editor-inspector-atprop-danio-trigger",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-escenas-tecla-o-boton-loadscene",
                title: "Escenas: cambiar con tecla o con botón UI (game.loadScene)",
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
                suggestedExportFileName: "ejemplo_escenas_tecla_o_boton_loadscene.lua",
                exampleSearchTags: "escena tecla botón UI loadScene ui.bind Enter Canvas script-ex-escenas-tecla-o-boton-loadscene",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-ui-hud-mostrar-ocultar-canvas",
                title: "UI: mostrar u ocultar canvas (HUD, menú pausa, ui.show / ui.hide)",
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
                suggestedExportFileName: "ejemplo_ui_hud_mostrar_ocultar_canvas.lua",
                exampleSearchTags: "canvas HUD menú pausa ui.show ui.hide ui.setFocus MapGui script-ex-ui-hud-mostrar-ocultar-canvas",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-mapa-tiles-runtime-o-escena",
                title: "Mapa: editar tiles en Play o cargar otra escena (world.setTile / loadScene)",
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
                suggestedExportFileName: "ejemplo_mapa_tiles_runtime_o_escena.lua",
                exampleSearchTags: "mapa tiles setTile world escena loadScene capa script-ex-mapa-tiles-runtime-o-escena",
                exampleDifficulty: "Avanzado"),

            new(
                id: "script-ex-mapa-romper-tile-collision",
                title: "Mapa: romper bloque al colisionar (world.setTile)",
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
                suggestedExportFileName: "ejemplo_mapa_romper_tile_collision.lua",
                exampleSearchTags: "romper bloque tile setTile onCollision mapa destruir script-ex-mapa-romper-tile-collision",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-ui-cartel-interactuar-tecla-e",
                title: "UI: cartel o mensaje al acercarte y pulsar tecla (E + Canvas)",
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
                suggestedExportFileName: "ejemplo_ui_cartel_interactuar_tecla_e.lua",
                exampleSearchTags: "cartel mensaje UI tecla E interactuar ui.show findByTag script-ex-ui-cartel-interactuar-tecla-e",
                exampleDifficulty: "Básico"),

            new(
                id: "script-ex-gameplay-enemigo-sigue-jugador",
                title: "Gameplay: enemigo que sigue al jugador (persecución simple)",
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
                suggestedExportFileName: "ejemplo_gameplay_enemigo_sigue_jugador.lua",
                exampleSearchTags: "enemigo IA persecución chase findByTag player script-ex-gameplay-enemigo-sigue-jugador",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-ui-boton-hud-pausar-movimiento",
                title: "UI: botón de HUD para pausar o reanudar el movimiento (WASD)",
                paraQue: "Combinar UI (Canvas + Button) con el mismo objeto: un clic alterna si responde a WASD/flechas.",
                porQueImporta: "Patrón base para menús, «pausa de jugador» o confirmar antes de mover.",
                enMotor:
                    "Crea un **UICanvas** en la jerarquía (p. ej. Id **HudMov**) y un **Button** hijo con Id **BtnToggle** (o los nombres que pongas en @prop). En **onStart** llama ui.bind(idCanvas, idBoton, \"click\", function …) y opcionalmente ui.setFocus(idCanvas) para que el canvas reciba clics. Solo el canvas con foco procesa el botón; si tienes varios HUD, gestiona foco con ui.show / ui.hide / ui.setFocus.",
                paragraphs: new[]
                {
                    "Este ejemplo reutiliza la idea de movimiento por ejes; la variable **movimientoActivo** se invierte en el callback del botón.",
                    "Sustituye HudMov / BtnToggle por los Ids reales de tu escena (Inspector de cada nodo UI).",
                },
                bullets: new[]
                {
                    "El texto del botón se edita en el editor (propiedad Content/Text del Button), no con Lua en este flujo básico.",
                    "Si el clic no llega: otro canvas puede tener el foco; ui.setFocus(\"HudMov\") o cierra overlays.",
                },
                subtitle: "UI / Gameplay",
                luaExampleCode: @"-- Canvas ""HudMov"" + Button ""BtnToggle"". Mismo movimiento por ejes que «Gameplay: movimiento con WASD y flechas».
-- @prop idCanvas: string = ""HudMov""
-- @prop idBoton: string = ""BtnToggle""
-- @prop velocidad: float = 220

local movimientoActivo = true

local function axisH()
    local m = 0
    if input.isKeyDown(Key.A) or input.isKeyDown(Key.Left) then m = m - 1 end
    if input.isKeyDown(Key.D) or input.isKeyDown(Key.Right) then m = m + 1 end
    return m
end

local function axisV()
    local m = 0
    if input.isKeyDown(Key.W) or input.isKeyDown(Key.Up) then m = m - 1 end
    if input.isKeyDown(Key.S) or input.isKeyDown(Key.Down) then m = m + 1 end
    return m
end

function onStart()
    local cid = idCanvas or ""HudMov""
    local bid = idBoton or ""BtnToggle""
    if ui and ui.bind then
        ui.bind(cid, bid, ""click"", function()
            movimientoActivo = not movimientoActivo
        end)
        if ui.setFocus then ui.setFocus(cid) end
    end
end

function onUpdate(dt)
    if not movimientoActivo or not input or not time then return end
    local t = time.delta or dt or 0
    if t <= 0 then return end
    local sp = (velocidad or 220) * t
    local hx, hy = axisH(), axisV()
    self.x = (self.x or 0) + hx * sp
    self.y = (self.y or 0) + hy * sp
end
",
                exampleCategory: "UI",
                suggestedExportFileName: "ejemplo_ui_boton_hud_pausar_movimiento.lua",
                exampleSearchTags: "GUI botón HUD ui.bind WASD pausa toggle movimiento script-ex-ui-boton-hud-pausar-movimiento",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-editor-atprop-velocidad-inspector",
                title: "Editor: @prop de velocidad editable en el Inspector",
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
                suggestedExportFileName: "ejemplo_editor_atprop_velocidad_inspector.lua",
                exampleSearchTags: "@prop inspector velocidad speed variables instancia script-ex-editor-atprop-velocidad-inspector",
                exampleDifficulty: "Básico"),

            new(
                id: "script-ex-arquitectura-require-modulo-scripts",
                title: "Arquitectura: require de módulo en Scripts/ (reutilizar código)",
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
                suggestedExportFileName: "ejemplo_arquitectura_require_modulo_scripts.lua",
                exampleSearchTags: "require módulo Scripts package librería script-ex-arquitectura-require-modulo-scripts",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-arquitectura-helpers-tabla-mismo-archivo",
                title: "Arquitectura: helpers en tabla local (sin require, mismo .lua)",
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
                suggestedExportFileName: "ejemplo_arquitectura_helpers_tabla_mismo_archivo.lua",
                exampleSearchTags: "tabla helper local mismo archivo sin require script-ex-arquitectura-helpers-tabla-mismo-archivo",
                exampleDifficulty: "Básico"),

            new(
                id: "script-ex-arquitectura-codigo-lua-externo-adaptar",
                title: "Arquitectura: código Lua de internet (copiar y adaptar al sandbox)",
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
                suggestedExportFileName: "ejemplo_arquitectura_codigo_lua_externo_adaptar.lua",
                exampleSearchTags: "librería externa copiar sandbox io luarocks script-ex-arquitectura-codigo-lua-externo-adaptar",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-gameplay-dia-noche-debug-draw",
                title: "Gameplay: día / noche aproximado con Debug.draw (sin luz global en Lua)",
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
                suggestedExportFileName: "ejemplo_gameplay_dia_noche_debug_draw.lua",
                exampleSearchTags: "día noche Debug.draw oscuridad tiempo script-ex-gameplay-dia-noche-debug-draw",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-gameplay-shake-sin-api-camara",
                title: "Gameplay: sacudida o jitter manual (sin camera.shake en Lua)",
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
                suggestedExportFileName: "ejemplo_gameplay_shake_sin_api_camara.lua",
                exampleSearchTags: "shake sacudida jitter impacto cámara script-ex-gameplay-shake-sin-api-camara",
                exampleDifficulty: "Intermedio"),

            new(
                id: "script-ex-datos-guardado-partida-sandbox-nota",
                title: "Datos: guardado de partida y limitaciones del sandbox (sin file.* genérico)",
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
                suggestedExportFileName: "ejemplo_datos_guardado_partida_sandbox_nota.lua",
                exampleSearchTags: "guardar save persistencia partida io sandbox script-ex-datos-guardado-partida-sandbox-nota",
                exampleDifficulty: "Avanzado"),
        };
    }
}
