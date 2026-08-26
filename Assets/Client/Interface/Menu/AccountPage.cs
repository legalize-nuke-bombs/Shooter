using System;
using Shooter.Accounts;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class AccountPage : MenuPage
    {
        private const string BackButton = "back";
        private const string SecretFoldout = "secret";
        private const string IdentityLabel = "identity";
        private const string PhraseField = "phrase";
        private const string StatusLabel = "status";
        private const string InvalidKey = "Недействительный ключ";
        private static readonly Journal Log = Logs.Here();

        private readonly Foldout secret;
        private readonly Label identity;
        private readonly TextField phrase;
        private readonly Label status;
        private string current;

        public AccountPage(VisualElement root) : base(root)
        {
            Require<Button>(BackButton).clicked += () => Backing?.Invoke();

            secret = Require<Foldout>(SecretFoldout);
            identity = Require<Label>(IdentityLabel);
            phrase = Require<TextField>(PhraseField);
            status = Require<Label>(StatusLabel);
            phrase.RegisterValueChangedCallback(change => Restore(change.newValue));

            Load();
        }

        public event Action Backing;

        protected override void Opened()
        {
            secret.value = false;
            Load();
        }

        private void Load()
        {
            Account account = Config.Account;
            if (account == null)
            {
                current = "";
                identity.text = "";
                phrase.SetValueWithoutNotify("");
                status.text = "";
                return;
            }

            current = account.Key;
            identity.text = account.Public;
            phrase.SetValueWithoutNotify(current);
            status.text = "";
        }

        private void Restore(string entered)
        {
            Account account;
            try
            {
                account = Account.FromKey(entered);
            }
            catch (Exception)
            {
                status.text = InvalidKey;
                return;
            }

            Config.Read().Account = account;
            Config.Save();
            Log.Info("Account restored from a recovery phrase");
            Load();
        }
    }
}
