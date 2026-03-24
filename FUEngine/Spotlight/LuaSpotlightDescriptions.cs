using System.Collections.Generic;
using FUEngine.Core;

namespace FUEngine.Spotlight;

/// <summary>Textos «para qué sirve» en Spotlight: hooks <see cref="KnownEvents"/> y miembros API <see cref="LuaEditorApiReflection"/>.</summary>
internal static class LuaSpotlightDescriptions
{
    public static bool TryGetHook(string hookName, out string title, out string detail, out string? example)
    {
        title = hookName;
        detail = "";
        example = null;
        switch (hookName)
        {
            case KnownEvents.OnAwake:
                title = "onAwake";
                detail = "Se llama una vez al cargar el script (tras ejecutar el archivo), antes de onStart. Sirve para registrar datos o preparar estado inicial.";
                return true;
            case KnownEvents.OnStart:
                title = "onStart";
                detail = "Una sola vez justo antes del primer onUpdate. Ideal para inicializar lógica que ya necesita el mundo listo.";
                example = "function onStart()\n  -- preparar variables\nend";
                return true;
            case KnownEvents.OnInteract:
                title = "onInteract";
                detail = "Cuando el jugador interactúa con este objeto (puertas, cofres, interruptores). Implementa la acción (abrir, coger objeto…).";
                return true;
            case KnownEvents.OnCollision:
                title = "onCollision";
                detail = "Colisión física con otro cuerpo (enemigo, jugador, pickup). Usa para daño, empuje o recoger ítems.";
                return true;
            case KnownEvents.OnFear:
                title = "onFear";
                detail = "Lógica de miedo o detección (p. ej. animatrónico cerca, mirada). El motor puede invocarlo según reglas de gameplay.";
                return true;
            case KnownEvents.OnSpawn:
                title = "onSpawn";
                detail = "Al aparecer o respawnear la entidad (enemigo, objeto). Configura estado inicial de la instancia en escena.";
                return true;
            case KnownEvents.OnDestroy:
                title = "onDestroy";
                detail = "Antes de destruir el objeto. Limpia referencias, efectos o guarda estado.";
                return true;
            case KnownEvents.OnRepair:
                title = "onRepair";
                detail = "Minijuego o acción de reparación (máquinas, generadores).";
                return true;
            case KnownEvents.OnHack:
                title = "onHack";
                detail = "Hackeo o reprogramación (paneles, animatrónicos).";
                return true;
            case KnownEvents.OnUpdate:
                title = "onUpdate";
                detail = "Cada frame: IA, movimiento, animaciones ligeras, timers. Recibe dt (segundos). Usa self, world, input, time…";
                example = "function onUpdate(dt)\n  -- lógica por frame\nend";
                return true;
            case KnownEvents.OnLateUpdate:
                title = "onLateUpdate";
                detail = "Tras onUpdate de todos los objetos. Útil para cámara o lógica que debe ir después del resto.";
                return true;
            case KnownEvents.OnLayerUpdate:
                title = "onLayerUpdate (script de capa)";
                detail = "Solo en scripts de capa del mapa: cada frame. Tabla Lua «layer» (offset, parallax, opacidad…). No hay self.";
                example = "function onLayerUpdate(dt)\n  layer.offsetX = layer.offsetX + 10 * dt\nend";
                return true;
            case KnownEvents.OnTriggerEnter:
                title = "onTriggerEnter";
                detail = "Entró algo en un trigger (zona sin colisión sólida). Para checkpoints, daño por área, etc.";
                return true;
            case KnownEvents.OnTriggerExit:
                title = "onTriggerExit";
                detail = "Salió algo del trigger.";
                return true;
            case KnownEvents.OnTrigger:
                title = "onTrigger";
                detail = "Trigger genérico (al pasar o activar). Depende de cómo el motor encadene el evento.";
                return true;
            case KnownEvents.OnDayStart:
                title = "onDayStart";
                detail = "Inicio del ciclo día (día/noche, turnos). Puede ser evento de mundo o script global.";
                return true;
            case KnownEvents.OnNightStart:
                title = "onNightStart";
                detail = "Inicio del ciclo noche.";
                return true;
            case KnownEvents.OnPlayerMove:
                title = "onPlayerMove";
                detail = "El jugador se movió. Útil para minimapa, guardado automático o triggers globales.";
                return true;
            case KnownEvents.OnZoneEnter:
                title = "onZoneEnter";
                detail = "El jugador entró en una zona del mapa (área definida en datos o editor).";
                return true;
            case KnownEvents.OnZoneExit:
                title = "onZoneExit";
                detail = "El jugador salió de una zona.";
                return true;
            case KnownEvents.OnChildAdded:
                title = "onChildAdded";
                detail = "Se añadió un hijo en la jerarquía de este objeto.";
                return true;
            case KnownEvents.OnChildRemoved:
                title = "onChildRemoved";
                detail = "Se quitó un hijo de la jerarquía.";
                return true;
            case KnownEvents.OnParentChanged:
                title = "onParentChanged";
                detail = "Cambió el padre de este objeto en la jerarquía.";
                return true;
            default:
                detail = $"Evento «{hookName}»: reservado en KnownEvents. Si es nuevo, documenta su propósito en LuaSpotlightDescriptions.";
                return true;
        }
    }

