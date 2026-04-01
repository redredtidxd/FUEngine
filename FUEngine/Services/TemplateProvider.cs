using System.Collections.Generic;
using System.Text.Json;
using FUEngine.Editor;

namespace FUEngine;

public static class TemplateProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public const string CategorySideScroller = "Side-scroller";
    public const string CategoryTopDown = "Top-down";
    public const string CategoryPuzzle = "Puzzle";
    public const string CategoryCasual = "Casual";
    public const string CategoryHorror = "Horror";
    public const string CategoryRPG = "RPG";
    public const string CategoryShooter = "Shooter";
    public const string CategoryBuilder = "Builder";

    public static List<string> GetAllCategories()
    {
        return new List<string> { "Todas", CategorySideScroller, CategoryTopDown, CategoryPuzzle, CategoryCasual, CategoryHorror, CategoryRPG, CategoryShooter, CategoryBuilder };
    }

    public static List<TemplateItem> GetAllTemplates()
    {
        return new List<TemplateItem>
        {
            new() { Id = 1, Name = "Plataforma clásica 2D (Side-scroller)", Description = "Estilo Mario / Celeste. Plataformas, enemigos y pickups en un mundo lateral. Colisión por tile, parallax 2 capas.", Category = CategorySideScroller },
            new() { Id = 2, Name = "Rogue-like top-down", Description = "Estilo Binding of Isaac. Chunks, suelo/paredes/puertas, cofres, enemigos, loot. Sprite stacking y paleta dinámica.", Category = CategoryTopDown },
            new() { Id = 3, Name = "Puzzle / lógica", Description = "Sokoban, bloques y switches. Tiles movibles e interactivos, palancas, puertas, triggers. Highlight de tile interactivo.", Category = CategoryPuzzle },
            new() { Id = 4, Name = "Metroidvania top-down", Description = "Mundo interconectado, llaves, puertas, cofres. Habilidades desbloqueables, capa de profundidad.", Category = CategoryTopDown },
            new() { Id = 5, Name = "Horror / Survival Pixel", Description = "Exploración, linterna, animatrónicos. Zonas oscuras, interruptores, eventos de miedo e iluminación.", Category = CategoryHorror },
            new() { Id = 6, Name = "RPG / combate por turnos", Description = "Estilo Pokémon o Fire Emblem. Grid, personajes, enemigos, loot. Turnos, ataque/heal, stacking por Y.", Category = CategoryRPG },
            new() { Id = 7, Name = "Shooter / Twin-stick top-down", Description = "Disparos, enemigos, cobertura, spawn points. Colisiones, daño, IA patrulla. Iluminación dinámica.", Category = CategoryShooter },
            new() { Id = 8, Name = "Endless Runner", Description = "Lateral, generación infinita de chunks. Obstáculos, collectibles, triggers y spawn aleatorio. Parallax multi-capa.", Category = CategorySideScroller },
            new() { Id = 9, Name = "Builder / Sandbox", Description = "Tipo Terraria. Tiles destructibles e interactivos, bloques, puertas. Eventos de destrucción y paleta dinámica.", Category = CategoryBuilder },
            new() { Id = 10, Name = "Mini Juegos / Casual", Description = "Puzzles, matching, arcade. Grid de objetos, fichas, botones, temporizadores. Animaciones simples.", Category = CategoryCasual }
        };
    }

    /// <summary>Módulos de script reutilizables: se fusionan con los de la plantilla al crear el proyecto (sin duplicar por id).</summary>
    public static List<ScriptItemDto> GetCommonScriptModules()
    {
        return new List<ScriptItemDto>
        {
            new() { Id = "onCollision_enemy", Nombre = "Colisión enemigo (módulo)", Eventos = new List<string> { "onCollision" } },
            new() { Id = "onCollision_pickup", Nombre = "Colisión pickup (módulo)", Eventos = new List<string> { "onCollision" } },
            new() { Id = "onCollision_trap", Nombre = "Colisión trampa (módulo)", Eventos = new List<string> { "onCollision" } },
            new() { Id = "onInteract", Nombre = "Interacción (módulo)", Eventos = new List<string> { "onInteract" } },
            new() { Id = "onTrigger", Nombre = "Trigger (módulo)", Eventos = new List<string> { "onInteract", "onCollision" } }
        };
    }

    /// <summary>Fusiona scripts de plantilla con módulos comunes; la plantilla tiene prioridad si repite id.</summary>
    public static ScriptsDto MergeWithCommonModules(ScriptsDto templateScripts)
    {
        var ids = new HashSet<string>(templateScripts.Scripts.Select(s => s.Id));
        var list = new List<ScriptItemDto>(templateScripts.Scripts);
        foreach (var m in GetCommonScriptModules())
            if (!ids.Contains(m.Id)) { list.Add(m); ids.Add(m.Id); }
        return new ScriptsDto { Scripts = list };
    }

    public static TemplateData GetTemplateData(int templateId)
    {
        return templateId switch
        {
            1 => CreatePlataforma2D(),
            2 => CreateRoguelike(),
            3 => CreatePuzzle(),
            4 => CreateMetroidvania(),
            5 => CreateHorror(),
            6 => CreateRPG(),
            7 => CreateShooter(),
            8 => CreateEndlessRunner(),
            9 => CreateBuilder(),
            10 => CreateMinijuegos(),
            _ => CreatePlataforma2D()
        };
    }

    private static TemplateData CreatePlataforma2D()
    {
        var project = new ProjectDto
        {
            Nombre = "Plataforma 2D",
            Descripcion = "Juego lateral estilo Mario/Celeste. Plataformas, enemigos, pickups. Parallax 2 capas (0.5x, 0.8x).",
            TileSize = 16,
            MapWidth = 80,
            MapHeight = 18,
            Infinite = false,
            MapPath = "mapa.json",
            ObjectsPath = "objetos.json"
        };
        var map = new MapDto { ChunkSize = 16 };
        var chunk0 = new ChunkDto { Cx = 0, Cy = 0 };
        for (int x = 0; x < 16; x++)
            for (int y = 12; y < 16; y++)
                chunk0.Tiles.Add(new TileDto { X = x, Y = y, TipoTile = 0, Colision = true, Interactivo = false });
        for (int x = 2; x < 6; x++) chunk0.Tiles.Add(new TileDto { X = x, Y = 8, TipoTile = 1, Colision = true, Interactivo = false });
        for (int x = 10; x < 14; x++) chunk0.Tiles.Add(new TileDto { X = x, Y = 6, TipoTile = 1, Colision = true, Interactivo = false });
        map.Chunks.Add(chunk0);
        var chunk1 = new ChunkDto { Cx = 1, Cy = 0 };
        for (int x = 0; x < 16; x++)
            for (int y = 12; y < 16; y++)
                chunk1.Tiles.Add(new TileDto { X = x, Y = y, TipoTile = 0, Colision = true, Interactivo = false });
        chunk1.Tiles.Add(new TileDto { X = 4, Y = 9, TipoTile = 1, Colision = true, Interactivo = false });
        map.Chunks.Add(chunk1);

        var objects = new ObjectsDto
        {
            Definitions = new List<ObjectDefinitionDto>
            {
                new() { Id = "enemigo", Nombre = "Enemigo", Colision = true, Interactivo = false, Destructible = true, Width = 1, Height = 1, ScriptId = "onCollision_enemy" },
                new() { Id = "pickup", Nombre = "Pickup", Colision = false, Interactivo = true, Destructible = true, Width = 1, Height = 1, ScriptId = "onCollision_pickup" },
                new() { Id = "trampa", Nombre = "Trampa", Colision = true, Interactivo = false, Destructible = false, Width = 1, Height = 1, ScriptId = "onCollision_trap" }
            },
            Instances = new List<ObjectInstanceDto>
            {
                new() { InstanceId = "e1", DefinitionId = "enemigo", X = 5, Y = 11, Nombre = "Enemigo 1" },
                new() { InstanceId = "p1", DefinitionId = "pickup", X = 12, Y = 5, Nombre = "Moneda" }
            }
        };
        var scripts = new ScriptsDto { Scripts = new List<ScriptItemDto> { new() { Id = "onCollision_enemy", Nombre = "Colisión enemigo", Eventos = new List<string> { "onCollision" } }, new() { Id = "onCollision_pickup", Nombre = "Colisión pickup", Eventos = new List<string> { "onCollision" } }, new() { Id = "onCollision_trap", Nombre = "Colisión trampa", Eventos = new List<string> { "onCollision" } } } };
        var animations = new AnimationsDto { Animations = new List<AnimationItemDto> { new() { Id = "personaje_idle", Nombre = "Personaje idle", Fps = 8, Frames = new List<string> { "placeholder_idle_0", "placeholder_idle_1" } }, new() { Id = "personaje_run", Nombre = "Personaje run", Fps = 12, Frames = new List<string> { "placeholder_run_0", "placeholder_run_1", "placeholder_run_2" } } } };
        return new TemplateData(project, map, objects, scripts, animations);
    }

    private static TemplateData CreateRoguelike()
    {
        var project = new ProjectDto { Nombre = "Rogue-like", Descripcion = "Top-down tipo Binding of Isaac. Chunks, cofres, enemigos, loot. Sprite stacking y paleta dinámica por zona.", TileSize = 16, MapWidth = 64, MapHeight = 64, Infinite = true, MapPath = "mapa.json", ObjectsPath = "objetos.json" };
        var map = new MapDto { ChunkSize = 16 };
        var c = new ChunkDto { Cx = 0, Cy = 0 };
        for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) c.Tiles.Add(new TileDto { X = x, Y = y, TipoTile = 0, Colision = false, Interactivo = false });
        for (int x = 0; x < 16; x++) { c.Tiles.Add(new TileDto { X = x, Y = 0, TipoTile = 1, Colision = true, Interactivo = false }); c.Tiles.Add(new TileDto { X = x, Y = 15, TipoTile = 1, Colision = true, Interactivo = false }); }
        for (int y = 0; y < 16; y++) { c.Tiles.Add(new TileDto { X = 0, Y = y, TipoTile = 1, Colision = true, Interactivo = false }); c.Tiles.Add(new TileDto { X = 15, Y = y, TipoTile = 1, Colision = true, Interactivo = false }); }
        c.Tiles.Add(new TileDto { X = 7, Y = 0, TipoTile = 3, Colision = false, Interactivo = true, ScriptId = "puerta" });
        map.Chunks.Add(c);
        var objects = new ObjectsDto
        {
            Definitions = new List<ObjectDefinitionDto> { new() { Id = "cofre", Nombre = "Cofre", Colision = true, Interactivo = true, Destructible = false, ScriptId = "onInteract_chest" }, new() { Id = "enemigo", Nombre = "Enemigo", Colision = true, Interactivo = false, Destructible = true, ScriptId = "onCollision" }, new() { Id = "trampa", Nombre = "Trampa", Colision = false, Interactivo = false, Destructible = true, ScriptId = "onTrigger" } },
            Instances = new List<ObjectInstanceDto> { new() { InstanceId = "c1", DefinitionId = "cofre", X = 7, Y = 7, Nombre = "Cofre" } }
        };
        var scripts = new ScriptsDto { Scripts = new List<ScriptItemDto> { new() { Id = "onInteract_chest", Nombre = "Abrir cofre", Eventos = new List<string> { "onInteract" } }, new() { Id = "puerta", Nombre = "Puerta sala", Eventos = new List<string> { "onInteract" } } } };
        return new TemplateData(project, map, objects, scripts, new AnimationsDto());
    }

    private static TemplateData CreatePuzzle()
    {
        var project = new ProjectDto { Nombre = "Puzzle", Descripcion = "Sokoban, bloques y switches. Tiles movibles e interactivos, palancas, puertas, triggers.", TileSize = 16, MapWidth = 32, MapHeight = 24, Infinite = false, MapPath = "mapa.json", ObjectsPath = "objetos.json" };
        var map = new MapDto { ChunkSize = 16 };
        var c = new ChunkDto { Cx = 0, Cy = 0 };
        for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) c.Tiles.Add(new TileDto { X = x, Y = y, TipoTile = 0, Colision = false, Interactivo = false });
        for (int x = 2; x < 14; x++) { c.Tiles.Add(new TileDto { X = x, Y = 2, TipoTile = 1, Colision = true, Interactivo = false }); c.Tiles.Add(new TileDto { X = x, Y = 13, TipoTile = 1, Colision = true, Interactivo = false }); }
        for (int y = 2; y < 14; y++) { c.Tiles.Add(new TileDto { X = 2, Y = y, TipoTile = 1, Colision = true, Interactivo = false }); c.Tiles.Add(new TileDto { X = 13, Y = y, TipoTile = 1, Colision = true, Interactivo = false }); }
        map.Chunks.Add(c);
        var objects = new ObjectsDto
        {
            Definitions = new List<ObjectDefinitionDto> { new() { Id = "palanca", Nombre = "Palanca", Colision = true, Interactivo = true, Destructible = false, ScriptId = "onActivate" }, new() { Id = "puerta", Nombre = "Puerta", Colision = true, Interactivo = false, Destructible = false, ScriptId = "onChangeState" }, new() { Id = "bloque", Nombre = "Bloque", Colision = true, Interactivo = false, Destructible = false }, new() { Id = "trigger", Nombre = "Trigger", Colision = false, Interactivo = true, Destructible = false, ScriptId = "onTrigger" } },
            Instances = new List<ObjectInstanceDto> { new() { InstanceId = "p1", DefinitionId = "palanca", X = 4, Y = 4, Nombre = "Palanca 1" }, new() { InstanceId = "d1", DefinitionId = "puerta", X = 10, Y = 7, Nombre = "Puerta" } }
        };
        var scripts = new ScriptsDto { Scripts = new List<ScriptItemDto> { new() { Id = "onActivate", Nombre = "Activar", Eventos = new List<string> { "onInteract" } }, new() { Id = "onChangeState", Nombre = "Cambiar estado", Eventos = new List<string> { "onTrigger" } }, new() { Id = "onTrigger", Nombre = "Trigger", Eventos = new List<string> { "onInteract" } } } };
        return new TemplateData(project, map, objects, scripts, new AnimationsDto());
    }

    private static TemplateData CreateMetroidvania()
    {
        var project = new ProjectDto { Nombre = "Metroidvania", Descripcion = "Mundo interconectado, llaves, puertas, cofres. Habilidades desbloqueables, capa de profundidad.", TileSize = 16, MapWidth = 80, MapHeight = 48, Infinite = false, MapPath = "mapa.json", ObjectsPath = "objetos.json" };
        var map = new MapDto { ChunkSize = 16 };
        for (int cx = 0; cx < 5; cx++) for (int cy = 0; cy < 3; cy++)
            {
                var ch = new ChunkDto { Cx = cx, Cy = cy };
                for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) ch.Tiles.Add(new TileDto { X = x, Y = y, TipoTile = 0, Colision = false, Interactivo = false });
                if (cy == 0) for (int x = 0; x < 16; x++) ch.Tiles.Add(new TileDto { X = x, Y = 15, TipoTile = 1, Colision = true, Interactivo = false });
                if (cy == 2) for (int x = 0; x < 16; x++) ch.Tiles.Add(new TileDto { X = x, Y = 0, TipoTile = 1, Colision = true, Interactivo = false });
                if (cx == 0) for (int y = 0; y < 16; y++) ch.Tiles.Add(new TileDto { X = 15, Y = y, TipoTile = 1, Colision = true, Interactivo = false });
                if (cx == 4) for (int y = 0; y < 16; y++) ch.Tiles.Add(new TileDto { X = 0, Y = y, TipoTile = 1, Colision = true, Interactivo = false });
                map.Chunks.Add(ch);
            }
        var objects = new ObjectsDto { Definitions = new List<ObjectDefinitionDto> { new() { Id = "llave", Nombre = "Llave", Colision = false, Interactivo = true, Destructible = true, ScriptId = "onUnlock" }, new() { Id = "puerta", Nombre = "Puerta", Colision = true, Interactivo = true, Destructible = false, ScriptId = "onRequireAbility" }, new() { Id = "cofre", Nombre = "Cofre", Colision = true, Interactivo = true, Destructible = false, ScriptId = "onInteract" }, new() { Id = "trampa", Nombre = "Trampa", Colision = false, Interactivo = false, Destructible = true, ScriptId = "onTrigger" } }, Instances = new List<ObjectInstanceDto> { new() { InstanceId = "ll1", DefinitionId = "llave", X = 20, Y = 20, Nombre = "Llave" }, new() { InstanceId = "pd1", DefinitionId = "puerta", X = 35, Y = 25, Nombre = "Puerta" } } };
        var scripts = new ScriptsDto { Scripts = new List<ScriptItemDto> { new() { Id = "onUnlock", Nombre = "Desbloquear", Eventos = new List<string> { "onInteract" } }, new() { Id = "onRequireAbility", Nombre = "Requerir habilidad", Eventos = new List<string> { "onInteract" } } } };
        return new TemplateData(project, map, objects, scripts, new AnimationsDto());
    }

    private static TemplateData CreateHorror()
    {
        var project = new ProjectDto { Nombre = "Horror Survival", Descripcion = "Exploración, linterna, animatrónicos. Zonas oscuras, interruptores, eventos de miedo e iluminación pixel-perfect.", TileSize = 16, MapWidth = 48, MapHeight = 32, Infinite = false, MapPath = "mapa.json", ObjectsPath = "objetos.json" };
        var map = new MapDto { ChunkSize = 16 };
        for (int cx = 0; cx < 3; cx++) for (int cy = 0; cy < 2; cy++)
            {
                var ch = new ChunkDto { Cx = cx, Cy = cy };
                for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) ch.Tiles.Add(new TileDto { X = x, Y = y, TipoTile = 0, Colision = false, Interactivo = false });
                for (int x = 0; x < 16; x++) { ch.Tiles.Add(new TileDto { X = x, Y = 0, TipoTile = 1, Colision = true, Interactivo = false }); ch.Tiles.Add(new TileDto { X = x, Y = 15, TipoTile = 1, Colision = true, Interactivo = false }); }
                for (int y = 0; y < 16; y++) { ch.Tiles.Add(new TileDto { X = 0, Y = y, TipoTile = 1, Colision = true, Interactivo = false }); ch.Tiles.Add(new TileDto { X = 15, Y = y, TipoTile = 1, Colision = true, Interactivo = false }); }
                map.Chunks.Add(ch);
            }
        var objects = new ObjectsDto { Definitions = new List<ObjectDefinitionDto> { new() { Id = "puerta", Nombre = "Puerta", Colision = true, Interactivo = true, ScriptId = "onInteract" }, new() { Id = "armario", Nombre = "Armario", Colision = true, Interactivo = true, ScriptId = "onInteract" }, new() { Id = "interruptor", Nombre = "Interruptor", Colision = false, Interactivo = true, ScriptId = "onPresence" }, new() { Id = "animatronico", Nombre = "Animatrónico", Colision = true, Interactivo = false, ScriptId = "onFear" } }, Instances = new List<ObjectInstanceDto> { new() { InstanceId = "i1", DefinitionId = "interruptor", X = 8, Y = 8, Nombre = "Luz" } } };
        var scripts = new ScriptsDto { Scripts = new List<ScriptItemDto> { new() { Id = "onFear", Nombre = "Evento miedo", Eventos = new List<string> { "onCollision", "onUpdate" } }, new() { Id = "onPresence", Nombre = "Trigger presencia", Eventos = new List<string> { "onInteract" } } } };
        return new TemplateData(project, map, objects, scripts, new AnimationsDto());
    }

    private static TemplateData CreateRPG()
    {
        var project = new ProjectDto { Nombre = "RPG por turnos", Descripcion = "Estilo Pokémon o Fire Emblem. Grid, personajes, enemigos, loot. Turnos, ataque/heal, stacking por Y.", TileSize = 16, MapWidth = 40, MapHeight = 30, Infinite = false, MapPath = "mapa.json", ObjectsPath = "objetos.json" };
        var map = new MapDto { ChunkSize = 16 };
        var c = new ChunkDto { Cx = 0, Cy = 0 };
        for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) c.Tiles.Add(new TileDto { X = x, Y = y, TipoTile = 0, Colision = false, Interactivo = false });
        c.Tiles.Add(new TileDto { X = 5, Y = 5, TipoTile = 1, Colision = true, Interactivo = false });
        c.Tiles.Add(new TileDto { X = 10, Y = 10, TipoTile = 1, Colision = true, Interactivo = false });
        map.Chunks.Add(c);
        var objects = new ObjectsDto { Definitions = new List<ObjectDefinitionDto> { new() { Id = "personaje", Nombre = "Personaje", Colision = true, Interactivo = false, Destructible = false, ScriptId = "onTurn" }, new() { Id = "enemigo", Nombre = "Enemigo", Colision = true, Interactivo = false, Destructible = true, ScriptId = "onAttack" }, new() { Id = "loot", Nombre = "Loot", Colision = false, Interactivo = true, Destructible = true, ScriptId = "onInteract" }, new() { Id = "obstaculo", Nombre = "Obstáculo", Colision = true, Interactivo = false, Destructible = false } }, Instances = new List<ObjectInstanceDto> { new() { InstanceId = "pc1", DefinitionId = "personaje", X = 3, Y = 3, Nombre = "Héroe" }, new() { InstanceId = "en1", DefinitionId = "enemigo", X = 8, Y = 8, Nombre = "Slime" } } };
        var scripts = new ScriptsDto { Scripts = new List<ScriptItemDto> { new() { Id = "onTurn", Nombre = "Turno", Eventos = new List<string> { "onUpdate" } }, new() { Id = "onAttack", Nombre = "Ataque", Eventos = new List<string> { "onInteract" } }, new() { Id = "onHeal", Nombre = "Curar", Eventos = new List<string> { "onInteract" } } } };
        return new TemplateData(project, map, objects, scripts, new AnimationsDto());
    }

    private static TemplateData CreateShooter()
    {
        var project = new ProjectDto { Nombre = "Shooter Twin-stick", Descripcion = "Disparos, enemigos, cobertura, spawn points. Colisiones, daño, IA patrulla. Iluminación dinámica.", TileSize = 16, MapWidth = 64, MapHeight = 48, Infinite = false, MapPath = "mapa.json", ObjectsPath = "objetos.json" };
        var map = new MapDto { ChunkSize = 16 };
        for (int cx = 0; cx < 4; cx++) for (int cy = 0; cy < 3; cy++)
            {
                var ch = new ChunkDto { Cx = cx, Cy = cy };
                for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) ch.Tiles.Add(new TileDto { X = x, Y = y, TipoTile = 0, Colision = false, Interactivo = false });
                if (cx == 0 || cx == 3) for (int y = 0; y < 16; y++) ch.Tiles.Add(new TileDto { X = cx == 0 ? 15 : 0, Y = y, TipoTile = 1, Colision = true, Interactivo = false });
                if (cy == 0 || cy == 2) for (int x = 0; x < 16; x++) ch.Tiles.Add(new TileDto { X = x, Y = cy == 0 ? 15 : 0, TipoTile = 1, Colision = true, Interactivo = false });
                if (cx == 1 && cy == 1) { ch.Tiles.Add(new TileDto { X = 4, Y = 4, TipoTile = 1, Colision = true, Interactivo = false }); ch.Tiles.Add(new TileDto { X = 10, Y = 10, TipoTile = 1, Colision = true, Interactivo = false }); }
                map.Chunks.Add(ch);
            }
        var objects = new ObjectsDto { Definitions = new List<ObjectDefinitionDto> { new() { Id = "enemigo", Nombre = "Enemigo", Colision = true, Interactivo = false, Destructible = true, ScriptId = "onPatrol" }, new() { Id = "bala", Nombre = "Bala", Colision = true, Interactivo = false, Destructible = true, ScriptId = "onCollision" }, new() { Id = "pickup", Nombre = "Pickup", Colision = false, Interactivo = true, Destructible = true, ScriptId = "onCollision" }, new() { Id = "cobertura", Nombre = "Cobertura", Colision = true, Interactivo = false, Destructible = false } }, Instances = new List<ObjectInstanceDto> { new() { InstanceId = "e1", DefinitionId = "enemigo", X = 20, Y = 20, Nombre = "Enemigo" }, new() { InstanceId = "c1", DefinitionId = "cobertura", X = 25, Y = 15, Nombre = "Barricada" } } };
        var scripts = new ScriptsDto { Scripts = new List<ScriptItemDto> { new() { Id = "onCollision", Nombre = "Colisión", Eventos = new List<string> { "onCollision" } }, new() { Id = "onDamage", Nombre = "Daño", Eventos = new List<string> { "onCollision" } }, new() { Id = "onPatrol", Nombre = "Patrulla IA", Eventos = new List<string> { "onUpdate" } } } };
        return new TemplateData(project, map, objects, scripts, new AnimationsDto());
    }

    private static TemplateData CreateEndlessRunner()
    {
        var project = new ProjectDto { Nombre = "Endless Runner", Descripcion = "Lateral, generación infinita de chunks. Obstáculos, collectibles, triggers y spawn aleatorio. Parallax multi-capa.", TileSize = 16, MapWidth = 64, MapHeight = 16, Infinite = true, MapPath = "mapa.json", ObjectsPath = "objetos.json" };
        var map = new MapDto { ChunkSize = 16 };
        for (int cx = 0; cx < 4; cx++)
        {
            var ch = new ChunkDto { Cx = cx, Cy = 0 };
            for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) ch.Tiles.Add(new TileDto { X = x, Y = y, TipoTile = 0, Colision = false, Interactivo = false });
            for (int x = 0; x < 16; x++) ch.Tiles.Add(new TileDto { X = x, Y = 12, TipoTile = 0, Colision = true, Interactivo = false });
            if (cx == 1) ch.Tiles.Add(new TileDto { X = 5, Y = 11, TipoTile = 1, Colision = true, Interactivo = false });
            if (cx == 2) ch.Tiles.Add(new TileDto { X = 10, Y = 11, TipoTile = 1, Colision = true, Interactivo = false });
            map.Chunks.Add(ch);
        }
        var objects = new ObjectsDto { Definitions = new List<ObjectDefinitionDto> { new() { Id = "enemigo", Nombre = "Enemigo", Colision = true, Interactivo = false, Destructible = true, ScriptId = "onCollision" }, new() { Id = "trampa", Nombre = "Trampa", Colision = true, Interactivo = false, Destructible = false, ScriptId = "onTrigger" }, new() { Id = "collectible", Nombre = "Collectible", Colision = false, Interactivo = true, Destructible = true, ScriptId = "onTrigger" } }, Instances = new List<ObjectInstanceDto> { new() { InstanceId = "col1", DefinitionId = "collectible", X = 25, Y = 8, Nombre = "Moneda" } } };
        var scripts = new ScriptsDto { Scripts = new List<ScriptItemDto> { new() { Id = "onTrigger", Nombre = "Trigger al pasar", Eventos = new List<string> { "onCollision" } }, new() { Id = "onSpawn", Nombre = "Spawn aleatorio", Eventos = new List<string> { "onUpdate" } } } };
        return new TemplateData(project, map, objects, scripts, new AnimationsDto());
    }

    private static TemplateData CreateBuilder()
    {
        var project = new ProjectDto { Nombre = "Builder Sandbox", Descripcion = "Tipo Terraria. Tiles destructibles e interactivos, bloques, puertas. Eventos de destrucción y paleta dinámica por zona.", TileSize = 16, MapWidth = 64, MapHeight = 64, Infinite = true, MapPath = "mapa.json", ObjectsPath = "objetos.json" };
        var map = new MapDto { ChunkSize = 16 };
        var c = new ChunkDto { Cx = 0, Cy = 0 };
        for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) c.Tiles.Add(new TileDto { X = x, Y = y, TipoTile = 0, Colision = false, Interactivo = false });
        for (int x = 0; x < 16; x++) c.Tiles.Add(new TileDto { X = x, Y = 15, TipoTile = 0, Colision = true, Interactivo = true });
        for (int x = 2; x < 14; x++) c.Tiles.Add(new TileDto { X = x, Y = 14, TipoTile = 1, Colision = true, Interactivo = true });
        map.Chunks.Add(c);
        var objects = new ObjectsDto { Definitions = new List<ObjectDefinitionDto> { new() { Id = "bloque", Nombre = "Bloque", Colision = true, Interactivo = true, Destructible = true, ScriptId = "onDestroy" }, new() { Id = "puerta", Nombre = "Puerta", Colision = true, Interactivo = true, Destructible = false, ScriptId = "onInteract" }, new() { Id = "elemento", Nombre = "Elemento interactivo", Colision = false, Interactivo = true, Destructible = true, ScriptId = "onInteract" } }, Instances = new List<ObjectInstanceDto> { new() { InstanceId = "b1", DefinitionId = "bloque", X = 5, Y = 13, Nombre = "Bloque" } } };
        var scripts = new ScriptsDto { Scripts = new List<ScriptItemDto> { new() { Id = "onDestroy", Nombre = "Destrucción", Eventos = new List<string> { "onInteract" } }, new() { Id = "onInteract", Nombre = "Interacción", Eventos = new List<string> { "onInteract" } } } };
        return new TemplateData(project, map, objects, scripts, new AnimationsDto());
    }

    private static TemplateData CreateMinijuegos()
    {
        var project = new ProjectDto { Nombre = "Mini Juegos", Descripcion = "Puzzles, matching, arcade. Grid de objetos, fichas, botones, temporizadores. Animaciones simples y paleta dinámica.", TileSize = 16, MapWidth = 24, MapHeight = 24, Infinite = false, MapPath = "mapa.json", ObjectsPath = "objetos.json" };
        var map = new MapDto { ChunkSize = 16 };
        var c = new ChunkDto { Cx = 0, Cy = 0 };
        for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) c.Tiles.Add(new TileDto { X = x, Y = y, TipoTile = 0, Colision = false, Interactivo = false });
        map.Chunks.Add(c);
        var objects = new ObjectsDto { Definitions = new List<ObjectDefinitionDto> { new() { Id = "ficha", Nombre = "Ficha", Colision = true, Interactivo = true, Destructible = false, ScriptId = "onMatch" }, new() { Id = "boton", Nombre = "Botón", Colision = false, Interactivo = true, Destructible = false, ScriptId = "onInteract" }, new() { Id = "temporizador", Nombre = "Temporizador", Colision = false, Interactivo = false, Destructible = false, ScriptId = "onComplete" } }, Instances = new List<ObjectInstanceDto> { new() { InstanceId = "f1", DefinitionId = "ficha", X = 3, Y = 3, Nombre = "Ficha" }, new() { InstanceId = "b1", DefinitionId = "boton", X = 7, Y = 7, Nombre = "Inicio" } } };
        var scripts = new ScriptsDto { Scripts = new List<ScriptItemDto> { new() { Id = "onMatch", Nombre = "Detectar combinación", Eventos = new List<string> { "onInteract" } }, new() { Id = "onComplete", Nombre = "Al completar acción", Eventos = new List<string> { "onUpdate" } } } };
        return new TemplateData(project, map, objects, scripts, new AnimationsDto());
    }

    public static string ToJson(object obj) => JsonSerializer.Serialize(obj, JsonOptions);
}

public class TemplateData
{
    public ProjectDto Project { get; }
    public MapDto Map { get; }
    public ObjectsDto Objects { get; }
    public ScriptsDto Scripts { get; }
    public AnimationsDto Animations { get; }

    public TemplateData(ProjectDto project, MapDto map, ObjectsDto objects, ScriptsDto scripts, AnimationsDto animations)
    {
        Project = project;
        Map = map;
        Objects = objects;
        Scripts = scripts;
        Animations = animations;
    }
}
