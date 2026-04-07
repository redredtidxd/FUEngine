using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace FUEngine.Installer;

internal sealed class InstallForm : Form
{
    private readonly PictureBox _picBrand = new();
    private readonly Label _lblWelcome = new();
    private readonly Label _lblFootnote = new();
    private readonly Label _lblProblem = new();
    private readonly GroupBox _grpDeps = new();
    private readonly CheckBox _chkVc = new();
    private readonly CheckBox _chkDx = new();
    private readonly CheckBox _chkNet = new();
    private readonly Label _lblPath = new();
    private readonly TextBox _txtPath = new();
    private readonly Button _btnBrowse = new();
    private readonly Button _btnInstall = new();
    private readonly Button _btnCancel = new();
    private readonly Label _lblStatus = new();
    private readonly ProgressBar _progress = new();

    public InstallForm()
    {
        Text = "Instalar FUEngine";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = SystemColors.Control;
        ForeColor = SystemColors.ControlText;
        Font = SystemFonts.MessageBoxFont;
        AutoScaleMode = AutoScaleMode.Font;

        _picBrand.Size = new Size(84, 84);
        _picBrand.SizeMode = PictureBoxSizeMode.Zoom;
        _picBrand.Location = new Point(20, 14);
        _picBrand.BackColor = Color.White;
        _picBrand.TabStop = false;
        TryLoadBrandImage();

        _lblWelcome.AutoSize = false;
        _lblWelcome.Size = new Size(368, 80);
        _lblWelcome.Location = new Point(112, 12);
        _lblWelcome.Text =
            "FUEngine: Motor 2D de alto rendimiento para Pixel Art. Incluye IDE integrado con validación de sintaxis, " +
            "sistema de partículas avanzado y soporte modular para Lua.";
        _lblWelcome.ForeColor = SystemColors.ControlText;

        _lblFootnote.AutoSize = false;
        _lblFootnote.Size = new Size(460, 56);
        _lblFootnote.Location = new Point(20, 102);
        _lblFootnote.ForeColor = SystemColors.GrayText;
        _lblFootnote.Text =
            "El ejecutable del motor incluye .NET; no hace falta el Desktop Runtime para abrir el editor. " +
            "Preferencias, historial y logs del usuario van a %LocalAppData%\\FUEngine (no en Archivos de programa). " +
            "Tus proyectos los guardas donde quieras (por ejemplo Documentos\\FUEngine); no se borran al reinstalar el motor.";

        _grpDeps.Text = "Dependencias del sistema (antes de copiar el motor)";
        _grpDeps.Location = new Point(20, 168);
        _grpDeps.Size = new Size(460, 102);

        _chkVc.AutoSize = true;
        _chkVc.Location = new Point(12, 22);
        _chkVc.Text = "Visual C++ 2015-2022 (x64) — recomendado (NLua, Vulkan, NAudio)";
        _chkVc.Checked = true;

        _chkDx.AutoSize = true;
        _chkDx.Location = new Point(12, 44);
        _chkDx.Text = "DirectX End-User Runtime (instalador web) — útil en equipos sin runtime completo";
        _chkDx.Checked = true;

        _chkNet.AutoSize = true;
        _chkNet.Location = new Point(12, 66);
        _chkNet.Text = "Si falta .NET 8 Desktop Runtime, abrir la página de descarga de Microsoft";
        _chkNet.Checked = false;

        _grpDeps.Controls.Add(_chkVc);
        _grpDeps.Controls.Add(_chkDx);
        _grpDeps.Controls.Add(_chkNet);

        _lblProblem.AutoSize = false;
        _lblProblem.Size = new Size(368, 96);
        _lblProblem.Location = new Point(112, 16);
        _lblProblem.ForeColor = Color.FromArgb(180, 0, 0);
        _lblProblem.Text =
            "Falta el motor embebido. Vuelve a generar el instalador: installer\\build-installer.ps1 (Release).";

        _lblPath.Text = "Carpeta de instalación del motor:";
        _lblPath.AutoSize = true;

        _txtPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _txtPath.Text = InstallOperations.GetDefaultInstallPath();

        _btnBrowse.Text = "Examinar…";
        _btnBrowse.Size = new Size(96, 28);
        _btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnBrowse.Click += (_, _) => BrowseFolder();

        _btnInstall.Text = "Instalar";
        _btnInstall.Size = new Size(110, 32);
        _btnInstall.UseVisualStyleBackColor = true;
        _btnInstall.Click += async (_, _) => await RunInstallAsync();

        _btnCancel.Text = "Salir";
        _btnCancel.Size = new Size(88, 32);
        _btnCancel.Click += (_, _) => Close();

        _lblStatus.AutoSize = false;
        _lblStatus.Height = 18;
        _lblStatus.ForeColor = SystemColors.GrayText;
        _lblStatus.Visible = false;

        _progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _progress.Height = 18;
        _progress.Style = ProgressBarStyle.Marquee;
        _progress.MarqueeAnimationSpeed = 25;
        _progress.Visible = false;

        Controls.Add(_picBrand);
        Controls.Add(_lblWelcome);
        Controls.Add(_lblFootnote);
        Controls.Add(_grpDeps);
        Controls.Add(_lblProblem);
        Controls.Add(_lblPath);
        Controls.Add(_txtPath);
        Controls.Add(_btnBrowse);
        Controls.Add(_btnInstall);
        Controls.Add(_btnCancel);
        Controls.Add(_lblStatus);
        Controls.Add(_progress);

        AcceptButton = _btnInstall;
        CancelButton = _btnCancel;

        ApplyLayout();
    }