    /// <summary>Detalle y ejemplo opcional para una clave «tabla.miembro».</summary>
    public static bool TryGetMemberHint(string fullKey, out string detail, out string? example)
    {
        if (MemberHints.TryGetValue(fullKey, out var h))
        {
            detail = h.Detail;
            example = h.Example;
            return true;
        }
        detail = "";
        example = null;
        return false;
    }

    /// <summary>Texto útil si no hay entrada manual (sigue siendo buscable por nombre de API).</summary>
    public static string DefaultMemberDetail(string prefix, string member)
    {
        var table = prefix.TrimEnd('.');
        return $"«{table}.{member}»: miembro de la API Lua del motor. Esta entrada no tiene texto extendido en MemberHints; revisa el manual del editor (Scripts / Lua), el resumen de la tabla «{table}» en Spotlight o el código [LuaVisible].";
    }

    /// <summary>Entradas para quien busca «¿qué es world?» sin escribir un método concreto.</summary>
    public static readonly (string Id, string Title, string Detail)[] GlobalTableGuides =
    {
        ("world", "Tabla global «world»", "Escena en Play: buscar objetos por nombre, instanceId, tag o ruta jerárquica; listar todos; instanciar o destruir prefabs; mover un proxy a (x,y); raycast contra colliders y/o tiles (o ambos); leer/escribir celdas del mapa por capa. Algunos métodos (SetWorldContext, ConfigurePlayTilemap…) son solo para el host del motor."),
        ("self", "Tabla global «self»", "Proxy del GameObject que ejecuta el script: identidad, nombre, etiquetas, posición/rotación/escala, visibilidad, orden de dibujo, sprite y animación, jerarquía (padre, hijos, find), getComponent → proxy component.* e instantiate de prefabs."),
        ("layer", "Tabla global «layer»", "Solo en scripts de capa del mapa: datos de la capa activa (nombre, id, índice, offset, parallax, opacidad). Úsala sobre todo en onLayerUpdate(dt)."),
        ("input", "Tabla global «input»", "Entrada en Play: teclas mantenidas o pulsadas, botones del ratón y posición del puntero. Pasa Key.* o cadenas como \"W\"; Mouse.Left / 0 para el botón izquierdo."),
        ("time", "Tabla global «time»", "Tiempo del juego: delta del frame, tiempo acumulado (time y seconds), número de frame y escala de tiempo."),
        ("audio", "Tabla global «audio»", "Sonido y música por id del manifiesto del proyecto: reproducir SFX/música, parar, ajustar buses (master, music, sfx)."),
        ("physics", "Tabla global «physics»", "Consultas sobre colliders en la escena (raycast en segmento, overlap en círculo). Complementa o diferencia del raycast en world según cómo esté cableado el host."),
        ("ui", "Tabla global «ui»", "Interfaz por canvas: mostrar/ocultar, foco, apilar estado de menús (pushState/popState), obtener elementos y enlazar eventos (click, hover…)."),
        ("game", "Tabla global «game»", "Flujo del juego: cargar otra escena por nombre, salir del Play, números aleatorios con semilla opcional."),
        ("ads", "Tabla global «ads»", "Monetización: intersticial, rewarded, banner, precarga y callbacks; el host implementa o simula."),
        ("Debug", "Tabla global «Debug»", "Dibujos de depuración sobre la vista de juego (líneas, círculos, rejilla) en coordenadas de casillas."),
        ("Key", "Tabla «Key» (constantes de tecla)", "Valores para input: W, A, S, D, flechas, Space, E, Q, F, Enter, Shift, Ctrl, Escape…"),
        ("Mouse", "Tabla «Mouse» (constantes de ratón)", "Botones: Left (0), Right (1) para input.isMouseDown."),
        ("component", "Proxy «component» (tras getComponent)", "No es una global suelta: lo devuelve self:getComponent(\"Tipo\"). invoke e invokeWithResult llaman métodos del componente o del script Lua asociado."),
    };

