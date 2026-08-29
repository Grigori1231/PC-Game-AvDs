using System;
using System.IO;
using System.Text.Json;

namespace Rogalik
{
    public class SaveState
    {
        public int HighScore { get; set; }
        public bool IntroSeen { get; set; }
    }

    public static class Save
    {
        private static readonly string PathFile =
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "save.json");

        private static SaveState data = LoadInternal();
        public static bool SessionIntroShown { get; set; } = false;

        private static SaveState LoadInternal()
        {
            try
            {
                if (File.Exists(PathFile))
                {
                    var json = File.ReadAllText(PathFile);
                    return JsonSerializer.Deserialize<SaveState>(json) ?? new SaveState();
                }
            }
            catch { }
            return new SaveState();
        }

        private static void SaveInternal()
        {
            try
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(PathFile, json);
            }
            catch { }
        }

        public static int HighScore
        {
            get => data.HighScore;
            set
            {
                if (value > data.HighScore)
                {
                    data.HighScore = value;
                    SaveInternal();
                }
            }
        }

        public static void ResetHighScore()
        {
            data.HighScore = 0;
            SaveInternal();
        }

        public static bool IntroSeen
        {
            get => data.IntroSeen;
            set { if (data.IntroSeen != value) { data.IntroSeen = value; SaveInternal(); } }
        }
    }
}