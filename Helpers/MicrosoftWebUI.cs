// Copyright (C) 2026 Raytolfas
// This file is part of Raytolfas Launcher.

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using XboxAuthNet.Game;
using XboxAuthNet.Game.Authenticators;
using XboxAuthNet.Game.OAuth;
using XboxAuthNet.OAuth.CodeFlow;

namespace RaytolfasLauncher
{
    public class MicrosoftOAuthProvider : IAuthenticationProvider
    {
        private readonly MicrosoftOAuthBuilder _oauth;
        private readonly IWebUI _webUI;

        public MicrosoftOAuthProvider(IWebUI webUI)
        {
            _webUI = webUI;
            _oauth = new MicrosoftOAuthBuilder(CmlLib.Core.Auth.Microsoft.JELoginHandler.DefaultMicrosoftOAuthClientInfo);
        }

        public IAuthenticator Authenticate() => _oauth.CodeFlow(b => b.WithWebUI(_webUI));
        public IAuthenticator AuthenticateInteractively() => _oauth.Interactive(b => b.WithWebUI(_webUI));
        public IAuthenticator AuthenticateSilently() => _oauth.Silent();
        public IAuthenticator ClearSession() => _oauth.Signout();
        public IAuthenticator Signout() => _oauth.Signout();
        public ISessionValidator CreateSessionValidator() => _oauth.Validator();
    }

    public class MicrosoftWebUI : IWebUI
    {
        private readonly Window _parent;
        private readonly string _language;

        public MicrosoftWebUI(Window parent, string language)
        {
            _parent = parent;
            _language = language;
        }

        public async Task<CodeFlowAuthorizationResult> DisplayDialogAndInterceptUri(
            Uri uri,
            ICodeFlowUrlChecker uriChecker,
            CancellationToken cancellationToken)
        {
            PlatformHelper.OpenBrowser(uri.AbsoluteUri);

            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new MicrosoftLoginDialog(uri.AbsoluteUri, uriChecker, _language);
                bool? result = await dialog.ShowDialog<bool?>(_parent);

                if (result != true || dialog.AuthResult == null || !dialog.AuthResult.Value.IsSuccess)
                {
                    throw new OperationCanceledException("Microsoft authentication was cancelled.");
                }

                return dialog.AuthResult.Value;
            });
        }

        public Task DisplayDialogAndNavigateUri(Uri uri, CancellationToken cancellationToken)
        {
            PlatformHelper.OpenBrowser(uri.AbsoluteUri);
            return Task.CompletedTask;
        }
    }
}
