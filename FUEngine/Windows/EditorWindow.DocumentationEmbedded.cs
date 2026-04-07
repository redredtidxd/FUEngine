using System;
using System.IO;
using System.Windows;
using FUEngine.Core;

namespace FUEngine;

public partial class EditorWindow
{
    private void WireDocumentationEmbeddedScriptExamples()
    {
        if (DocumentationEmbedded == null) return;
        DocumentationEmbedded.AllowCreateScriptFromProject = true;
        DocumentationEmbedded.RequestCreateScriptFromExample += DocumentationEmbedded_OnRequestCreateScriptFromExample;
        DocumentationEmbedded.RequestOpenDetachedWindow += DocumentationEmbedded_OnRequestOpenDetachedWindow;
    }

    private void DocumentationEmbedded_OnRequestOpenDetachedWindow(object? sender, EventArgs e)
    {
        if (DocumentationEmbedded == null) return;
        var topic = DocumentationEmbedded.GetActiveTopicIdForDetach();
        DocumentationOverlay.Visibility = Visibility.Collapsed;
        SyncDiscordRichPresence();
        var w = new DocumentationWindow { Owner = this, InitialTopicId = topic };
        w.Show();
    }

    private void DocumentationEmbedded_OnRequestCreateScriptFromExample(object? sender, CreateScriptFromExampleEventArgs e)
    {
        if (string.IsNullOrEmpty(_project.ProjectDirectory)) return;
        var scriptsDir = Path.Combine(_project.ProjectDirectory, "Scripts");
        try
        {
            if (!Directory.Exists(scriptsDir))
                Directory.CreateDirectory(scriptsDir);
        }
        catch (Exception ex)
        {
            EditorLog.Error("No se pudo crear la carpeta Scripts: " + ex.Message, "Ayuda");
            return;
        }

        var fileName = e.SuggestedFileName;
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "ejemplo.lua";
        if (!fileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
            fileName += ".lua";
        var dest = GetUniqueScriptPathInDirectory(scriptsDir, fileName);
        try
        {
            File.WriteAllText(dest, e.LuaBody ?? "", System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            EditorLog.Error("No se pudo crear el script: " + ex.Message, "Ayuda");
            return;
        }

        if (!ScriptRegistryProjectWriter.TryRegisterLuaFile(_project.ProjectDirectory, dest, out _, out _, out var regErr) &&
            !string.IsNullOrEmpty(regErr))
            EditorLog.Warning("Script creado; aviso al registrar en scripts.json: " + regErr, "Ayuda");

        ProjectExplorer?.RefreshTree();
        AddOrSelectTab("Scripts");
        (GetTabByKind("Scripts")?.Content as ScriptsTabContent)?.OpenFile(dest);
        EditorLog.Toast($"Script creado: {Path.GetFileName(dest)}", LogLevel.Info, "Ayuda");
        SyncDiscordRichPresence();
    }

    private static string GetUniqueScriptPathInDirectory(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path)) return path;
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        for (var i = 2; i < 1000; i++)
        {
            path = Path.Combine(directory, baseName + "_" + i + ext);
            if (!File.Exists(path)) return path;
        }

        return Path.Combine(directory, baseName + "_" + Guid.NewGuid().ToString("N")[..8] + ext);
    }
}
