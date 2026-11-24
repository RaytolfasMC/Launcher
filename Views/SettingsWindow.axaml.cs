using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace RaytolfasLauncher
{
    public partial class SettingsWindow : Window
    {
        private readonly LauncherSettings settings;
        private readonly List<JavaProfile> javaProfiles;
        private bool isManualEditing;
        public bool SettingsSaved { get; private set; }

        private static string cachedGithubUrl = "https://github.com/RaytolfasMC/Launcher";
        private const string RepoJsonUrl = "https://raw.githubusercontent.com/Raytolfas/Assets/refs/heads/main/RaytolfasLauncherMC/repo.json";

        public SettingsWindow() : this(new LauncherSettings(), new List<JavaProfile>())
        {
        }

        public SettingsWindow(LauncherSettings settings, List<JavaProfile> javaProfiles)
        {
            InitializeComponent();
            this.settings = settings;
            this.javaProfiles = javaProfiles;

            RefreshRepoUrlAsync();

            this.Loaded += (s, e) => UpdateContentWidth();
            if (SettingsScrollViewer != null)
            {
                SettingsScrollViewer.SizeChanged += (s, e) => UpdateContentWidth();
            }

            InitializeValues();
        }

        private void UpdateContentWidth()
        {
            if (SettingsScrollViewer == null) return;
            double w = SettingsScrollViewer.Bounds.Width;
            if (w <= 0) return;
            double contentWidth = Math.Max(200, w - 40);

            if (SectionGeneral != null) SectionGeneral.Width = contentWidth;
            if (SectionJava != null) SectionJava.Width = contentWidth;
            if (SectionGame != null) SectionGame.Width = contentWidth;
            if (SectionAppearance != null) SectionAppearance.Width = contentWidth;
            if (SectionAbout != null) SectionAbout.Width = contentWidth;
        }

        private void InitializeValues()
        {
            RamSlider.Value = settings.SelectedRam;
            RamValueText.Text = settings.SelectedRam + " MB";
            RamInputBox.Text = settings.SelectedRam.ToString();
            RamSlider.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == nameof(Slider.Value) && !isManualEditing)
                {
                    int rawVal = (int)Math.Round(RamSlider.Value);
                    int snappedVal = (int)Math.Round(rawVal / 512.0) * 512;
                    if (snappedVal < 1024) snappedVal = 1024;
                    if (Math.Abs(RamSlider.Value - snappedVal) > 1)
                    {
                        RamSlider.Value = snappedVal;
                        return;
                    }
                    RamValueText.Text = snappedVal + " MB";
                    RamInputBox.Text = snappedVal.ToString();
                }
            };

            PathBox.Text = string.IsNullOrWhiteSpace(settings.MinecraftPath)
                ? PlatformHelper.GetDefaultMinecraftFolder()
                : settings.MinecraftPath;

            BgPathBox.Text = settings.CustomBackgroundPath ?? "";
            JavaArgsBox.Text = settings.JvmArgs ?? "-XX:+UseG1GC";
            JavaPathBox.Text = settings.JavaPath ?? "";

            WinWidthBox.Text = settings.WindowWidth.ToString();
            WinHeightBox.Text = settings.WindowHeight.ToString();
            CbFullScreen.IsChecked = settings.IsFullScreen;

            CbHideLauncher.IsChecked = settings.HideLauncherOnPlay;
            CbShowAvatar.IsChecked = settings.ShowAccountAvatar;
            CbOpenLogWindow.IsChecked = settings.OpenLogWindowOnLaunch;
            CbDiscordRPC.IsChecked = settings.ShowDiscordStatus;
            CbElyBySkins.IsChecked = settings.EnableElyBySkins;

            CbReleases.IsChecked = settings.ShowReleases;
            CbSnapshots.IsChecked = settings.ShowSnapshots;
            CbModded.IsChecked = settings.ShowModded;

            UiScaleSlider.Value = settings.UiScaleIndex;
            UiScaleSlider.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == nameof(Slider.Value))
                {
                    int idx = (int)Math.Round(UiScaleSlider.Value);
                    UpdateUiScaleText(idx);
                }
            };
            UpdateUiScaleText(settings.UiScaleIndex);

            InitializeLanguages();
            InitializeJavaProfiles();
            ApplyLocalization(settings.Language);
        }

        private void InitializeLanguages()
        {
            var languages = LocalizationManager.SupportedLanguages;
            LanguageBox.ItemsSource = languages.Select(l => l.DisplayName).ToList();

            int selectedIndex = 0;
            for (int i = 0; i < languages.Count; i++)
            {
                if (string.Equals(languages[i].Code, settings.Language, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (LanguageBox.Items.Count > 0)
                LanguageBox.SelectedIndex = selectedIndex;

            LanguageBox.SelectionChanged += (s, e) =>
            {
                int idx = LanguageBox.SelectedIndex;
                if (idx >= 0 && idx < languages.Count)
                {
                    ApplyLocalization(languages[idx].Code);
                }
            };
        }

        private void ApplyLocalization(string langCode)
        {
            string lang = LocalizationManager.NormalizeLanguage(langCode);
            string T(string key) => LocalizationManager.Get(key, lang);

            Title = T("settings.title");
            if (SettingsTitleText != null) SettingsTitleText.Text = T("settings.title");

            if (TabGeneralText != null) TabGeneralText.Text = T("settings.tab.general");
            if (TabJavaText != null) TabJavaText.Text = T("settings.tab.java");
            if (TabGameText != null) TabGameText.Text = T("settings.tab.game");
            if (TabAppearanceText != null) TabAppearanceText.Text = T("settings.tab.appearance");
            if (TabAboutText != null) TabAboutText.Text = T("settings.tab.about");

            if (LanguageLabel != null) LanguageLabel.Text = T("settings.general.language");
            if (BehaviorLabel != null) BehaviorLabel.Text = T("settings.general.behavior");
            if (CbHideLauncher != null) CbHideLauncher.Content = T("settings.general.hide_launcher");
            if (CbShowAvatar != null) CbShowAvatar.Content = T("settings.general.show_avatar");
            if (CbOpenLogWindow != null) CbOpenLogWindow.Content = T("settings.general.open_logs");
            if (CbDiscordRPC != null) CbDiscordRPC.Content = T("settings.general.discord");

            if (RamTitleLabel != null) RamTitleLabel.Text = T("settings.java.ram_title");
            if (JavaProfileLabel != null) JavaProfileLabel.Text = T("settings.java.profile_title");
            if (JavaPathLabel != null) JavaPathLabel.Text = T("settings.java.path_hint");
            if (BrowseJavaButton != null) BrowseJavaButton.Content = T("common.browse");
            if (JavaArgsLabel != null) JavaArgsLabel.Text = T("settings.java.args_title");

            if (GameFolderLabel != null) GameFolderLabel.Text = T("settings.game.folder_title");
            if (BrowsePathButton != null) BrowsePathButton.Content = T("common.browse");
            if (ResetPathButton != null) ResetPathButton.Content = T("common.reset");
            if (ResolutionLabel != null) ResolutionLabel.Text = T("settings.game.resolution_title");
            if (CbFullScreen != null) CbFullScreen.Content = T("settings.game.fullscreen");
            if (VersionFiltersLabel != null) VersionFiltersLabel.Text = T("settings.game.versions_title");
            if (CbReleases != null) CbReleases.Content = T("settings.game.releases");
            if (CbSnapshots != null) CbSnapshots.Content = T("settings.game.snapshots");
            if (CbModded != null) CbModded.Content = T("settings.game.modded");
            if (SkinsLabel != null) SkinsLabel.Text = T("settings.game.skins_title");
            if (CbElyBySkins != null) CbElyBySkins.Content = T("settings.game.elyby");

            if (UiScaleLabel != null) UiScaleLabel.Text = T("settings.appearance.ui_scale");
            if (UiScaleSmallLabel != null) UiScaleSmallLabel.Text = T("settings.appearance.scale_small");
            if (UiScaleStandardLabel != null) UiScaleStandardLabel.Text = T("settings.appearance.scale_standard");
            if (UiScaleLargeLabel != null) UiScaleLargeLabel.Text = T("settings.appearance.scale_large");
            UpdateUiScaleText((int)Math.Round(UiScaleSlider?.Value ?? 1.0));

            if (BackgroundLabel != null) BackgroundLabel.Text = T("settings.appearance.bg_title");
            if (SelectBackgroundButton != null) SelectBackgroundButton.Content = T("common.choose");
            if (ResetBackgroundButton != null) ResetBackgroundButton.Content = T("common.reset");

            if (PoweredByLabel != null) PoweredByLabel.Text = T("about.powered_by");
            if (WebsiteBtnText != null) WebsiteBtnText.Text = T("about.website");
            if (GithubBtnText != null) GithubBtnText.Text = T("about.github");

            if (CancelButton != null) CancelButton.Content = T("common.cancel");
            if (SaveButton != null) SaveButton.Content = T("common.save");
        }

        private void InitializeJavaProfiles()
        {
            JavaProfileBox.ItemsSource = javaProfiles.Select(p => p.Name).ToList();

            int selectedIndex = 0;
            for (int i = 0; i < javaProfiles.Count; i++)
            {
                if (string.Equals(javaProfiles[i].Id, settings.SelectedJavaProfileId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (JavaProfileBox.Items.Count > 0)
                JavaProfileBox.SelectedIndex = selectedIndex;

            JavaProfileBox.SelectionChanged += (s, e) =>
            {
                int idx = JavaProfileBox.SelectedIndex;
                if (idx < 0 || idx >= javaProfiles.Count) return;
                var p = javaProfiles[idx];
                if (p.Id == "auto")
                {
                    JavaPathBox.Text = "";
                    JavaPathBox.IsReadOnly = true;
                }
                else if (p.Id == "custom")
                {
                    JavaPathBox.IsReadOnly = false;
                    JavaPathBox.Text = settings.JavaPath ?? "";
                }
                else
                {
                    JavaPathBox.IsReadOnly = true;
                    JavaPathBox.Text = p.JavaPath ?? "";
                }
            };
        }

        private void Tab_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                SwitchTab(tag);
            }
        }

        private void SwitchTab(string tag)
        {
            SectionGeneral.IsVisible = tag == "general";
            SectionJava.IsVisible = tag == "java";
            SectionGame.IsVisible = tag == "game";
            SectionAppearance.IsVisible = tag == "appearance";
            SectionAbout.IsVisible = tag == "about";

            UpdateNavBtn(TabGeneralBtn, tag == "general");
            UpdateNavBtn(TabJavaBtn, tag == "java");
            UpdateNavBtn(TabGameBtn, tag == "game");
            UpdateNavBtn(TabAppearanceBtn, tag == "appearance");
            UpdateNavBtn(TabAboutBtn, tag == "about");
        }

        private void UpdateNavBtn(Button btn, bool active)
        {
            if (active)
            {
                if (!btn.Classes.Contains("active"))
                    btn.Classes.Add("active");
            }
            else
            {
                btn.Classes.Remove("active");
            }
        }

        private async void SelectPath_Click(object? sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Выберите папку установки Minecraft",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                PathBox.Text = folders[0].Path.LocalPath;
            }
        }

        private void ResetPath_Click(object? sender, RoutedEventArgs e)
        {
            PathBox.Text = PlatformHelper.GetDefaultMinecraftFolder();
        }

        private async void SelectJavaPath_Click(object? sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите исполняемый файл Java",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Java Executable")
                    {
                        Patterns = PlatformHelper.IsWindows ? new[] { "javaw.exe", "java.exe", "*.exe" } : new[] { "java", "*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                JavaPathBox.Text = files[0].Path.LocalPath;
            }
        }

        private async void SelectBackground_Click(object? sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите фоновое изображение",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Изображения")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp" }
                    }
                }
            });

            if (files.Count > 0)
            {
                BgPathBox.Text = files[0].Path.LocalPath;
            }
        }

        private void ResetBackground_Click(object? sender, RoutedEventArgs e)
        {
            BgPathBox.Text = "";
        }

        private void UpdateUiScaleText(int index)
        {
            if (UiScaleValueText == null) return;
            string lang = LocalizationManager.SupportedLanguages.Count > LanguageBox.SelectedIndex && LanguageBox.SelectedIndex >= 0
                ? LocalizationManager.SupportedLanguages[LanguageBox.SelectedIndex].Code
                : settings.Language;
            UiScaleValueText.Text = index switch
            {
                0 => $"{LocalizationManager.Get(lang, "settings.appearance.scale_small")} (85%)",
                2 => $"{LocalizationManager.Get(lang, "settings.appearance.scale_large")} (115%)",
                _ => $"{LocalizationManager.Get(lang, "settings.appearance.scale_standard")} (100%)"
            };
            if (UiScaleSmallLabel != null)
                UiScaleSmallLabel.Foreground = index == 0 ? Avalonia.Media.Brushes.White : Avalonia.Media.Brush.Parse("#60646C");
            if (UiScaleStandardLabel != null)
                UiScaleStandardLabel.Foreground = index == 1 ? Avalonia.Media.Brush.Parse("#A250FF") : Avalonia.Media.Brush.Parse("#60646C");
            if (UiScaleLargeLabel != null)
                UiScaleLargeLabel.Foreground = index == 2 ? Avalonia.Media.Brushes.White : Avalonia.Media.Brush.Parse("#60646C");
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            settings.SelectedRam = (int)RamSlider.Value;
            settings.MinecraftPath = (PathBox.Text ?? "").Trim();
            settings.CustomBackgroundPath = (BgPathBox.Text ?? "").Trim();
            settings.UiScaleIndex = (int)Math.Round(UiScaleSlider.Value);
            settings.JvmArgs = (JavaArgsBox.Text ?? "").Trim();

            if (int.TryParse(WinWidthBox.Text, out int w) && w >= 320) settings.WindowWidth = w;
            if (int.TryParse(WinHeightBox.Text, out int h) && h >= 240) settings.WindowHeight = h;
            settings.IsFullScreen = CbFullScreen.IsChecked ?? false;

            settings.HideLauncherOnPlay = CbHideLauncher.IsChecked ?? false;
            settings.ShowAccountAvatar = CbShowAvatar.IsChecked ?? false;
            settings.OpenLogWindowOnLaunch = CbOpenLogWindow.IsChecked ?? false;
            settings.ShowDiscordStatus = CbDiscordRPC.IsChecked ?? false;
            settings.EnableElyBySkins = CbElyBySkins.IsChecked ?? false;

            settings.ShowReleases = CbReleases.IsChecked ?? true;
            settings.ShowSnapshots = CbSnapshots.IsChecked ?? false;
            settings.ShowModded = CbModded.IsChecked ?? true;

            int langIdx = LanguageBox.SelectedIndex;
            if (langIdx >= 0 && langIdx < LocalizationManager.SupportedLanguages.Count)
                settings.Language = LocalizationManager.SupportedLanguages[langIdx].Code;

            int profileIdx = JavaProfileBox.SelectedIndex;
            if (profileIdx >= 0 && profileIdx < javaProfiles.Count)
            {
                var profile = javaProfiles[profileIdx];
                settings.SelectedJavaProfileId = profile.Id;
                if (profile.Id == "auto")
                {
                    settings.JavaPath = "";
                }
                else if (profile.Id == "custom")
                {
                    settings.JavaPath = (JavaPathBox.Text ?? "").Trim();
                    profile.JavaPath = settings.JavaPath;
                }
                else
                {
                    settings.JavaPath = profile.JavaPath;
                }
            }

            settings.Save();
            SettingsSaved = true;
            Close();
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RamValueText_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            RamValueText.IsVisible = false;
            RamInputBox.IsVisible = true;
            RamInputBox.Text = ((int)RamSlider.Value).ToString();
            RamInputBox.Focus();
            RamInputBox.SelectAll();
        }

        private void ApplyManualRam()
        {
            if (int.TryParse(RamInputBox.Text?.Trim(), out int mb) && mb >= 512 && mb <= 65536)
            {
                isManualEditing = true;
                if (mb > RamSlider.Maximum) RamSlider.Maximum = Math.Max(16384, mb);
                if (mb < RamSlider.Minimum) RamSlider.Minimum = Math.Min(1024, mb);
                RamSlider.Value = mb;
                RamValueText.Text = mb + " MB";
                isManualEditing = false;
            }
            else
            {
                RamValueText.Text = ((int)RamSlider.Value) + " MB";
            }
            RamInputBox.IsVisible = false;
            RamValueText.IsVisible = true;
        }

        private void RamInputBox_LostFocus(object? sender, RoutedEventArgs e)
        {
            ApplyManualRam();
        }

        private void RamInputBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyManualRam();
            }
            else if (e.Key == Key.Escape)
            {
                RamInputBox.IsVisible = false;
                RamValueText.IsVisible = true;
            }
        }

        private void OpenRaytolfasSite_Click(object? sender, PointerPressedEventArgs e)
        {
            PlatformHelper.OpenBrowser("https://raytolfas.com");
        }

        private void OpenLauncherSite_Click(object? sender, RoutedEventArgs e)
        {
            PlatformHelper.OpenBrowser("https://launcher.raytolfas.com");
        }

        private static async void RefreshRepoUrlAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
                client.DefaultRequestHeaders.Add("User-Agent", "RaytolfasLauncher/1.1");
                string json = await client.GetStringAsync(RepoJsonUrl);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("url", out var prop))
                {
                    string? val = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                        cachedGithubUrl = val.Trim();
                }
            }
            catch { }
        }

        private void OpenGithub_Click(object? sender, RoutedEventArgs e)
        {
            PlatformHelper.OpenBrowser(cachedGithubUrl);
        }

        private void Header_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        private void ResizeN_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.North, e);
        private void ResizeS_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.South, e);
        private void ResizeW_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.West, e);
        private void ResizeE_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.East, e);
        private void ResizeNW_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.NorthWest, e);
        private void ResizeNE_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.NorthEast, e);
        private void ResizeSW_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.SouthWest, e);
        private void ResizeSE_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.SouthEast, e);

        private void BeginResize(WindowEdge edge, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginResizeDrag(edge, e);
            }
        }
    }
}
