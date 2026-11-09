using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Zulvyre
{
    [Serializable]
    public class AccountRecord
    {
        public string Name;
        public string Salt;
        public string Hash;
        public string Created;
        public string LastSeen;
        public bool HeroSet;
        public int Hero;
    }

    [Serializable]
    class AccountFile
    {
        public List<AccountRecord> Accounts = new List<AccountRecord>();
    }

    /// <summary>
    /// Local player accounts for the Windows build. This gates entry into the game;
    /// it is not a security boundary (the file lives next to the game's saves), so the
    /// hash only keeps plain passwords out of the JSON.
    /// </summary>
    public static class AccountStore
    {
        const string PrefLast = "zulvyre.account";
        static AccountFile file;
        static readonly List<string> sessionNames = new List<string>();

        public static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, "accounts.json"); }
        }

        public static string LastAccount
        {
            get { return PlayerPrefs.GetString(PrefLast, string.Empty); }
        }

        static AccountFile Data
        {
            get
            {
                if (file != null) return file;
                try
                {
                    if (File.Exists(FilePath))
                    {
                        string json = File.ReadAllText(FilePath);
                        file = JsonUtility.FromJson<AccountFile>(json);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[accounts] could not read store: " + exception.Message);
                }
                if (file == null) file = new AccountFile();
                if (file.Accounts == null) file.Accounts = new List<AccountRecord>();
                return file;
            }
        }

        public static List<string> Names()
        {
            sessionNames.Clear();
            var accounts = Data.Accounts;
            for (int i = 0; i < accounts.Count; i++) sessionNames.Add(accounts[i].Name);
            return sessionNames;
        }

        public static bool Exists(string name)
        {
            return Find(name) != null;
        }

        public static string Create(string name, string password, int hero, out string error)
        {
            error = ValidateName(name);
            if (error != null) return null;
            if (Exists(name))
            {
                error = "That name is already sworn in. Log in instead.";
                return null;
            }
            if (ValidatePassword(password) is string passwordError)
            {
                error = passwordError;
                return null;
            }

            var salt = NewSalt();
            var record = new AccountRecord
            {
                Name = name.Trim(),
                Salt = salt,
                Hash = Hash(name.Trim(), password, salt),
                Created = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                LastSeen = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                HeroSet = true,
                Hero = hero
            };
            Data.Accounts.Add(record);
            Save();
            Remember(record.Name);
            return record.Name;
        }

        public static string Verify(string name, string password, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(password))
            {
                error = "Enter your account name and password.";
                return null;
            }

            var record = Find(name);
            if (record == null)
            {
                error = "No such account. Create one to enter the village.";
                return null;
            }
            if (Hash(record.Name, password, record.Salt) != record.Hash)
            {
                error = "That password does not match.";
                return null;
            }

            record.LastSeen = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            Save();
            Remember(record.Name);
            return record.Name;
        }

        public static string CreatedFor(string name)
        {
            var record = Find(name);
            return record != null ? record.Created : string.Empty;
        }

        public static int HeroFor(string name, int fallback)
        {
            var record = Find(name);
            return record != null && record.HeroSet && record.Hero >= 0 ? record.Hero : fallback;
        }

        public static void SetHero(string name, int hero)
        {
            var record = Find(name);
            if (record == null) return;
            record.HeroSet = true;
            record.Hero = hero;
            Save();
        }

        static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Choose a name of at least three letters.";
            string trimmed = name.Trim();
            if (trimmed.Length < 3) return "Choose a name of at least three letters.";
            if (trimmed.Length > 16) return "Keep the name to sixteen letters.";
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                bool ok = char.IsLetterOrDigit(c) || c == '_' || c == ' ' || c == '-' || c == '\'';
                if (!ok) return "Letters, numbers, spaces, - and _ only.";
            }
            return null;
        }

        static string ValidatePassword(string password)
        {
            if (password == null || password.Length < 4) return "The password needs at least four characters.";
            if (password.Length > 24) return "Keep the password to twenty-four characters.";
            return null;
        }

        static AccountRecord Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var accounts = Data.Accounts;
            for (int i = 0; i < accounts.Count; i++)
            {
                if (string.Equals(accounts[i].Name, name.Trim(), StringComparison.OrdinalIgnoreCase)) return accounts[i];
            }
            return null;
        }

        static void Remember(string name)
        {
            PlayerPrefs.SetString(PrefLast, name);
            PlayerPrefs.Save();
        }

        static void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[accounts] could not write store: " + exception.Message);
            }
        }

        static string NewSalt()
        {
            var bytes = new byte[12];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        static string Hash(string name, string password, string salt)
        {
            using (var sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(("zulvyre|" + name.ToLowerInvariant() + "|" + salt + "|" + password)));
                return Convert.ToBase64String(digest);
            }
        }
    }
}