    /// <summary>Claves «prefix.member» (misma forma que <see cref="LuaEditorApiReflection"/>).</summary>
    public static readonly IReadOnlyDictionary<string, (string Detail, string? Example)> MemberHints =
        new Dictionary<string, (string, string?)>(StringComparer.OrdinalIgnoreCase)
        {
            // --- layer ---
            ["layer.offsetX"] = ("Desplazamiento horizontal de la capa del mapa (píxeles / unidad del descriptor). Sirve para parallax o scroll manual.", "layer.offsetX = layer.offsetX + 10 * dt"),
            ["layer.offsetY"] = ("Desplazamiento vertical de la capa.", "layer.offsetY = layer.offsetY - 5 * dt"),
            ["layer.opacity"] = ("Opacidad 0–100 (como en el inspector). No uses «alpha»; el proxy expone opacity.", "layer.opacity = 80"),
            ["layer.parallaxX"] = ("Factor de parallax horizontal (la capa se mueve más lento que la cámara).", null),
            ["layer.parallaxY"] = ("Factor de parallax vertical.", null),
            ["layer.name"] = ("Nombre visible de la capa en el mapa.", null),
            ["layer.id"] = ("Identificador único (GUID) de la capa.", null),
            ["layer.index"] = ("Índice de la capa en TileMap.Layers.", null),

            // --- world (escena, búsqueda, tiles, raycasts) ---
            ["world.findObject"] = ("Busca el primer objeto en escena por nombre; devuelve self proxy o nil.", "local p = world.findObject(\"Player\")"),
            ["world.findObjectByInstanceId"] = ("Resuelve un objeto por InstanceId (objetos.json); devuelve proxy o nil.", null),
            ["world.getObjectByName"] = ("Alias de findObject: obtiene proxy por nombre.", null),
            ["world.findByTag"] = ("Lista de proxies con esa etiqueta.", "local list = world.findByTag(\"enemy\")"),
            ["world.getObjectByTag"] = ("Alias de findByTag.", null),
            ["world.getObjects"] = ("Todos los objetos de la escena como lista de proxies.", null),
            ["world.getAllObjects"] = ("Alias de getObjects.", null),
            ["world.findByPath"] = ("Busca por ruta jerárquica (ej. \"Boss/Weapon\").", null),
            ["world.spawn"] = ("Instancia un prefab en (x,y) con rotación 0; atajo de instantiate.", null),
            ["world.instantiate"] = ("Crea una instancia del prefab/seed en posición; variant opcional para semillas con sufijo.", "world.instantiate(\"enemy\", 10, 5, 0, \"fast\")"),
            ["world.findNearestByTag"] = ("Objeto con esa etiqueta más cercano a (x,y) en casillas.", null),
            ["world.destroy"] = ("Destruye un objeto pasando su proxy (self u otro).", "world.destroy(enemy)"),
            ["world.setPosition"] = ("Mueve un proxy a (x,y) en casillas; nil seguro.", "world.setPosition(obj, 4, 2)"),
            ["world.getPlayer"] = ("Devuelve proxy del objeto llamado \"Player\" si existe.", null),
            ["world.raycast"] = ("Rayo en casillas contra colliders; ignora opcionalmente un proxy. Devuelve datos de impacto o nil.", "local hit = world.raycast(ox, oy, dx, dy, 10, self)"),
            ["world.raycastTiles"] = ("Raycast solo sobre el tilemap (colisión de celdas en Play).", null),
            ["world.raycastCombined"] = ("El impacto más cercano entre objetos y tiles (útil para disparos).", null),
            ["world.getTile"] = ("ID de catálogo en la celda (capa por nombre); 0 si vacío.", "local id = world.getTile(tx, ty, \"Solid\")"),
            ["world.setTile"] = ("Pone o borra tile por ID de catálogo en la celda (Play + tileset).", null),
            ["world.ResolveGameObjectByInstanceId"] = ("Bajo nivel: devuelve entidad C# por id; en Lua sueles usar findObjectByInstanceId.", null),
            ["world.SetWorldContext"] = ("Uso interno del host (editor/Play). No lo llames desde scripts de juego.", null),
            ["world.SetRaycastImpl"] = ("Inyecta la implementación de raycast; solo el motor.", null),
            ["world.ConfigurePlayTilemap"] = ("Configura mapa en Play y rutas de proyecto; solo el motor.", null),
            ["world.GetPlayTileMap"] = ("Acceso al TileMap activo en Play; avanzado.", null),

            // --- self (GameObject) ---
            ["self.id"] = ("Identificador de instancia del objeto (objetos.json).", null),
            ["self.name"] = ("Nombre visible del objeto en la jerarquía.", "self.name = \"Coin\""),
            ["self.tag"] = ("Primera etiqueta (compatibilidad); mejor usa tags/hasTag.", null),
            ["self.tags"] = ("Lista de etiquetas del objeto.", null),
            ["self.hasTag"] = ("Comprueba si tiene una etiqueta (ignora mayúsculas).", "if self:hasTag(\"pickup\") then … end"),
            ["self.x"] = ("Posición X en casillas (mundo).", "self.x = self.x + speed * dt"),
            ["self.y"] = ("Posición Y en casillas.", null),
            ["self.rotation"] = ("Rotación en grados.", null),
            ["self.scale"] = ("Escala uniforme (media de X/Y).", null),
            ["self.visible"] = ("Si se dibuja el objeto.", null),
            ["self.active"] = ("Activo en runtime; false desactiva lógica y puede destruir.", null),
            ["self.renderOrder"] = ("Orden de dibujado dentro de la capa (mayor = delante).", null),
            ["self.destroy"] = ("Marca destrucción vía mundo o cola del juego.", "self.destroy()"),
            ["self.move"] = ("Mueve a coordenadas absolutas en casillas.", "self.move(nx, ny)"),
            ["self.rotate"] = ("Suma ángulo en grados.", null),
            ["self.playAnimation"] = ("Reproduce clip de animación por nombre (si el host lo soporta).", null),
            ["self.stopAnimation"] = ("Detiene animación actual.", null),
            ["self.setSpriteTexture"] = ("Asigna textura del sprite (ruta relativa al proyecto).", null),
            ["self.addSpriteFrame"] = ("Añade recorte al sprite sheet (píxeles en textura).", null),
            ["self.clearSpriteFrames"] = ("Vacía frames del sprite.", null),
            ["self.spriteFrame"] = ("Índice del frame actual (animación por frames).", null),
            ["self.setSpriteAnimationFps"] = ("Velocidad de animación automática (fps); 0 = solo manual.", null),
            ["self.setSpriteSortOffset"] = ("Orden fino dentro de la misma capa de render.", null),
            ["self.setSpriteDisplaySize"] = ("Tamaño de dibujado en casillas (ancho, alto).", null),
            ["self.setSpriteTint"] = ("Tinte multiplicador RGB del sprite (0–1+).", null),
            ["self.getComponent"] = ("Obtiene ComponentProxy por nombre de tipo (Health, ScriptComponent…).", "local h = self:getComponent(\"Health\")"),
            ["self.addComponent"] = ("Intenta añadir componente por nombre; puede fallar sin factory.", null),
            ["self.removeComponent"] = ("Quita un componente por nombre de tipo.", null),
            ["self.find"] = ("Hijo directo por nombre; devuelve proxy o nil.", null),
            ["self.findInHierarchy"] = ("Busca por ruta (\"Enemy/Arm\").", null),
            ["self.setParent"] = ("Cambia el padre en la jerarquía (otro proxy o nil).", null),
            ["self.getParent"] = ("Proxy del padre o nil si es raíz.", null),
            ["self.getChildren"] = ("Lista de proxies hijos.", null),
            ["self.instantiate"] = ("Crea prefab como hijo del mundo vía world (misma firma que world.instantiate).", null),

            // --- input ---
            ["input.isKeyDown"] = ("¿Tecla mantenida pulsada? Pasa Key.W o cadena \"W\".", "if input.isKeyDown(Key.W) then … end"),
            ["input.isKeyPressed"] = ("¿Se pulsó en este frame? (según implementación del host).", null),
            ["input.isMouseDown"] = ("¿Botón del ratón abajo? 0 o Mouse.Left.", null),
            ["input.mouseX"] = ("Posición X del puntero en coordenadas de juego.", null),
            ["input.mouseY"] = ("Posición Y del puntero.", null),

            // --- time ---
            ["time.delta"] = ("Duración del último frame en segundos (para movimiento con dt).", "self.x = self.x + speed * time.delta"),
            ["time.time"] = ("Tiempo de simulación acumulado (segundos).", null),
            ["time.seconds"] = ("Alias de time: segundos totales.", null),
            ["time.frame"] = ("Índice de frame (entero).", null),
            ["time.scale"] = ("Escala de tiempo (1 = normal; 0 = pausa lógica si el host lo aplica).", null),

            // --- audio ---
            ["audio.play"] = ("Reproduce sonido por id de manifiesto (SFX).", "audio.play(\"sfx/jump\")"),
            ["audio.playMusic"] = ("Música por id; sobrecarga con loop opcional.", null),
            ["audio.playSfx"] = ("Alias de play para efectos.", null),
            ["audio.stopMusic"] = ("Para música con fundido opcional (segundos).", null),
            ["audio.setVolume"] = ("Volumen por bus: master, music o sfx (0..1).", null),
            ["audio.stop"] = ("Para un sonido por id.", null),
            ["audio.stopAll"] = ("Para todos los sonidos activos.", null),
            ["audio.setMasterVolume"] = ("Volumen maestro 0..1.", null),

            // --- physics ---
            ["physics.raycast"] = ("Segmento entre dos puntos en casillas contra colliders sólidos; nil si no hay golpe.", null),
            ["physics.overlapCircle"] = ("Círculo en casillas; devuelve lista de proxies que intersectan (incluye triggers).", null),

            // --- ui ---
            ["ui.SetBackend"] = ("Solo host: conecta backend de UI; no uses desde scripts de contenido.", null),
            ["ui.show"] = ("Muestra un canvas de UI por id.", "ui.show(\"HUD_Main\")"),
            ["ui.hide"] = ("Oculta un canvas.", null),
            ["ui.setFocus"] = ("Canvas que recibe input (menús, prioridad).", null),
            ["ui.pushState"] = ("Guarda estado visible/focus para menús anidados.", null),
            ["ui.popState"] = ("Restaura el estado guardado con pushState.", null),
            ["ui.get"] = ("Obtiene elemento UI por canvas y id (binding desde Lua).", null),
            ["ui.bind"] = ("Enlaza evento (click, hover…) a función Lua/callback.", null),

            // --- game ---
            ["game.setRandomSeed"] = ("Fija semilla del RNG para reproducibilidad.", null),
            ["game.randomInt"] = ("Entero aleatorio [min, max).", "local r = game.randomInt(0, 10)"),
            ["game.randomDouble"] = ("Real aleatorio [0, 1).", null),
            ["game.loadScene"] = ("Pide cargar otra escena por nombre (el host debe tener OnLoadScene).", "game.loadScene(\"Level2\")"),
            ["game.quit"] = ("Sale del Play / juego si el host implementa OnQuit.", null),
            ["game.OnLoadScene"] = ("Callback que asigna el motor: no lo reasignes desde Lua salvo avanzado.", null),
            ["game.OnQuit"] = ("Callback de salida; uso interno del host.", null),

            // --- Debug (depuración en ventana de juego) ---
            ["Debug.drawLine"] = ("Dibuja línea en overlay de depuración (coordenadas de casillas como el mundo).", "Debug.drawLine(x1,y1,x2,y2)"),
            ["Debug.drawCircle"] = ("Círculo de depuración (centro + radio en casillas).", null),
            ["Debug.drawGrid"] = ("Rejilla centrada en casillas (útil para alinear).", null),
            ["Debug.GetLastFrameSnapshot"] = ("Avanzado: instantánea de comandos de dibujo; no suele hacer falta en Lua.", null),

            // --- ads ---
            ["ads.RunOnMainThread"] = ("Cola de ejecución en hilo UI; lo rellena el host.", null),
            ["ads.showInterstitial"] = ("Muestra anuncio intersticial; callback opcional al cerrar.", null),
            ["ads.showRewarded"] = ("Anuncio con recompensa; callback con true/false.", null),
            ["ads.showBanner"] = ("Banner en placement opcional.", null),
            ["ads.loadInterstitial"] = ("Precarga intersticial.", null),
            ["ads.loadRewarded"] = ("Precarga rewarded.", null),
            ["ads.isRewardedReady"] = ("¿Hay rewarded listo para mostrar?", null),
            ["ads.setTestMode"] = ("Modo test de red de anuncios.", null),
            ["ads.setTagForChildDirectedTreatment"] = ("Cumplimiento COPPA / menores.", null),

            // --- component (getComponent) ---
            ["component.invoke"] = ("Llama un método del componente o del script Lua (nombre en string).", "local h = self:getComponent(\"Health\")\nh:invoke(\"takeDamage\", 10)"),
            ["component.invokeWithResult"] = ("Como invoke pero devuelve el primer valor si el script lo retorna.", null),
            ["component.typeName"] = ("Nombre del tipo C# del componente.", null),

            // --- Key.* (cadenas para input.isKeyDown) ---
            ["Key.W"] = ("Cadena de tecla «W» (movimiento / binding).", "input.isKeyDown(Key.W)"),
            ["Key.A"] = ("Tecla A.", null),
            ["Key.S"] = ("Tecla S.", null),
            ["Key.D"] = ("Tecla D.", null),
            ["Key.Left"] = ("Flecha izquierda.", null),
            ["Key.Right"] = ("Flecha derecha.", null),
            ["Key.Up"] = ("Flecha arriba.", null),
            ["Key.Down"] = ("Flecha abajo.", null),
            ["Key.Space"] = ("Barra espaciadora (cadena SPACE).", null),
            ["Key.E"] = ("Tecla E.", null),
            ["Key.Q"] = ("Tecla Q.", null),
            ["Key.F"] = ("Tecla F.", null),
            ["Key.Enter"] = ("Intro.", null),
            ["Key.Shift"] = ("Mayús.", null),
            ["Key.Ctrl"] = ("Control.", null),
            ["Key.Escape"] = ("Escape.", null),

            // --- Mouse.* ---
            ["Mouse.Left"] = ("Botón izquierdo (0).", "input.isMouseDown(Mouse.Left)"),
            ["Mouse.Right"] = ("Botón derecho (1).", null),
        };
}
