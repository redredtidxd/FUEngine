using System.IO;
using System.Linq;
using System.Text;
using FUEngine.Core;
using FUEngine.Help;

namespace FUEngine.Spotlight;

/// <summary>Totales del índice estático (manual + Lua en memoria).</summary>
internal readonly record struct SpotlightIndexTotals(
    int Documentation,
    int LuaReflection,
    int LuaGlobalGuides,
    int LuaHooks,
    int LuaBuiltins,
    int LuaKeywords,
    int LuaTotal,
    int HubRecentProjects);

/// <summary>Índice en memoria para Spotlight (documentación + API Lua); archivos/objetos se calculan en la búsqueda.</summary>
internal static class SpotlightIndex
{
    private static List<SpotlightItem>? _docItems;
    private static List<SpotlightItem>? _luaItems;
    private static readonly object Gate = new();

    public static void EnsureBuilt()
    {
        lock (Gate)
        {
            _docItems ??= BuildDocumentation();
            _luaItems ??= BuildLua();
        }
    }

    private static List<SpotlightItem> BuildDocumentation()
    {
        var list = new List<SpotlightItem>();
        foreach (var t in EngineDocumentation.Topics)
        {
            var sb = new StringBuilder();
            sb.Append(t.Title).Append(' ');
            if (!string.IsNullOrEmpty(t.ParaQue)) sb.Append(t.ParaQue).Append(' ');
            if (!string.IsNullOrEmpty(t.PorQueImporta)) sb.Append(t.PorQueImporta).Append(' ');
            foreach (var p in t.Paragraphs) sb.Append(p).Append(' ');
            if (t.Bullets != null)
                foreach (var b in t.Bullets) sb.Append(b).Append(' ');
            list.Add(new SpotlightItem
            {
                Category = SpotlightCategory.Documentation,
                Title = t.Title,
                Subtitle = "Manual integrado",
                SearchText = sb.ToString(),
                DocumentationTopicId = t.Id
            });
        }
        return list;
    }

    private static List<SpotlightItem> BuildLua()
    {
        var list = new List<SpotlightItem>();
        var seenLuaKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var map = LuaEditorApiReflection.BuildMemberMap();
        foreach (var (id, title, detail) in LuaSpotlightDescriptions.GlobalTableGuides)
        {
            if (!seenLuaKeys.Add(id)) continue;
            list.Add(new SpotlightItem
            {
                Category = SpotlightCategory.LuaApi,
                Title = title,
                Subtitle = "Resumen API (tabla global)",
                SearchText = id + " " + title + " " + detail,
                LuaSignature = id,
                LuaDetail = detail
            });
        }

        foreach (var kv in map)
        {
            var prefix = kv.Key;
            foreach (var member in kv.Value)
            {
                var full = prefix + member;
                seenLuaKeys.Add(full);
                var detail = LuaSpotlightDescriptions.DefaultMemberDetail(prefix, member);
                string? ex = null;
                if (LuaSpotlightDescriptions.TryGetMemberHint(full, out var hintDetail, out var hintEx))
                {
                    detail = hintDetail;
                    ex = hintEx;
                }
                list.Add(new SpotlightItem
                {
                    Category = SpotlightCategory.LuaApi,
                    Title = full,
                    Subtitle = "API Lua (reflexión)",
                    SearchText = full + " " + detail,
                    LuaSignature = full,
                    LuaDetail = detail,
                    LuaExample = ex
                });
            }
        }

        foreach (var ev in KnownEvents.All)
        {
            seenLuaKeys.Add(ev);
            LuaSpotlightDescriptions.TryGetHook(ev, out var title, out var det, out var ex);
            list.Add(new SpotlightItem
            {
                Category = SpotlightCategory.LuaApi,
                Title = title,
                Subtitle = "Hook / evento",
                SearchText = ev + " " + title + " " + det,
                LuaSignature = ev,
                LuaDetail = det,
                LuaExample = ex
            });
        }

        foreach (var (name, detail) in LuaSpotlightBuiltins.Entries)
        {
            if (!seenLuaKeys.Add(name)) continue;
            list.Add(new SpotlightItem
            {
                Category = SpotlightCategory.LuaApi,
                Title = name,
                Subtitle = LuaSpotlightBuiltins.BuiltinSubtitle,
                SearchText = name + " " + detail,
                LuaSignature = name,
                LuaDetail = detail
            });
        }

        foreach (var (name, detail) in LuaLanguageKeywords.Entries)
        {
            if (!seenLuaKeys.Add(name)) continue;
            list.Add(new SpotlightItem
            {
                Category = SpotlightCategory.LuaApi,
                Title = name,
                Subtitle = LuaLanguageKeywords.KeywordSubtitle,
                SearchText = name + " " + detail,
                LuaSignature = name,
                LuaDetail = detail
            });
        }

        return list;
    }

