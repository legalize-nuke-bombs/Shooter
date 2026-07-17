using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Auth;

namespace Shooter.Menu
{
    public class LoginScreen
    {
        private readonly VisualElement screen;
        private readonly VisualElement confirmBlock;
        private readonly VisualElement displayNameBlock;
        private readonly TextField usernameField;
        private readonly TextField passwordField;
        private readonly TextField confirmField;
        private readonly TextField displayNameField;
        private readonly Button submitBtn;
        private readonly Button modeLink;
        private readonly Label formTitle;
        private readonly Label status;

        private readonly MenuApi api;
        private readonly Action onLoggedIn;

        private bool registerMode = true;
        private bool busy;

        public LoginScreen(VisualElement root, MenuApi api, Action onLoggedIn)
        {
            this.api = api;
            this.onLoggedIn = onLoggedIn;

            screen = root.Q<VisualElement>("login-screen");
            confirmBlock = root.Q<VisualElement>("confirm-block");
            displayNameBlock = root.Q<VisualElement>("displayname-block");
            usernameField = root.Q<TextField>("username-field");
            passwordField = root.Q<TextField>("password-field");
            confirmField = root.Q<TextField>("confirm-field");
            displayNameField = root.Q<TextField>("displayname-field");
            submitBtn = root.Q<Button>("submit-btn");
            modeLink = root.Q<Button>("mode-link");
            formTitle = root.Q<Label>("form-title");
            status = root.Q<Label>("login-status");

            usernameField.value = PlayerPrefs.GetString("username", "");

            modeLink.clicked += () => SetRegisterMode(!registerMode);
            submitBtn.clicked += Submit;
            passwordField.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Return && !registerMode) Submit(); });
            confirmField.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Return) Submit(); });

            if (usernameField.value.Length > 0)
                SetRegisterMode(false);
        }

        public void Show() => screen.RemoveFromClassList("hidden");
        public void Hide() => screen.AddToClassList("hidden");

        private void SetRegisterMode(bool register)
        {
            registerMode = register;
            formTitle.text = register ? "РЕГИСТРАЦИЯ" : "ВХОД";
            submitBtn.text = register ? "СОЗДАТЬ АККАУНТ" : "ВОЙТИ";
            modeLink.text = register ? "Уже есть аккаунт? Войти" : "Нет аккаунта? Создать";
            if (register)
            {
                confirmBlock.RemoveFromClassList("hidden");
                displayNameBlock.RemoveFromClassList("hidden");
            }
            else
            {
                confirmBlock.AddToClassList("hidden");
                displayNameBlock.AddToClassList("hidden");
            }
            status.text = "";
        }

        private void Submit()
        {
            if (busy) return;
            status.text = "";

            string username = usernameField.value.Trim();
            string password = passwordField.value;
            string displayName = registerMode ? displayNameField.value.Trim() : username;

            if (!Regex.IsMatch(username, "^[a-zA-Z0-9_]{4,20}$")) { status.text = "Имя пользователя: 4-20 символов, латиница, цифры, подчёркивание"; return; }
            if (password.Length < 8 || password.Length > 40) { status.text = "Пароль: 8-40 символов"; return; }
            bool upper = false, lower = false, digit = false;
            foreach (char c in password)
            {
                if (char.IsUpper(c)) upper = true;
                else if (char.IsLower(c)) lower = true;
                else if (char.IsDigit(c)) digit = true;
            }
            if (!upper || !lower || !digit) { status.text = "Пароль должен содержать заглавную и строчную буквы и цифру"; return; }
            if (registerMode)
            {
                if (passwordField.value != confirmField.value) { status.text = "Пароли не совпадают"; return; }
                if (displayName.Length < 1 || displayName.Length > 40) { status.text = "Отображаемое имя: 1-40 символов"; return; }
            }

            busy = true;
            submitBtn.SetEnabled(false);

            Action<string, string> onDone = (token, error) =>
            {
                if (error != null)
                {
                    busy = false;
                    submitBtn.SetEnabled(true);
                    status.text = error;
                    return;
                }

                Session.Token = token;
                api.Me((me, meError) =>
                {
                    busy = false;
                    submitBtn.SetEnabled(true);

                    if (meError != null)
                    {
                        Session.Token = "";
                        status.text = meError;
                        return;
                    }

                    Session.Username = username;
                    Session.DisplayName = me.displayName;
                    Session.UserId = me.id;

                    PlayerPrefs.SetString("username", username);
                    PlayerPrefs.Save();

                    status.text = "";
                    onLoggedIn();
                });
            };

            if (registerMode) api.Register(username, displayName, password, onDone);
            else api.Login(username, password, onDone);
        }
    }
}
