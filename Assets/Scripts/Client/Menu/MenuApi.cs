using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Shooter.Serialization;
using Shooter.Client.Account;

namespace Shooter.Client.Menu
{
    public class MenuApi
    {
        private static readonly Dictionary<string, string> ErrorTexts = new Dictionary<string, string>
        {
            { "NOT_AUTHENTICATED", "Сессия истекла. Выполните вход заново." },
            { "INVALID_PASSWORD", "Неверный пароль." },
            { "WEAK_PASSWORD", "Пароль: 8-40 символов, заглавная и строчная буквы, цифра." },
            { "USERNAME_TAKEN", "Это имя пользователя уже занято." },
            { "USER_NOT_FOUND", "Пользователь не найден." },
            { "EMPTY_REQUEST", "Пустой запрос." },
            { "MALFORMED_REQUEST", "Сервер отклонил запрос как некорректный." },
            { "WORLD_NOT_FOUND", "Мир не найден. Проверьте идентификатор." },
            { "WORLD_DOES_NOT_ACCEPT_NEW_MEMBERS", "Этот мир не принимает новых участников." },
            { "NOT_A_MEMBER", "Вы не состоите в этом мире." },
            { "NOT_A_CREATOR", "Действие доступно только владельцу мира." },
            { "BLACKLISTED", "Вы в чёрном списке этого мира." },
            { "GAME_SERVER_UNAVAILABLE", "Игровой сервер недоступен. Попробуйте ещё раз." },
            { "INTERNAL_ERROR", "Внутренняя ошибка сервера." }
        };

        private readonly MonoBehaviour runner;

        public MenuApi(MonoBehaviour runner)
        {
            this.runner = runner;
        }

        public void CheckServer(Action<ServerInfoResponse> onDone)
        {
            Request("GET", "/api/server", null, false, (code, text) =>
            {
                if (code != 200) { onDone(null); return; }
                ServerInfoResponse info = Json.Deserialize<ServerInfoResponse>(text);
                onDone(info != null && !string.IsNullOrEmpty(info.Name) ? info : null);
            });
        }

        public void Login(string username, string password, Action<string, string> onDone)
        {
            string body = Json.Serialize(new LoginRequest { Username = username, Password = password });
            Request("POST", "/api/auth/login", body, false, (code, text) => OnTokenResponse(code, text, onDone));
        }

        public void Register(string username, string displayName, string password, Action<string, string> onDone)
        {
            string body = Json.Serialize(new RegisterRequest { Username = username, DisplayName = displayName, Password = password });
            Request("POST", "/api/auth/register", body, false, (code, text) => OnTokenResponse(code, text, onDone));
        }

        public void Me(Action<UserDto, string> onDone)
        {
            Request("GET", "/api/users/me", null, true, (code, text) =>
            {
                if (code != 200) { onDone(null, HumanError(code, text)); return; }
                UserDto me = Json.Deserialize<UserDto>(text);
                if (me == null || me.Id <= 0) { onDone(null, "Не удалось получить профиль."); return; }
                onDone(me, null);
            });
        }

        public void LoadWorlds(int page, int size, Action<List<WorldDto>, string> onDone)
        {
            string path = "/api/worlds?playerRole=MEMBER&page=" + page + "&size=" + size;

            Request("GET", path, null, true, (code, text) =>
            {
                if (code != 200) { onDone(null, HumanError(code, text)); return; }
                WorldsWrap wrap = Json.Deserialize<WorldsWrap>("{\"items\":" + text + "}");
                if (wrap?.Items == null) { onDone(null, "Не удалось получить список миров."); return; }
                onDone(wrap.Items, null);
            });
        }

        public void Join(string worldId, Action<string> onDone)
        {
            Request("POST", "/api/worlds/" + worldId + "/players", "{}", true, (code, text) =>
                onDone(code == 200 || code == 201 ? null : HumanError(code, text)));
        }

        public void CreateWorld(CreateWorldRequest request, Action<string> onDone)
        {
            Request("POST", "/api/worlds", Json.Serialize(request), true, (code, text) =>
                onDone(code == 200 || code == 201 ? null : HumanError(code, text)));
        }

        private static void OnTokenResponse(long code, string text, Action<string, string> onDone)
        {
            if (code != 200 && code != 201) { onDone(null, HumanError(code, text)); return; }
            TokenResponse response = Json.Deserialize<TokenResponse>(text);
            if (response == null || string.IsNullOrEmpty(response.Token)) { onDone(null, "Сервер не вернул токен."); return; }
            onDone(response.Token, null);
        }

        private void Request(string method, string path, string body, bool auth, Action<long, string> onDone)
        {
            runner.StartCoroutine(RequestRoutine(method, path, body, auth, onDone));
        }

        private static IEnumerator RequestRoutine(string method, string path, string body, bool auth, Action<long, string> onDone)
        {
            string url = Session.HttpBase + path;
            using var request = new UnityWebRequest(url, method);
            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            if (auth) request.SetRequestHeader("Authorization", "Bearer " + Session.Token);
            request.timeout = 10;

            yield return request.SendWebRequest();

            string text = request.downloadHandler.text ?? "";
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
                onDone(0, request.error);
            else
                onDone(request.responseCode, text);
        }

        private static string HumanError(long code, string text)
        {
            if (code == 0) return "Нет соединения с сервером.";
            ProblemResponse problem = Json.Deserialize<ProblemResponse>(text);
            if (problem != null && !string.IsNullOrEmpty(problem.Code))
            {
                if (ErrorTexts.TryGetValue(problem.Code, out string human)) return human;
                if (!string.IsNullOrEmpty(problem.Detail)) return problem.Detail;
                return problem.Code;
            }
            return "Ошибка " + code + ".";
        }
    }
}
