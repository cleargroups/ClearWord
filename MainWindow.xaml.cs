using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ClearWord.Models;
using ClearWord.Services;
using Microsoft.Win32;

namespace ClearWord
{
    public partial class MainWindow : Window
    {
        private readonly LanguageService _lang = LanguageService.I;
        private string? _fp;
        private bool _mod;
        private WordConfig _cfg = new();

        private static readonly byte[] _key = SHA256.HashData(Encoding.UTF8.GetBytes("ClearWord_Secure_Key_2026!"));
        private static readonly List<(string Name, Color Color)> _colors = new()
        {
            ("Чёрный",Colors.Black),("Белый",Colors.White),("Красный",Colors.Red),("Зелёный",Colors.Green),
            ("Синий",Colors.Blue),("Жёлтый",Colors.Yellow),("Оранжевый",Color.FromRgb(255,165,0)),
            ("Фиолетовый",Color.FromRgb(128,0,128)),("Серый",Colors.Gray),("Тёмно-серый",Color.FromRgb(64,64,64)),
            ("Светло-серый",Color.FromRgb(211,211,211)),("Коричневый",Color.FromRgb(139,69,19)),
            ("Розовый",Color.FromRgb(255,192,203)),("Голубой",Color.FromRgb(0,191,255)),
            ("Тёмно-зелёный",Color.FromRgb(0,100,0)),("Тёмно-синий",Color.FromRgb(0,0,139)),
            ("Тёмно-красный",Color.FromRgb(139,0,0)),("Золотой",Color.FromRgb(255,215,0)),
            ("Бежевый",Color.FromRgb(245,245,220)),("Бирюзовый",Color.FromRgb(64,224,208)),
        };

        private static readonly string[] _fonts = { "Clear" };

        public MainWindow()
        {
            InitializeComponent();
            Setup();
            ApplyConfig();
            SetLang(true);
        }

        private void Setup()
        {
            FontBox.ItemsSource = _fonts;
            FontBox.SelectedItem = "Clear";
            FontBox.SelectionChanged += (_, _) =>
            {
                if (FontBox.SelectedItem is string f)
                {
                    _cfg.FontFamily = f;
                    RichBox.FontFamily = new FontFamily(f);
                    _mod = true; UpdateStar();
                }
            };

            FontSizeBox.ItemsSource = new double[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 28, 32, 36, 48, 60, 72 };
            FontSizeBox.SelectedItem = 14.0;
            FontSizeBox.SelectionChanged += (_, _) =>
            {
                if (FontSizeBox.SelectedItem is double d)
                {
                    _cfg.FontSize = d;
                    RichBox.FontSize = d;
                    _mod = true; UpdateStar();
                }
            };

            NewMenuItem.Click += (_, _) => NewDoc();
            OpenMenuItem.Click += (_, _) => Open();
            SaveMenuItem.Click += (_, _) => Save();
            SaveAsMenuItem.Click += (_, _) => SaveAs();
            ExitMenuItem.Click += (_, _) => Close();

            UndoMenuItem.Click += (_, _) => RichBox.Undo();
            RedoMenuItem.Click += (_, _) => RichBox.Redo();
            CutMenuItem.Click += (_, _) => ApplicationCommands.Cut.Execute(null, RichBox);
            CopyMenuItem.Click += (_, _) => ApplicationCommands.Copy.Execute(null, RichBox);
            PasteMenuItem.Click += (_, _) => ApplicationCommands.Paste.Execute(null, RichBox);
            SelectAllMenuItem.Click += (_, _) => RichBox.SelectAll();

            SpellCheckMenuItem.Click += (_, _) => RichBox.SpellCheck.IsEnabled = SpellCheckMenuItem.IsChecked;
            GitHubMenuItem.Click += (_, _) => Process.Start(new ProcessStartInfo("https://github.com/ClearGroups") { UseShellExecute = true });
            EnglishMenuItem.Click += (_, _) => SetLang(true);
            RussianMenuItem.Click += (_, _) => SetLang(false);

            BoldBtn.Click += (_, _) => ToggleProp(TextElement.FontWeightProperty, FontWeights.Bold, FontWeights.Normal);
            ItalicBtn.Click += (_, _) => ToggleProp(TextElement.FontStyleProperty, FontStyles.Italic, FontStyles.Normal);
            UnderBtn.Click += (_, _) => ToggleUnderline();
            FontColorBtn.Click += (_, _) => ShowColorPicker(true);
            BgColorBtn.Click += (_, _) => ShowColorPicker(false);

            RichBox.TextChanged += (_, _) => { _mod = true; UpdateStar(); UpdateWordCount(); };
            Closing += (_, e) =>
            {
                if (_mod)
                {
                    var r = MessageBox.Show(_lang.S("SavePrompt"), "ClearWord", MessageBoxButton.YesNoCancel);
                    if (r == MessageBoxResult.Yes) Save();
                    else if (r == MessageBoxResult.Cancel) e.Cancel = true;
                }
            };
        }

