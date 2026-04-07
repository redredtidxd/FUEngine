using System.IO;
using System.Linq;
using System.Text;
using FUEngine.Core;
using FUEngine.Help;

namespace FUEngine.Spotlight;

/// <summary>Totales del índice estático (manual + Lua en memoria).</summary>
internal readonly record struct SpotlightIndexTotals(
    int Documentation,
    int ScriptExamples,
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
    private static List<SpotlightItem>? _scriptExampleItems;
    private static List<SpotlightItem>? _luaItems;
    private static readonly object Gate = new();
    private static readonly object ProjectFileGate = new();
    private static readonly Dictionary<string, List<string>> ProjectFileCache = new(StringComparer.OrdinalIgnoreCase);

    public static void EnsureBuilt()
    {
        lock (Gate)
        {
            _docItems ??= BuildDocumentation();
            _scriptExampleItems ??= BuildScriptExamples();
            _luaItems ??= BuildLua();
        }
    }

    private static List<SpotlightItem> BuildDocumentation()
    {
        var list = new List<SpotlightItem>();
        foreach (var t in EngineDocumentation.Topics)
        {
            if (EngineDocumentation.IsLuaReferenceSidebarTopic(t.Id)) continue;
            if (EngineDocumentation.IsScriptExamplesSidebarTopic(t.Id)) continue;
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

    private static List<SpotlightItem> BuildScriptExamples()
    {
        var list = new List<SpotlightItem>();
        foreach (var t in EngineDocumentation.Topics)
        {
            if (!EngineDocumentation.IsScriptExamplesSidebarTopic(t.Id)) continue;
            var sb = new StringBuilder();
            sb.Append(t.Title).Append(' ');
            if (!string.IsNullOrEmpty(t.ExampleCategory)) sb.Append(t.ExampleCategory).Append(' ');
            if (!string.IsNullOrEmpty(t.ParaQue)) sb.Append(t.ParaQue).Append(' ');
            if (!string.IsNullOrEmpty(t.PorQueImporta)) sb.Append(t.PorQueImporta).Append(' ');
            if (!string.IsNullOrEmpty(t.EnMotor)) sb.Append(t.EnMotor).Append(' ');
            foreach (var p in t.Paragraphs) sb.Append(p).Append(' ');
            if (t.Bullets != null)
                foreach (var b in t.Bullets) sb.Append(b).Append(' ');
            if (!string.IsNullOrEmpty(t.LuaExampleCode))
                sb.Append(t.LuaExampleCode).Append(' ');
            if (!string.IsNullOrEmpty(t.ExampleSearchTags))
                sb.Append(t.ExampleSearchTags).Append(' ');
            if (!string.IsNullOrEmpty(t.ExampleDifficulty))
                sb.Append(t.ExampleDifficulty).Append(' ');

            var cat = string.IsNullOrWhiteSpace(t.ExampleCategory) ? "Ejemplos" : t.ExampleCategory.Trim();
            var dif = string.IsNullOrWhiteSpace(t.ExampleDifficulty) ? "" : (" · " + t.ExampleDifficulty.Trim());
            var subtitle = cat + dif;

            var detail = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(t.ParaQue)) detail.Append(t.ParaQue).Append('\n');
            if (!string.IsNullOrWhiteSpace(t.PorQueImporta)) detail.Append(t.PorQueImporta);

            var preview = t.LuaExampleCode;
            if (!string.IsNullOrEmpty(preview) && preview.Length > 900)
                preview = preview.Substring(0, 900) + "\n-- ...";
            list.Add(new SpotlightItem
            {
                Category = SpotlightCategory.ScriptExamples,
                Title = t.Title,
                Subtitle = subtitle,
                SearchText = sb.ToString(),
                DocumentationTopicId = t.Id,
                LuaDetail = detail.ToString().Trim(),
                LuaExample = preview
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
                LuaDetail = detail,
                DocumentationTopicId = "lua-kw-" + name
            });
        }

        var introLua = EngineDocumentation.Topics.FirstOrDefault(x => x.Id == EngineDocumentation.LuaReferenceIntroTopicId);
        if (introLua != null && seenLuaKeys.Add("lua-ref-intro-spot"))
        {
            var sbIntro = new StringBuilder();
            sbIntro.Append(introLua.Title).Append(' ');
            if (!string.IsNullOrEmpty(introLua.ParaQue)) sbIntro.Append(introLua.ParaQue).Append(' ');
            foreach (var p in introLua.Paragraphs) sbIntro.Append(p).Append(' ');
            list.Add(new SpotlightItem
            {
                Category = SpotlightCategory.LuaApi,
                Title = introLua.Title,
                Subtitle = "Índice Lua (sintaxis y librería)",
                SearchText = sbIntro.ToString(),
                DocumentationTopicId = introLua.Id,
                LuaSignature = introLua.Id,
                LuaDetail = introLua.ParaQue ?? introLua.Title
            });
        }

        foreach (var t in EngineDocumentation.Topics)
        {
            if (!t.Id.StartsWith("lua-guide-", StringComparison.Ordinal)) continue;
            var key = "spotlua:" + t.Id;
            if (!seenLuaKeys.Add(key)) continue;
            var sb = new StringBuilder();
            sb.Append(t.Title).Append(' ');
            if (!string.IsNullOrEmpty(t.ParaQue)) sb.Append(t.ParaQue).Append(' ');
            foreach (var p in t.Paragraphs) sb.Append(p).Append(' ');
            list.Add(new SpotlightItem
            {
                Category = SpotlightCategory.LuaApi,
                Title = t.Title,
                Subtitle = "Guía Lua (librería)",
                SearchText = sb.ToString(),
                DocumentationTopicId = t.Id,
                LuaSignature = t.Id,
                LuaDetail = t.ParaQue ?? t.Title
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
            ScriptExamples: _scriptExampleItems!.Count,
            LuaReflection: refl,
            LuaGlobalGuides: guides,
            LuaHooks: hooks,
            LuaBuiltins: builtins,
            LuaKeywords: keywords,
            LuaTotal: lua.Count,
            HubRecentProjects: hub);
    }

    public static IReadOnlyList<SpotlightItem> ScriptExampleItems
    {
        get
        {
            EnsureBuilt();
            return _scriptExampleItems!;
        }
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
        return ScoreMatch(query, haystack) > 0;
    }

    private static IEnumerable<string> Tokenize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) yield break;
        var t = s.Trim();
        var sb = new StringBuilder();
        for (var i = 0; i < t.Length; i++)
        {
            var c = t[i];
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
            {
                sb.Append(char.ToLowerInvariant(c));
                continue;
            }
            if (sb.Length > 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
        }
        if (sb.Length > 0) yield return sb.ToString();
    }

    private static int ScoreMatch(string query, string haystack)
    {
        if (string.IsNullOrWhiteSpace(query)) return 1;
        if (string.IsNullOrEmpty(haystack)) return 0;
        var q = query.Trim();
        if (q.Length == 0) return 1;

        // Match rápido exacto (case-insensitive).
        if (haystack.Contains(q, StringComparison.OrdinalIgnoreCase))
            return 120;

        // Scoring por tokens: todas las palabras deben aparecer (parcial) en algún token del texto.
        var qTokens = Tokenize(q).ToArray();
        if (qTokens.Length == 0) return 0;

        var hLower = haystack.ToLowerInvariant();
        var score = 0;
        foreach (var qt in qTokens)
        {
            if (qt.Length == 0) continue;
            var idx = hLower.IndexOf(qt, StringComparison.Ordinal);
            if (idx < 0) return 0;

            // Mejor si coincide al inicio de palabra (espacio/guion/punto) o al principio.
            var boundary = idx == 0 ||
                           !char.IsLetterOrDigit(hLower[idx - 1]) ||
                           hLower[idx - 1] == '_' || hLower[idx - 1] == '-' || hLower[idx - 1] == '.';
            score += boundary ? 40 : 18;
            score += Math.Clamp(20 - idx / 24, 0, 20); // más arriba = mejor
        }
        return score;
    }

    private static IEnumerable<SpotlightItem> RankAndTake(IEnumerable<SpotlightItem> items, string q, int take)
    {
        if (string.IsNullOrWhiteSpace(q))
            return items.Take(take);

        var list = items
            .Select(x =>
            {
                var hay = (x.Title ?? "") + " " + (x.Subtitle ?? "") + " " + (x.LuaDetail ?? "") + " " + (x.SearchText ?? "");
                var s = ScoreMatch(q, hay);
                return (Item: x, Score: s);
            })
            .Where(p => p.Score > 0)
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Item.Title, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(p => p.Item);
        return list;
    }

    public static IEnumerable<SpotlightItem> FilterDocs(string q)
    {
        EnsureBuilt();
        return RankAndTake(DocumentationItems, q, string.IsNullOrWhiteSpace(q) ? 14 : 55);
    }

    public static IEnumerable<SpotlightItem> FilterLua(string q)
    {
        EnsureBuilt();
        return RankAndTake(LuaItems, q, string.IsNullOrWhiteSpace(q) ? 28 : 80);
    }

    public static IEnumerable<SpotlightItem> FilterScriptExamples(string q)
    {
        EnsureBuilt();
        return RankAndTake(ScriptExampleItems, q, string.IsNullOrWhiteSpace(q) ? 18 : 55);
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
            List<string> files;
            lock (ProjectFileGate)
            {
                if (!ProjectFileCache.TryGetValue(projectDir, out files!))
                {
                    files = new List<string>(2048);
                    foreach (var ext in new[] { "*.lua", "*.map", "*.seed" })
                    {
                        foreach (var path in Directory.EnumerateFiles(projectDir, ext, SearchOption.AllDirectories))
                        {
                            if (path.Contains("snapshots", StringComparison.OrdinalIgnoreCase)) continue;
                            files.Add(path);
                        }
                    }
                    ProjectFileCache[projectDir] = files;
                }
            }

            foreach (var path in files)
            {
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

    public static SpotlightItem? MatchChangelogFile(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return null;
        var t = q.Trim();
        if (!t.Contains("changelog", StringComparison.OrdinalIgnoreCase) &&
            !t.Contains("historial", StringComparison.OrdinalIgnoreCase) &&
            !t.Contains("versión", StringComparison.OrdinalIgnoreCase) &&
            !t.Contains("novedad", StringComparison.OrdinalIgnoreCase))
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
