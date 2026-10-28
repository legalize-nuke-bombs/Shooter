using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class LlmNotes : MonoBehaviour, ISaveableComponent
    {
        [SerializeField] private int nameLimit = 25;
        [SerializeField] private int descriptionLimit = 100;
        [SerializeField] private int contentLimit = 5000;
        [SerializeField] private int amountLimit = 100;

        private readonly Dictionary<string, LlmNote> notesByName = new();

        private struct SaveData
        {
            public Dictionary<string, LlmNote> Notes { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                Notes = new Dictionary<string, LlmNote>(notesByName)
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            notesByName.Clear();
            foreach ((string noteName, LlmNote note) in saveData.Notes)
            {
                notesByName.Add(noteName, note);
            }
        }

        public int Count => notesByName.Count;

        public int NameLimit => nameLimit;
        public int DescriptionLimit => descriptionLimit;
        public int ContentLimit => contentLimit;
        public int AmountLimit => amountLimit;

        public string ListNotes()
        {
            StringBuilder noteList = new();

            foreach ((string noteName, LlmNote note) in notesByName)
            {
                noteList.Append(noteName).Append(": ").Append(note.Description);
                if (!string.IsNullOrEmpty(note.Updated)) noteList.Append(" (").Append(note.Updated).Append(')');
                noteList.Append('\n');
            }

            return noteList.Length > 0 ? noteList.ToString() : "Nothing yet";
        }

        public string ReadNoteContent(string noteName)
        {
            if (notesByName.TryGetValue(noteName, out LlmNote note)) return note.Content;
            throw new ArgumentException($"Note named {noteName} does not exist");
        }

        public void AddNote(string noteName, LlmNote note)
        {
            ValidateAmountLimit();
            ValidateNote(noteName, note);
            note.Updated = Llm.FormatCurrentWorldTime();
            if (!notesByName.TryAdd(noteName, note)) throw new ArgumentException($"Note named {noteName} already exists");
        }

        public void ReplaceNote(string noteName, LlmNote note)
        {
            if (!notesByName.ContainsKey(noteName)) throw new ArgumentException($"Note named {noteName} does not exist");
            ValidateNote(noteName, note);
            note.Updated = Llm.FormatCurrentWorldTime();
            notesByName[noteName] = note;
        }

        public void RemoveNote(string noteName)
        {
            if (!notesByName.Remove(noteName)) throw new ArgumentException($"Note named {noteName} does not exist");
        }

        public string SearchNotes(string regexPattern)
        {
            Regex regex = new(regexPattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            var matchCountsByName = new Dictionary<string, int>();

            foreach ((string noteName, LlmNote note) in notesByName)
            {
                int matchCount = regex.Matches(noteName + "\n" + note.Description + "\n" + note.Content).Count;
                if (matchCount > 0) matchCountsByName[noteName] = matchCount;
            }

            var mostMatchedFirst = matchCountsByName.OrderByDescending(nameAndMatchCount => nameAndMatchCount.Value);

            StringBuilder report = new();
            foreach ((string noteName, int matchCount) in mostMatchedFirst)
            {
                report.Append($"{noteName} ({notesByName[noteName].Description}) : {matchCount} matches").Append('\n');
            }

            return report.Length > 0 ? report.ToString() : "No matches found";
        }

        private void ValidateAmountLimit()
        {
            if (Count >= AmountLimit)
            {
                throw new ArgumentException($"Note amount limit ({amountLimit}) exceeded. Merge existing notes or delete unnecessary ones");
            }
        }

        private void ValidateNote(string noteName, LlmNote note)
        {
            if (string.IsNullOrEmpty(noteName) || note == null || string.IsNullOrEmpty(note.Description) || note.Content == null)
            {
                throw new ArgumentException("Please, fill in all fields.");
            }
            if (noteName.Length > NameLimit)
            {
                throw new ArgumentException($"The note name size must be up to {NameLimit} characters, got {noteName.Length}");
            }
            if (note.Description.Length > DescriptionLimit)
            {
                throw new ArgumentException($"The note description size must be up to {DescriptionLimit} characters, got {note.Description.Length}");
            }
            if (note.Content.Length > ContentLimit)
            {
                throw new ArgumentException($"The note content size must be up to {ContentLimit} characters, got {note.Content.Length}");
            }
        }
    }
}
