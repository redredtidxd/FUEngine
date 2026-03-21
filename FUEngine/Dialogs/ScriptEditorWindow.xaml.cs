using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace FUEngine;

public partial class ScriptEditorWindow : Window
{
    private string _filePath = "";
    private string _originalContent = "";
    private bool _modified;

    public ScriptEditorWindow()
    {
        InitializeComponent();
    }

    public void OpenFile(string filePath)
    {
        _filePath = filePath ?? "";
        Title = "Editor - " + (Path.GetFileName(_filePath) ?? "sin nombre");
        TxtFileName.Text = _filePath;
        _originalContent = "";
        _modified = false;
        TxtModified.Visibility = Visibility.Collapsed;
        if (File.Exists(_filePath))
        {
            try
            {
                _originalContent = File.ReadAllText(_filePath, Encoding.UTF8);
                EditorTextBox.Text = _originalContent;
            }
            catch (Exception ex)
            {
                EditorTextBox.Text = "";
                TxtStatus.Text = "Error al cargar: " + ex.Message;
                return;
            }
        }
        else
            EditorTextBox.Text = "";
        UpdateLineNumbers();
        TxtStatus.Text = "Listo";
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        EditorTextBox.Focus();
    }

    private void EditorTextBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _modified = EditorTextBox.Text != _originalContent;
        TxtModified.Visibility = _modified ? Visibility.Visible : Visibility.Collapsed;
        UpdateLineNumbers();
    }

    private void UpdateLineNumbers()
    {
        var text = EditorTextBox?.Text ?? "";
        var lines = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
        var nums = string.Join(Environment.NewLine, Enumerable.Range(1, Math.Max(1, lines)));
        TxtLineNumbers.Text = nums;
    }

    private void EditorTextBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            SaveFile();
        }
    }

    private void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        SaveFile();
    }

    private void SaveFile()
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            TxtStatus.Text = "No hay archivo abierto.";
            return;
        }
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, EditorTextBox.Text, Encoding.UTF8);
            _originalContent = EditorTextBox.Text;
            _modified = false;
            TxtModified.Visibility = Visibility.Collapsed;
            TxtStatus.Text = "Guardado.";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Error: " + ex.Message;
            System.Windows.MessageBox.Show(this, "Error al guardar: " + ex.Message, "Guardar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClose_OnClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_modified && System.Windows.MessageBox.Show(this, "Hay cambios sin guardar. ¿Cerrar de todos modos?", "Cerrar editor", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            e.Cancel = true;
        base.OnClosing(e);
    }
}