        private void ApplyConfig()
        {
            RichBox.FontFamily = new FontFamily(_cfg.FontFamily);
            RichBox.FontSize = _cfg.FontSize;
            RichBox.Foreground = new SolidColorBrush(_cfg.FontColor);
            RichBox.Background = new SolidColorBrush(_cfg.BgColor);
            RichBox.SpellCheck.IsEnabled = _cfg.SpellCheckEnabled;
            FontBox.SelectedItem = _cfg.FontFamily;
            FontSizeBox.SelectedItem = _cfg.FontSize;
            SpellCheckMenuItem.IsChecked = _cfg.SpellCheckEnabled;
        }

        private void ToggleProp(DependencyProperty prop, object on, object off)
        {
            var sel = RichBox.Selection;
            var range = sel.IsEmpty ? new TextRange(RichBox.Document.ContentStart, RichBox.Document.ContentEnd) : sel;
            var current = range.GetPropertyValue(prop);
            range.ApplyPropertyValue(prop, current.Equals(on) ? off : on);
            _mod = true; UpdateStar();
        }

        private void ToggleUnderline()
        {
            var sel = RichBox.Selection;
            var range = sel.IsEmpty ? new TextRange(RichBox.Document.ContentStart, RichBox.Document.ContentEnd) : sel;
            var current = range.GetPropertyValue(Inline.TextDecorationsProperty);
            range.ApplyPropertyValue(Inline.TextDecorationsProperty, current == TextDecorations.Underline ? null : TextDecorations.Underline);
            _mod = true; UpdateStar();
        }

        private void ShowColorPicker(bool isForeground)
        {
            int cols = 5, rows = 4, btnW = 56, btnH = 44, pad = 8, gap = 4;
            double winW = cols * (btnW + gap) + pad * 2 + 14;
            double winH = rows * (btnH + gap) + pad * 2 + 38;

            var win = new Window
            {
                Title = isForeground ? "Цвет текста" : "Цвет холста",
                Width = winW, Height = winH,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this, ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Manual
            };

            var p = new WrapPanel { Width = winW - 16, Margin = new Thickness(pad) };

            foreach (var ci in _colors)
            {
                var bb = (ci.Color == Colors.White || ci.Color == Color.FromRgb(255, 255, 0)) ? Brushes.Gray : Brushes.Transparent;
                var b = new Button
                {
                    Width = btnW, Height = btnH, Margin = new Thickness(gap / 2),
                    Background = new SolidColorBrush(ci.Color),
                    BorderBrush = bb, BorderThickness = new Thickness(1),
                    ToolTip = ci.Name, Cursor = Cursors.Hand
                };
                var c = ci.Color;
                b.Click += (_, _) =>
                {
                    if (isForeground) { _cfg.FontColor = c; RichBox.Foreground = new SolidColorBrush(c); }
                    else { _cfg.BgColor = c; RichBox.Background = new SolidColorBrush(c); }
                    _mod = true; UpdateStar(); win.Close();
                };
                p.Children.Add(b);
            }
            win.Content = p;
            win.ShowDialog();
        }

        private string GetConfigDir()
        {
            string dir = string.IsNullOrEmpty(_fp) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : Path.GetDirectoryName(_fp)!;
            string configDir = Path.Combine(dir, "ClearConfig", "ClearWord");
            Directory.CreateDirectory(configDir);
            return configDir;
        }

        private string GetConfigPath()
        {
            string name = string.IsNullOrEmpty(_fp) ? "untitled" : Path.GetFileNameWithoutExtension(_fp);
            return Path.Combine(GetConfigDir(), name + ".clew.config");
        }

        private void SaveConfig()
        {
            _cfg.Language = _lang.L;
            _cfg.SpellCheckEnabled = RichBox.SpellCheck.IsEnabled;
            File.WriteAllText(GetConfigPath(), JsonSerializer.Serialize(_cfg, new JsonSerializerOptions { WriteIndented = true }));
        }

        private void LoadConfig()
        {
            string cp = GetConfigPath();
            if (File.Exists(cp))
            {
                try { _cfg = JsonSerializer.Deserialize<WordConfig>(File.ReadAllText(cp)) ?? new WordConfig(); }
                catch { _cfg = new WordConfig(); }
            }
            else _cfg = new WordConfig();
            _lang.L = _cfg.Language;
            ApplyConfig();
            SetLang(_lang.L == "en-US");
        }

        private string Encrypt(string text)
        {
            using Aes aes = Aes.Create(); aes.Key = _key; aes.GenerateIV();
            byte[] iv = aes.IV;
            using var enc = aes.CreateEncryptor();
            byte[] plain = Encoding.UTF8.GetBytes(text);
            byte[] cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
            byte[] result = new byte[iv.Length + cipher.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, result, iv.Length, cipher.Length);
            return Convert.ToHexString(result);
        }

