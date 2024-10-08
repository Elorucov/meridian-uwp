﻿using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Jupiter.Mvvm;
using Meridian.Services;
using Meridian.Utils.Helpers;
using Meridian.Utils.Messaging;
using Meridian.View;
using Meridian.View.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using VkLib;
using VkLib.Auth;
using VkLib.Core.Users;
using VkLib.Error;
using Windows.Security.Authentication.Web;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI;
using VkLib.Core.Auth;

namespace Meridian.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        #region Scope settings

        private const VkScopeSettings ScopeSettings =
            VkScopeSettings.CanAccessAudios |
            VkScopeSettings.CanAccessVideos |
            VkScopeSettings.CanAccessFriends |
            VkScopeSettings.CanAccessGroups |
            VkScopeSettings.CanAccessWall |
            VkScopeSettings.CanAccessStatus |
            VkScopeSettings.CanAccessPhotos;

        #endregion

        private Vk _vk;

        private string _login;
        private string _password;
        private bool _isLoginFormVisible;
        private bool _isLoginStartVisible = true;
        private bool _isLoginFormInputEnabled = true;

        private bool _isCaptchaVisible;
        private string _captchaImg;
        private string _captchaSid;
        private string _captchaKey;

        private VkProfile _user;
        private string _welcomeText = Resources.GetStringByKey("Login_Start");

        #region Commands

        public DelegateCommand SignInStartCommand { get; private set; }

        public DelegateCommand LoginCommand { get; private set; }

        public DelegateCommand SignUpCommand { get; private set; }

        #endregion

        public string Login
        {
            get { return _login; }
            set { Set(ref _login, value); }
        }

        public string Password
        {
            get { return _password; }
            set { Set(ref _password, value); }
        }

        public bool IsCaptchaVisible
        {
            get { return _isCaptchaVisible; }
            private set { Set(ref _isCaptchaVisible, value); }
        }

        public string CaptchaImg
        {
            get { return _captchaImg; }
            private set { Set(ref _captchaImg, value); }
        }

        public string CaptchaKey
        {
            get { return _captchaKey; }
            set { Set(ref _captchaKey, value); }
        }

        public VkProfile User
        {
            get { return _user; }
            set { Set(ref _user, value); }
        }

        public string WelcomeText
        {
            get { return _welcomeText; }
            set { Set(ref _welcomeText, value); }
        }

        public bool IsLoginFormVisible
        {
            get { return _isLoginFormVisible; }
            set { Set(ref _isLoginFormVisible, value); }
        }

        public bool IsLoginStartVisible
        {
            get { return _isLoginStartVisible; }
            set { Set(ref _isLoginStartVisible, value); }
        }

        public bool IsLoginFormInputEnabled
        {
            get { return _isLoginFormInputEnabled; }
            set { Set(ref _isLoginFormInputEnabled, value); }
        }

        public LoginViewModel()
        {
            _vk = Ioc.Resolve<Vk>();

            RegisterTasks("login");
        }

        protected override void InitializeCommands()
        {
            SignInStartCommand = new DelegateCommand(() =>
            {
                IsLoginFormVisible = true;
                IsLoginStartVisible = false;
                IsLoginFormInputEnabled = true;
            });

            LoginCommand = new DelegateCommand(DoLogin);

            SignUpCommand = new DelegateCommand(OpenLoginByTokenDialog);
        }

        private async void DoLogin()
        {
            if (Operations["login"].IsWorking)
                return;

            var t = TaskStarted("login");

            IsLoginFormInputEnabled = false;

            try
            {
                IsCaptchaVisible = false;

                var result = await _vk.Auth.Login(Login, Password, ScopeSettings, _captchaSid, _captchaKey);
                if (!string.IsNullOrEmpty(result.Token))
                {
                    AppState.VkToken = result;

                    await OnLogin();
                }
            }
            catch (VkCaptchaNeededException ex)
            {
                Logger.Info("Unable to login. Captcha needed.");

                IsCaptchaVisible = true;
                _captchaSid = ex.CaptchaSid;
                CaptchaImg = ex.CaptchaImg;
            }
            catch (VkInvalidClientException ex)
            {
                Logger.Error(ex, "Unable to login. Invalid client.");

                t.Error = Resources.GetStringByKey("Login_InvalidClient");
            }
            catch (VkNeedValidationException ex)
            {
                Logger.Info("Validation requred");

                t.Error = Resources.GetStringByKey("Login_ValidationRequired");

                ValidateUser(ex.RedirectUri);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to login");

                t.Error = Resources.GetStringByKey("Login_CommonError");
            }
            finally
            {
                IsLoginFormInputEnabled = true;
                t.Finish();
            }
        }

        private async void ValidateUser(Uri redirectUri)
        {
            var result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, redirectUri, new Uri("https://oauth.vk.com/blank.html"));
            if (result.ResponseStatus == WebAuthenticationStatus.Success)
            {
                var token = _vk.OAuth.ProcessAuth(new Uri(result.ResponseData));
                if (token != null && token.AccessToken != null && token.AccessToken.Token != null)
                {
                    TaskError("login", null);

                    token.AccessToken.ExpiresIn = DateTime.MaxValue;

                    _vk.AccessToken = token.AccessToken;
                    AppState.VkToken = token.AccessToken;

                    await OnLogin();
                }
            }
        }

        private async Task OnLogin()
        {
            IsLoginFormVisible = false;

            /*var p = _vk.LoginParams ?? new Dictionary<string, string>()
            {
                ["version"] = "4.11.1",
                ["func_v"] = "5"
            };

            p["userId"] = _vk.AccessToken.UserId.ToString();

            await _vk.Execute.GetBaseData(_vk.LoginParams);*/

            if (User == null) User = await _vk.Users.Get(userId: _vk.AccessToken.UserId, fields: "photo,photo_100");

            WelcomeText = string.Format(Resources.GetStringByKey("Login_Welcome"), User.FirstName);

            Messenger.Default.Send(new MessageUserAuthChanged { IsLoggedIn = true });

            await Task.Delay(1000);

            NavigationService.Navigate(typeof(MyMusicView), clearHistory: true);
        }

        private async void OpenLoginByTokenDialog() {
            StackPanel content = new StackPanel();

            PasswordBox at = new PasswordBox {
                Margin = new Thickness(0, 0, 0, 12)
            };

            TextBlock err = new TextBlock {
                Foreground = new SolidColorBrush(Colors.Red),
                TextWrapping = TextWrapping.Wrap
            };

            content.Children.Add(new TextBlock { TextWrapping = TextWrapping.Wrap, Text = "Введите access token от официального приложения ВК (мобильные клиенты либо десктопный VK Мессенджер)." });
            content.Children.Add(at);
            content.Children.Add(err);

            ContentDialog dlg = new ContentDialog {
                Title = "Auth with token",
                PrimaryButtonText = "Auth",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = content
            };

            dlg.PrimaryButtonClick += async (a, b) => {
                b.Cancel = true;
                dlg.IsPrimaryButtonEnabled = false;
                dlg.IsSecondaryButtonEnabled = false;
                User = await CheckTokenAsync(at.Password, err);
                if (User != null) {
                    dlg.Hide();

                    VkAccessToken token = new VkAccessToken {
                        UserId = User.Id,
                        Token = at.Password,
                        ExpiresIn = DateTime.MaxValue
                    };

                    _vk.AccessToken = token;
                    AppState.VkToken = token;

                    await OnLogin();
                } else {
                    _vk.AccessToken = null;
                    dlg.IsPrimaryButtonEnabled = true;
                    dlg.IsSecondaryButtonEnabled = true;
                }
            };

            await dlg.ShowAsync();
        }

        private async Task<VkProfile> CheckTokenAsync(string token, TextBlock err) {
            try {
                err.Text = "Checking access to audio API...";

                VkAccessToken at = new VkAccessToken {
                    Token = token
                };

                _vk.AccessToken = at;

                var user = await _vk.Users.Get(fields: "photo,photo_100");
                var result = await _vk.Audio.Get(user.Id, count: 1);
                return user;
            } catch (VkInvalidTokenException vktex) {
                err.Text = $"Invalid token!";
                return null;
            } catch (VkException vkex) {
                err.Text = $"VK API Error: {vkex.Error}";
                return null;
            } catch (Exception ex) {
                err.Text = $"Exception (0x{ex.HResult.ToString("x8")}): {ex.Message}";
                return null;
            }
        }
    }
}