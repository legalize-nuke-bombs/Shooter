using System;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class AccountPage : MainMenuPage
    {
        private const string BackButtonName = "back";
        private const string PrivateKeyFoldoutName = "secret";
        private const string PublicKeyLabelName = "identity";
        private const string PrivateKeyFieldName = "phrase";
        private const string StatusLabelName = "status";
        private const string InvalidKeyStatus = "Недействительный ключ";
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private readonly Foldout privateKeyFoldout;
        private readonly Label publicKeyLabel;
        private readonly TextField privateKeyField;
        private readonly Label statusLabel;
        private string shownPrivateKey;

        public AccountPage(VisualElement root) : base(root)
        {
            RequireElement<Button>(BackButtonName).clicked += () => BackClicked?.Invoke();

            privateKeyFoldout = RequireElement<Foldout>(PrivateKeyFoldoutName);
            publicKeyLabel = RequireElement<Label>(PublicKeyLabelName);
            privateKeyField = RequireElement<TextField>(PrivateKeyFieldName);
            statusLabel = RequireElement<Label>(StatusLabelName);
            privateKeyField.RegisterValueChangedCallback(change => RestoreAccountFromPrivateKey(change.newValue));

            ShowCurrentAccount();
        }

        public event Action BackClicked;

        protected override void HandlePageShown()
        {
            privateKeyFoldout.value = false;
            ShowCurrentAccount();
        }

        private void ShowCurrentAccount()
        {
            Account account = Config.Account;
            if (account == null)
            {
                shownPrivateKey = "";
                publicKeyLabel.text = "";
                privateKeyField.SetValueWithoutNotify("");
                statusLabel.text = "";
                return;
            }

            shownPrivateKey = account.PrivateKey;
            publicKeyLabel.text = account.PublicKey;
            privateKeyField.SetValueWithoutNotify(shownPrivateKey);
            statusLabel.text = "";
        }

        private void RestoreAccountFromPrivateKey(string privateKey)
        {
            Account account;
            try
            {
                account = Account.FromPrivateKey(privateKey);
            }
            catch (Exception)
            {
                statusLabel.text = InvalidKeyStatus;
                return;
            }

            Config.GetGameConfig().Account = account;
            Config.SaveGameConfig();
            Log.Info("Account restored from a recovery phrase");
            ShowCurrentAccount();
        }
    }
}