        private string Decrypt(string hex)
        {
            byte[] data = Convert.FromHexString(hex);
            using Aes aes = Aes.Create(); aes.Key = _key; aes.IV = data[..16];
            using var dec = aes.CreateDecryptor();
            byte[] plain = dec.TransformFinalBlock(data[16..], 0, data[16..].Length);
            return Encoding.UTF8.GetString(plain);
        }

        private void NewDoc()
        {
            if (_mod) { var r = MessageBox.Show(_lang.S("SavePrompt"), "ClearWord", MessageBoxButton.YesNoCancel); if (r == MessageBoxResult.Yes) Save(); else if (r == MessageBoxResult.Cancel) return; }
            RichBox.Document = new FlowDocument();
            _fp = null; _mod = false; _cfg = new WordConfig();
            ApplyConfig(); UpdateStar(); UpdateWordCount();
        }

        private void Save()
        {
            if (_fp == null) { SaveAs(); return; }
            var range = new TextRange(RichBox.Document.ContentStart, RichBox.Document.ContentEnd);
            using var ms = new MemoryStream();
            range.Save(ms, DataFormats.Rtf);
            File.WriteAllText(_fp, Encrypt(Encoding.UTF8.GetString(ms.ToArray())));
            SaveConfig();
            _mod = false; UpdateStar();
        }

        private void SaveAs()
        {
            var d = new SaveFileDialog { Filter = "Clear Word (*.clew)|*.clew", DefaultExt = "clew", FileName = _lang.S("Untitled"), Title = _lang.S("SaveTitle") };
            if (d.ShowDialog() == true) { _fp = d.FileName; Save(); }
        }

        private void Open()
        {
            if (_mod) { var r = MessageBox.Show(_lang.S("SavePrompt"), "ClearWord", MessageBoxButton.YesNoCancel); if (r == MessageBoxResult.Yes) Save(); else if (r == MessageBoxResult.Cancel) return; }
            var d = new OpenFileDialog { Filter = "Clear Word (*.clew)|*.clew", Title = _lang.S("OpenTitle") };
            if (d.ShowDialog() == true)
            {
                _fp = d.FileName;
                LoadConfig();
                string rtf = Decrypt(File.ReadAllText(_fp));
                var range = new TextRange(RichBox.Document.ContentStart, RichBox.Document.ContentEnd);
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(rtf));
                range.Load(ms, DataFormats.Rtf);
                _mod = false; UpdateStar(); UpdateWordCount();
            }
        }

        private void UpdateStar()
        {
            Title = (_fp == null ? _lang.S("Untitled") : Path.GetFileNameWithoutExtension(_fp)) + ".clew" + (_mod ? "*" : "");
            ModifiedIndicator.Text = _mod ? "●" : "";
        }

        private void UpdateWordCount()
        {
            string text = new TextRange(RichBox.Document.ContentStart, RichBox.Document.ContentEnd).Text;
            int count = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            WordCountText.Text = string.Format(_lang.S("Words"), count);
        }

        private void SetLang(bool en)
        {
            _lang.L = en ? "en-US" : "ru-RU";
            EnglishMenuItem.IsChecked = en; RussianMenuItem.IsChecked = !en;
            RichBox.Language = System.Windows.Markup.XmlLanguage.GetLanguage(_lang.L);

            FileMenu.Header = _lang.S("File"); NewMenuItem.Header = _lang.S("New"); OpenMenuItem.Header = _lang.S("Open");
            SaveMenuItem.Header = _lang.S("Save"); SaveAsMenuItem.Header = _lang.S("SaveAs"); ExitMenuItem.Header = _lang.S("Exit");
            EditMenu.Header = _lang.S("Edit"); UndoMenuItem.Header = _lang.S("Undo"); RedoMenuItem.Header = _lang.S("Redo");
            CutMenuItem.Header = _lang.S("Cut"); CopyMenuItem.Header = _lang.S("Copy"); PasteMenuItem.Header = _lang.S("Paste");
            SelectAllMenuItem.Header = _lang.S("SelectAll");
            SettingsMenu.Header = _lang.S("Settings"); SpellCheckMenuItem.Header = _lang.S("SpellCheck");
            GitHubMenuItem.Header = _lang.S("GitHub");
            LanguageMenu.Header = _lang.S("Language"); EnglishMenuItem.Header = _lang.S("English"); RussianMenuItem.Header = _lang.S("Russian");
            BoldBtn.ToolTip = _lang.S("Bold"); ItalicBtn.ToolTip = _lang.S("Italic"); UnderBtn.ToolTip = _lang.S("Underline");
            FontColorBtn.ToolTip = _lang.S("FontColor"); BgColorBtn.ToolTip = _lang.S("BgColor");
            LangStatusText.Text = _lang.S("LangStatus");
            UpdateStar();
        }
    }
}