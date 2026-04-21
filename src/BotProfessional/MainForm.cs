using System.Text;

namespace BotProfessional;

public sealed class MainForm : Form
{
    private readonly TextBox _txtFolder = new() { PlaceholderText = "Klasör yolu...", Width = 620 };
    private readonly Button _btnBrowse = new() { Text = "Gözat", Width = 80 };
    private readonly Button _btnScan = new() { Text = "Dosyaları Tara", Width = 120 };
    private readonly Button _btnFix = new() { Text = "Encoding Düzelt + Çeviri Uygula", Width = 260 };
    private readonly TextBox _txtExtensions = new() { Text = ".txt,.ini,.xml,.json,.cfg,.lua", Width = 280 };
    private readonly CheckBox _chkUtf8Bom = new() { Text = "UTF-8 BOM kullan", Width = 140 };
    private readonly CheckBox _chkBackup = new() { Text = "Yedek (.bak) oluştur", Width = 160, Checked = true };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true, AllowUserToAddRows = false };
    private readonly TextBox _txtRules = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly TextBox _txtLog = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, ReadOnly = true };
    private readonly Label _lblSummary = new() { AutoSize = true, Text = "Hazır" };

    private List<FileReport> _reports = new();

    public MainForm()
    {
        Text = "Bot Professional - Dosya Çeviri ve Encoding Merkezi";
        Width = 1300;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        BuildLayout();

        _btnBrowse.Click += (_, _) => BrowseFolder();
        _btnScan.Click += async (_, _) => await ScanAsync();
        _btnFix.Click += async (_, _) => await FixAsync();
    }

    private void BuildLayout()
    {
        var panelTop = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(8),
            AutoSize = false
        };

        panelTop.Controls.AddRange(new Control[] { _txtFolder, _btnBrowse, _btnScan, _btnFix, _txtExtensions, _chkUtf8Bom, _chkBackup, _lblSummary });

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(FileReport.Path), HeaderText = "Dosya", Width = 570 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(FileReport.EncodingName), HeaderText = "Encoding", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(FileReport.HasReplacementChar), HeaderText = "Bozuk Karakter", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(FileReport.RuleHits), HeaderText = "Eşleşen Kural", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(FileReport.Status), HeaderText = "Durum", Width = 250 });

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var tabFiles = new TabPage("Dosyalar") { Padding = new Padding(6) };
        var tabRules = new TabPage("Çeviri Kuralları") { Padding = new Padding(6) };
        var tabLog = new TabPage("Log") { Padding = new Padding(6) };

        tabFiles.Controls.Add(_grid);

        _txtRules.Text = "örnek=example\r\nkarakter=character\r\n";
        tabRules.Controls.Add(_txtRules);

        tabLog.Controls.Add(_txtLog);

        tabs.TabPages.Add(tabFiles);
        tabs.TabPages.Add(tabRules);
        tabs.TabPages.Add(tabLog);

        Controls.Add(tabs);
        Controls.Add(panelTop);
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _txtFolder.Text = dialog.SelectedPath;
        }
    }

    private async Task ScanAsync()
    {
        var folder = _txtFolder.Text.Trim();
        if (!Directory.Exists(folder))
        {
            MessageBox.Show("Geçerli bir klasör seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        try
        {
            var extensions = ParseExtensions(_txtExtensions.Text);
            var rules = ParseRules(_txtRules.Text);
            _reports = await Task.Run(() => FileQualityService.Scan(folder, extensions, rules));
            _grid.DataSource = _reports;
            UpdateSummary();
            Log($"Tarama tamamlandı: {_reports.Count} dosya bulundu.");
        }
        catch (Exception ex)
        {
            Log($"Tarama hatası: {ex.Message}");
            MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task FixAsync()
    {
        if (_reports.Count == 0)
        {
            MessageBox.Show("Önce tarama yapın.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true);
        try
        {
            var rules = ParseRules(_txtRules.Text);
            var encoding = new UTF8Encoding(_chkUtf8Bom.Checked);

            await Task.Run(() => FileQualityService.ApplyFixes(_reports, rules, encoding, _chkBackup.Checked, Log));
            _grid.Refresh();
            UpdateSummary();
            Log("Fix işlemi tamamlandı.");
        }
        catch (Exception ex)
        {
            Log($"Fix hatası: {ex.Message}");
            MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _btnBrowse.Enabled = !busy;
        _btnScan.Enabled = !busy;
        _btnFix.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void UpdateSummary()
    {
        var broken = _reports.Count(r => r.HasReplacementChar);
        var ok = _reports.Count(r => r.Status.StartsWith("OK", StringComparison.OrdinalIgnoreCase));
        _lblSummary.Text = $"Toplam: {_reports.Count} | Bozuk Karakter: {broken} | OK: {ok}";
    }

    private static List<string> ParseExtensions(string raw)
    {
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : $".{e.ToLowerInvariant()}")
            .Distinct()
            .ToList();
    }

    private static IReadOnlyList<RulePair> ParseRules(string raw)
    {
        var list = new List<RulePair>();
        var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var idx = line.IndexOf('=');
            if (idx < 1 || idx == line.Length - 1) continue;
            var src = line[..idx];
            var dest = line[(idx + 1)..];
            list.Add(new RulePair(src, dest));
        }

        return list;
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Log(message));
            return;
        }

        _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}

public readonly record struct RulePair(string Source, string Target);

public sealed class FileReport
{
    public required string Path { get; init; }
    public required string FullPath { get; init; }
    public required string EncodingName { get; init; }
    public bool HasReplacementChar { get; set; }
    public int RuleHits { get; set; }
    public string Status { get; set; } = "Bekliyor";
}

public static class FileQualityService
{
    private static readonly Encoding Utf8Strict = new UTF8Encoding(false, true);

    public static List<FileReport> Scan(string folder, IReadOnlyCollection<string> extensions, IReadOnlyList<RulePair> rules)
    {
        var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(path => extensions.Contains(System.IO.Path.GetExtension(path).ToLowerInvariant()))
            .ToList();

        var results = new List<FileReport>(files.Count);
        foreach (var file in files)
        {
            var bytes = File.ReadAllBytes(file);
            var (encoding, text) = Decode(bytes);
            var hits = rules.Sum(r => CountOccurrences(text, r.Source));
            var relativePath = file.StartsWith(folder, StringComparison.OrdinalIgnoreCase)
                ? file[folder.Length..].TrimStart(System.IO.Path.DirectorySeparatorChar)
                : file;

            results.Add(new FileReport
            {
                Path = relativePath,
                FullPath = file,
                EncodingName = encoding.WebName,
                HasReplacementChar = text.Contains('\uFFFD'),
                RuleHits = hits,
                Status = "OK - Tarandı"
            });
        }

        return results;
    }

    public static void ApplyFixes(
        IReadOnlyCollection<FileReport> reports,
        IReadOnlyList<RulePair> rules,
        Encoding targetEncoding,
        bool createBackup,
        Action<string> log)
    {
        foreach (var report in reports)
        {
            var fullPath = report.FullPath;

            if (!File.Exists(fullPath))
            {
                report.Status = "Atlandı - dosya yok";
                continue;
            }

            try
            {
                var bytes = File.ReadAllBytes(fullPath);
                var (_, text) = Decode(bytes);

                foreach (var rule in rules)
                {
                    text = text.Replace(rule.Source, rule.Target, StringComparison.Ordinal);
                }

                if (createBackup)
                {
                    File.WriteAllBytes($"{fullPath}.bak", bytes);
                }

                File.WriteAllText(fullPath, text, targetEncoding);
                report.Status = "OK - Düzeltildi";
                report.EncodingName = targetEncoding.WebName;
                report.HasReplacementChar = text.Contains('\uFFFD');
                log($"Düzeltildi: {report.Path}");
            }
            catch (Exception ex)
            {
                report.Status = $"Hata - {ex.Message}";
                log($"Hata: {report.Path} => {ex.Message}");
            }
        }
    }


    private static (Encoding Encoding, string Text) Decode(byte[] bytes)
    {
        if (HasUtf8Bom(bytes))
        {
            return (Encoding.UTF8, Encoding.UTF8.GetString(bytes));
        }

        try
        {
            return (Utf8Strict, Utf8Strict.GetString(bytes));
        }
        catch (DecoderFallbackException)
        {
            var cp1254 = Encoding.GetEncoding(1254);
            return (cp1254, cp1254.GetString(bytes));
        }
    }

    private static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    }

    private static int CountOccurrences(string source, string token)
    {
        if (string.IsNullOrEmpty(token)) return 0;

        var count = 0;
        var idx = 0;
        while ((idx = source.IndexOf(token, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += token.Length;
        }

        return count;
    }
}
