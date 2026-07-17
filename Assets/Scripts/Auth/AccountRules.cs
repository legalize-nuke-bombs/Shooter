using System.Text.RegularExpressions;

namespace Shooter.Auth
{
    public static class AccountRules
    {
        public static string ValidateUsername(string username)
        {
            return Regex.IsMatch(username, "^[a-zA-Z0-9_]{4,20}$")
                ? null
                : "Имя пользователя: 4-20 символов, латиница, цифры, подчёркивание";
        }

        public static string ValidatePassword(string password)
        {
            if (password.Length < 8 || password.Length > 40) return "Пароль: 8-40 символов";

            bool upper = false, lower = false, digit = false;
            foreach (char character in password)
            {
                if (char.IsUpper(character)) upper = true;
                else if (char.IsLower(character)) lower = true;
                else if (char.IsDigit(character)) digit = true;
            }
            return upper && lower && digit ? null : "Пароль должен содержать заглавную и строчную буквы и цифру";
        }

        public static string ValidateDisplayName(string displayName)
        {
            return displayName.Length >= 1 && displayName.Length <= 40
                ? null
                : "Отображаемое имя: 1-40 символов";
        }
    }
}