    private void ApplyLayout()
    {
        var ok = InstallOperations.HasInstallablePayload();
        _lblProblem.Visible = !ok;
        _lblWelcome.Visible = ok;
        _lblFootnote.Visible = ok;
        _grpDeps.Visible = ok;
        _btnInstall.Enabled = ok;

        var padLeft = _picBrand.Visible ? 112 : 20;
        var welcomeW = _picBrand.Visible ? 368 : 460;
        _lblWelcome.SetBounds(padLeft, 12, welcomeW, 80);
        if (!ok)
        {
            _lblProblem.SetBounds(padLeft, 16, welcomeW, 96);
        }

        ClientSize = new Size(500, ok ? 430 : 260);

        var pathTop = ok ? 286 : 128;
        _lblPath.Location = new Point(20, pathTop);
        _txtPath.Location = new Point(20, pathTop + 22);
        _txtPath.Width = ClientSize.Width - 40 - 100 - 8;
        _btnBrowse.Location = new Point(ClientSize.Width - 20 - 96, pathTop + 19);

        var btnTop = pathTop + 56;
        _btnInstall.Location = new Point(20, btnTop);
        _btnCancel.Location = new Point(138, btnTop);
        _lblStatus.Location = new Point(20, btnTop + 36);
        _lblStatus.Width = ClientSize.Width - 40;
        _progress.Location = new Point(20, btnTop + 58);
        _progress.Width = ClientSize.Width - 40;
    }

    private void TryLoadBrandImage()
    {
        try
        {
            var asm = typeof(InstallForm).Assembly;
            var resName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("mando_logo_de_fuengine.png", StringComparison.Ordinal));
            if (resName == null) return;
            using var stream = asm.GetManifestResourceStream(resName);
            if (stream == null) return;
            using var temp = Image.FromStream(stream);
            _picBrand.Image = new Bitmap(temp);
            _picBrand.Visible = true;
        }
        catch
        {
            _picBrand.Visible = false;
            /* recurso no embebido o imagen inválida */
        }
    }

    private void BrowseFolder()
    {
        using var d = new FolderBrowserDialog
        {
            Description = "Carpeta donde instalar FUEngine",
            UseDescriptionForTitle = true,
            InitialDirectory = Path.GetDirectoryName(_txtPath.Text.Trim())
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        };
        if (d.ShowDialog(this) == DialogResult.OK)
            _txtPath.Text = Path.Combine(d.SelectedPath, InstallConstants.ProductName);
    }

    private async Task RunInstallAsync()
    {
        var path = _txtPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show("Elige una carpeta.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            path = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var opts = new PrerequisiteOptions(_chkVc.Checked, _chkDx.Checked, _chkNet.Checked);

        _btnInstall.Enabled = false;
        _btnBrowse.Enabled = false;
        _btnCancel.Enabled = false;
        _chkVc.Enabled = false;
        _chkDx.Enabled = false;
        _chkNet.Enabled = false;
        _progress.Visible = true;
        _lblStatus.Text = "";
        _lblStatus.Visible = false;

        try
        {
            var progress = new Progress<string>(msg =>
            {
                void Apply()
                {
                    _lblStatus.Text = msg;
                    _lblStatus.Visible = !string.IsNullOrEmpty(msg);
                }

                if (InvokeRequired)
                    BeginInvoke(Apply);
                else
                    Apply();
            });

            await Task.Run(() =>
            {
                PrerequisitesInstaller.Run(opts, progress);
                InstallOperations.PrepareInstallDirectory(path, progress);
                InstallOperations.InstallEngineTo(path, progress);
            });

            InstallOperations.TryCreateDesktopShortcut(path);

            OpenFolderInExplorer(path);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _lblStatus.Visible = false;
            _lblStatus.Text = "";
            _progress.Visible = false;
            _btnInstall.Enabled = InstallOperations.HasInstallablePayload();
            _btnBrowse.Enabled = true;
            _btnCancel.Enabled = true;
            _chkVc.Enabled = true;
            _chkDx.Enabled = true;
            _chkNet.Enabled = true;
        }
    }

    private static void OpenFolderInExplorer(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true,
            };
            psi.ArgumentList.Add(folderPath);
            Process.Start(psi);
        }
        catch
        {
            /* sin Explorer o ruta inválida */
        }
    }
}
