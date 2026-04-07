using System.Collections.Generic;
using System.IO;
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
        {
            project.FormatMigrationDeclinedAtOpen = false;
            return true;
        }

        var msg =
            $"Este proyecto usa el formato interno v{project.ProjectFormatVersion}; el motor actual usa v{ProjectSchema.CurrentFormatVersion}.\n\n" +
            "¿Actualizar el archivo del proyecto de forma segura? No se borran mapas ni assets; se aplican los pasos de migración del manifiesto para esta versión del motor.\n\n" +
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
            project.FormatMigrationDeclinedAtOpen = false;
            if (File.Exists(projectFilePath))
            {
                try
                {
                    File.Copy(projectFilePath, projectFilePath + ".bak", overwrite: true);
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show(owner,
                        "No se pudo crear la copia de seguridad (.bak) antes de migrar:\n" + ex.Message +
                        "\n\nHaz una copia manual del .FUE si lo necesitas; al continuar se guardará la migración sin .bak automático.",
                        "Formato del proyecto",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }

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

        project.FormatMigrationDeclinedAtOpen = true;
        WpfMessageBox.Show(owner,
            "El proyecto se abrirá sin modificar el archivo .FUE en disco.\n\n" +
            "Riesgo: este motor puede asumir formato nuevo en otras partes. Si guardas mezclando criterios, podrías obtener datos desalineados. Conviene migrar pronto o trabajar con una copia.\n\n" +
            "Al guardar el manifiesto, el campo de versión de formato seguirá siendo el antiguo hasta que aceptes migrar al abrir.",
            "Formato del proyecto",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return true;
    }
}
