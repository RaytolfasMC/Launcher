// Copyright (C) 2026 Raytolfas
// This file is part of Raytolfas Launcher.

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using XboxAuthNet.OAuth.CodeFlow;

namespace RaytolfasLauncher
{
    public partial class MicrosoftLoginDialog : Window
    {
        private readonly string _authUrl;
        private readonly ICodeFlowUrlChecker _uriChecker;
        private readonly string _language;

        public CodeFlowAuthorizationResult? AuthResult { get; private set; }

        public MicrosoftLoginDialog() : this("https://login.live.com", new CodeFlowUrlChecker(), LocalizationManager.DefaultLanguage) { }

        public MicrosoftLoginDialog(string authUrl, ICodeFlowUrlChecker uriChecker, string language = LocalizationManager.DefaultLanguage)
        {
            InitializeComponent();
            _authUrl = authUrl;
            _uriChecker = uriChecker;
            _language = LocalizationManager.NormalizeLanguage(language);

            Title = LocalizationManager.Get("account.microsoft_dialog_title", _language);
            TitleText.Text = LocalizationManager.Get("account.microsoft_dialog_title", _language).ToUpperInvariant();
            Step1Text.Text = LocalizationManager.Get("account.microsoft_step1", _language);
            Step2Text.Text = LocalizationManager.Get("account.microsoft_step2", _language);
            PasteButton.Content = LocalizationManager.Get("account.microsoft_paste_button", _language);
            OpenBrowserButton.Content = LocalizationManager.Get("account.microsoft_open_browser", _language);
            CancelButton.Content = LocalizationManager.Get("account.cancel", _language);
            ConfirmButton.Content = LocalizationManager.Get("account.use", _language);

            Activated += OnActivated;
            UrlTextBox.AttachedToVisualTree += (s, e) => UrlTextBox.Focus();
        }

        private async void OnActivated(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UrlTextBox.Text))
                return;

            try
            {
                if (Clipboard != null)
                {
                    string? text = await Clipboard.GetTextAsync();
                    if (!string.IsNullOrWhiteSpace(text) && (text.Contains("oauth20_desktop.srf") || text.StartsWith("code=")))
                    {
                        UrlTextBox.Text = text.Trim();
                    }
                }
            }
            catch
            {
            }
        }

        private async void Paste_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard != null)
                {
                    string? text = await Clipboard.GetTextAsync();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        UrlTextBox.Text = text.Trim();
                        Confirm_Click(sender, e);
                    }
                }
            }
            catch
            {
            }
        }

        private void OpenBrowser_Click(object? sender, RoutedEventArgs e)
        {
            PlatformHelper.OpenBrowser(_authUrl);
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            AuthResult = null;
            Close(false);
        }

        private void UrlTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Confirm_Click(sender, e);
            }
        }

        private void Confirm_Click(object? sender, RoutedEventArgs e)
        {
            string input = UrlTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(input))
                return;

            Uri? targetUri = null;
            if (Uri.TryCreate(input, UriKind.Absolute, out var parsed) && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
            {
                targetUri = parsed;
            }
            else if (input.StartsWith("code=", StringComparison.OrdinalIgnoreCase))
            {
                targetUri = new Uri("https://login.live.com/oauth20_desktop.srf?" + input);
            }
            else if (!input.Contains('/') && !input.Contains(' '))
            {
                targetUri = new Uri("https://login.live.com/oauth20_desktop.srf?code=" + input);
            }

            if (targetUri == null)
            {
                ShowError(LocalizationManager.Get("account.microsoft_invalid_url", _language));
                return;
            }

            var result = _uriChecker.GetAuthCodeResult(targetUri);
            if (result.IsSuccess)
            {
                AuthResult = result;
                Close(true);
            }
            else
            {
                string msg = !string.IsNullOrEmpty(result.ErrorDescription)
                    ? result.ErrorDescription
                    : (!string.IsNullOrEmpty(result.Error) ? result.Error : LocalizationManager.Get("account.microsoft_invalid_url", _language));
                ShowError(msg);
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.IsVisible = true;
        }
    }
}