    public static SpotlightIndexTotals GetIndexTotals()
    {
        EnsureBuilt();
        var lua = _luaItems!;
        var refl = lua.Count(x => x.Subtitle == "API Lua (reflexión)");
        var guides = lua.Count(x => x.Subtitle == "Resumen API (tabla global)");
        var hooks = lua.Count(x => x.Subtitle == "Hook / evento");
        var builtins = lua.Count(x => x.Subtitle == LuaSpotlightBuiltins.BuiltinSubtitle);
        var keywords = lua.Count(x => x.Subtitle == LuaLanguageKeywords.KeywordSubtitle);
        var hub = 0;
        try
        {
            hub = StartupService.LoadRecentProjects().Count(x => File.Exists(x.Path));
        }
        catch
        {
            /* ignore */
        }

        return new SpotlightIndexTotals(
            Documentation: _docItems!.Count,
            LuaReflection: refl,
            LuaGlobalGuides: guides,
            LuaHooks: hooks,
            LuaBuiltins: builtins,
            LuaKeywords: keywords,
            LuaTotal: lua.Count,
            HubRecentProjects: hub);
    }

    public static IReadOnlyList<SpotlightItem> DocumentationItems
    {
        get
        {
            EnsureBuilt();
            return _docItems!;
        }
    }

    public static IReadOnlyList<SpotlightItem> LuaItems
    {
        get
        {
            EnsureBuilt();
            return _luaItems!;
        }
    }

    public static bool Matches(string query, string haystack)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        return haystack.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static IEnumerable<SpotlightItem> FilterDocs(string q)
    {
        EnsureBuilt();
        if (string.IsNullOrWhiteSpace(q))
            return DocumentationItems.Take(10);
        return DocumentationItems.Where(x => Matches(q, x.Title + " " + x.SearchText)).Take(40);
    }

    public static IEnumerable<SpotlightItem> FilterLua(string q)
    {
        EnsureBuilt();
        if (string.IsNullOrWhiteSpace(q))
            return LuaItems.Take(30);
        return LuaItems.Where(x => Matches(q, x.Title + " " + x.Subtitle + " " + (x.LuaDetail ?? "") + " " + x.SearchText)).Take(60);
    }

