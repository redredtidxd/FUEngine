using System.Collections.Generic;
using System.Windows;
using WpfMessageBox = System.Windows.MessageBox;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>Al abrir un proyecto en el editor: ofrece migración segura del formato interno.</summary>
public static class ProjectFormatOpenHelper
{
    /// <summary>Carga el proyecto y, si el formato es anterior, muestra diálogo. Devuelve false si falló la carga, el usuario canceló, o falló el guardado tras migrar.</summary>
    public static bool TryPromptAndLoad(string projectFilePath, Window? owner, out ProjectInfo? project, out string? error)
    {
        project = null;
        error = null;
        try
        {
            project = ProjectSerialization.Load(projectFilePath);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        if (!ProjectFormatMigration.NeedsUpgrade(project))
            return true;

        var msg =
            $"Este proyecto usa el formato interno v{project.ProjectFormatVersion}; el motor actual usa v{ProjectSchema.CurrentFormatVersion}.\n\n" +
            "¿Actualizar el archivo del proyecto de forma segura? No se borran datos ni recursos; solo se añaden campos y metadatos necesarios.\n\n" +
            "• Sí — guardar ahora y continuar\n" +
            "• No — abrir sin modificar el archivo (se volverá a preguntar al abrir)\n" +
            "• Cancelar — no abrir el proyecto";

        var r = WpfMessageBox.Show(owner, msg, "Formato del proyecto", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (r == MessageBoxResult.Cancel)
        {
            project = null;
            return false;
        }

        if (r == MessageBoxResult.Yes)
        {
            var warnings = new List<string>();
            ProjectFormatMigration.ApplySafeUpgrade(project, warnings);
            try
            {
                ProjectSerialization.Save(project, projectFilePath);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                project = null;
                return false;
            }

            if (warnings.Count > 0)
            {
                WpfMessageBox.Show(owner,
                    "Actualización aplicada:\n\n" + string.Join("\n", warnings),
                    "Formato del proyecto",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            return true;
        }

        WpfMessageBox.Show(owner,
            "El proyecto se abrirá sin modificar el archivo. Puedes seguir trabajando; al guardar, el JSON conservará el formato anterior hasta que elijas actualizar al abrir.",
            "Formato del proyecto",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return true;
    }
}
