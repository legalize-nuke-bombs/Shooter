using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Shooter.Game.Core.Saves;
using UnityEngine;

namespace Shooter.Game.Llm.Notes
{
    public class LlmNotes : MonoBehaviour, ISaveableComponent
    {
        [SerializeField] private int nameLimit = 25;
        [SerializeField] private int descriptionLimit = 100;
        [SerializeField] private int contentLimit = 5000;
        [SerializeField] private int amountLimit = 100;

        private readonly Dictionary<string, LlmNote> notes = new();

        public string ComponentKey => "LlmNotes";
        private struct SaveData
        {
            public Dictionary<string, LlmNote> Notes { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                Notes = new Dictionary<string, LlmNote>(notes)
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            notes.Clear();
            foreach (var kvp in sd.Notes) notes.Add(kvp.Key, kvp.Value);
        }

        public int Count => notes.Count;

        public int NameLimit => nameLimit;
        public int DescriptionLimit => descriptionLimit;
        public int ContentLimit => contentLimit;
        public int AmountLimit => amountLimit;

        public string List()
        {
            var sb = new StringBuilder();

            foreach (KeyValuePair<string, LlmNote> kvp in notes) sb.AppendLine(kvp.Key + ": " + kvp.Value.Description);

            return sb.Length > 0
                ? sb.ToString()
                : "Nothing yet";
        }

        public string Read(string key)
        {
            if (notes.TryGetValue(key, out LlmNote note)) return note.Content;
            throw new ArgumentException($"Note named {key} does not exist");
        }

        public void Add(string key, LlmNote note)
        {
            ValidateCount();
            ValidateNote(key, note);
            if (!notes.TryAdd(key, note)) throw new ArgumentException($"Note named {key} already exists");
        }

        public void Remove(string key)
        {
            if (!notes.Remove(key)) throw new ArgumentException($"Note named {key} does not exist");
        }

        public string Matches(string regexPattern)
        {
            var regex = new Regex(regexPattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            var report = new Dictionary<string, int>();

            foreach (KeyValuePair<string, LlmNote> kvp in notes)
            {
                int matchCount = regex.Matches(kvp.Key + "\n" + kvp.Value.Description + "\n" + kvp.Value.Content).Count;
                if (matchCount > 0) report[kvp.Key] = matchCount;
            }

            IOrderedEnumerable<KeyValuePair<string, int>> sortedReport = report.OrderByDescending(kvp => kvp.Value);

            var sb = new StringBuilder();
            foreach (KeyValuePair<string, int> kvp in sortedReport)
            {
                string noteName = kvp.Key;
                int matchesCount = kvp.Value;

                sb.AppendLine($"{noteName} ({notes[noteName].Description}) : {matchesCount} matches");
            }

            return sb.Length > 0
                ? sb.ToString()
                : "No matches found";
        }

        private void ValidateCount()
        {
            if (Count >= AmountLimit)
                throw new ArgumentException(
                    $"Note amount limit ({amountLimit}) exceeded. Merge existing notes or delete unnecessary ones");
        }

        private void ValidateNote(string key, LlmNote note)
        {
            if (string.IsNullOrEmpty(key) || note == null || string.IsNullOrEmpty(note.Description) ||
                note.Content == null) throw new ArgumentException("Please, fill in all fields.");
            if (key.Length > NameLimit)
                throw new ArgumentException(
                    $"The note name size must be up to {NameLimit} characters, got {key.Length}");
            if (note.Description.Length > DescriptionLimit)
                throw new ArgumentException(
                    $"The note description size must be up to {DescriptionLimit} characters, got {note.Description.Length}");
            if (note.Content.Length > ContentLimit)
                throw new ArgumentException(
                    $"The note content size must be up to {ContentLimit} characters, got {note.Content.Length}");
        }
    }
}