    public static IEnumerable<SpotlightItem> SearchProjectFiles(string projectDir, string q, int max = 40)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2 || string.IsNullOrEmpty(projectDir) || !Directory.Exists(projectDir))
            return Array.Empty<SpotlightItem>();
        q = q.Trim();
        var ql = q.ToLowerInvariant();
        var results = new List<SpotlightItem>();
        try
        {
            foreach (var ext in new[] { "*.lua", "*.map", "*.seed" })
            {
                foreach (var path in Directory.EnumerateFiles(projectDir, ext, SearchOption.AllDirectories))
                {
                    if (path.Contains("snapshots", StringComparison.OrdinalIgnoreCase)) continue;
                    var name = Path.GetFileName(path);
                    if (!name.ToLowerInvariant().Contains(ql) && !path.ToLowerInvariant().Contains(ql)) continue;
                    results.Add(new SpotlightItem
                    {
                        Category = SpotlightCategory.ProjectFile,
                        Title = name,
                        Subtitle = Path.GetRelativePath(projectDir, path),
                        SearchText = path,
                        FilePath = path
                    });
                    if (results.Count >= max) return results;
                }
            }
        }
        catch
        {
            /* ignore */
        }
        return results;
    }

    public static IEnumerable<SpotlightItem> SearchSceneObjects(ObjectLayer layer, string q, int max = 30)
    {
        if (layer == null || string.IsNullOrWhiteSpace(q)) return Array.Empty<SpotlightItem>();
        q = q.Trim();
        return layer.Instances
            .Where(i =>
                (!string.IsNullOrEmpty(i.Nombre) && i.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                i.InstanceId.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(i.DefinitionId) && i.DefinitionId.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .Take(max)
            .Select(i => new SpotlightItem
            {
                Category = SpotlightCategory.SceneObject,
                Title = string.IsNullOrEmpty(i.Nombre) ? i.InstanceId : i.Nombre,
                Subtitle = "Objeto en escena · " + i.DefinitionId,
                SearchText = i.Nombre + i.InstanceId + i.DefinitionId,
                ObjectInstanceId = i.InstanceId
            });
    }

    public static IEnumerable<SpotlightItem> SearchHubProjects(string q, int max = 25)
    {
        if (string.IsNullOrWhiteSpace(q)) return Array.Empty<SpotlightItem>();
        q = q.Trim();
        var list = StartupService.LoadRecentProjects()
            .Where(x => File.Exists(x.Path))
            .Where(x =>
                (x.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Path?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.ShortPath?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(max)
            .Select(x => new SpotlightItem
            {
                Category = SpotlightCategory.HubProject,
                Title = x.Name ?? Path.GetFileName(Path.GetDirectoryName(x.Path) ?? ""),
                Subtitle = x.ShortPath ?? "",
                SearchText = x.Path,
                HubProjectPath = x.Path
            });
        return list;
    }

    public static SpotlightItem? MatchAiOnboarding(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return null;
        var t = q.Trim();
        if (!t.Contains("novedad", StringComparison.OrdinalIgnoreCase) &&
            !t.Contains("changelog", StringComparison.OrdinalIgnoreCase) &&
            !t.Contains("onboarding", StringComparison.OrdinalIgnoreCase) &&
            !t.Contains("ia ", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(t, "ai", StringComparison.OrdinalIgnoreCase))
            return null;
        var path = FindRepoMarkdown("docs/AI-ONBOARDING.md");
        if (path == null) return null;
        return new SpotlightItem
        {
            Category = SpotlightCategory.ExternalDoc,
            Title = "Guía técnica para IAs (AI-ONBOARDING)",
            Subtitle = path,
            SearchText = "novedades documentación ia onboarding",
            ExternalMarkdownPath = path
        };
    }

    public static SpotlightItem? MatchChangelogFile(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return null;
        var t = q.Trim();
        if (!t.Contains("changelog", StringComparison.OrdinalIgnoreCase) &&
            !t.Contains("historial", StringComparison.OrdinalIgnoreCase) &&
            !t.Contains("versión", StringComparison.OrdinalIgnoreCase))
            return null;
        var path = FindRepoMarkdown("docs/CHANGELOG.md");
        if (path == null) return null;
        return new SpotlightItem
        {
            Category = SpotlightCategory.ExternalDoc,
            Title = "CHANGELOG del repo",
            Subtitle = path,
            SearchText = t,
            ExternalMarkdownPath = path
        };
    }

    private static string? FindRepoMarkdown(string relative)
    {
        try
        {
            var dir = AppContext.BaseDirectory;
            for (var i = 0; i < 10; i++)
            {
                var p = Path.Combine(dir, relative.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(p)) return p;
                var parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
        }
        catch { /* ignore */ }
        return null;
    }
}
