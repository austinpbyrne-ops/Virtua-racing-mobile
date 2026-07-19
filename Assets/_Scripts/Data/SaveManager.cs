using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace VRacer.Data
{
    /// <summary>
    /// Persistence layer: settings, high scores, lap times.
    /// Uses PlayerPrefs for settings, JSON files for scores/times.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SETTINGS_KEY = "VRacerSettings";
        private const string HIGHSCORES_KEY = "VRacerHighScores";
        private const string GHOST_DATA_KEY = "VRacerGhostData";

        private string savePath;

        // Settings
        public GameSettings Settings { get; private set; }

        // High scores per track
        public Dictionary<string, List<HighScoreEntry>> HighScores { get; private set; }

        // Best lap per track (for ghost car)
        public Dictionary<string, float> BestLapTimes { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            savePath = Path.Combine(Application.persistentDataPath, "VRacerData");
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            LoadAll();
        }

        private void LoadAll()
        {
            LoadSettings();
            LoadHighScores();
            LoadBestLaps();
        }

        // ============================================================
        // SETTINGS
        // ============================================================

        public void LoadSettings()
        {
            string json = PlayerPrefs.GetString(SETTINGS_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                Settings = new GameSettings(); // Defaults
            }
            else
            {
                Settings = JsonUtility.FromJson<GameSettings>(json);
            }
        }

        public void SaveSettings()
        {
            string json = JsonUtility.ToJson(Settings, true);
            PlayerPrefs.SetString(SETTINGS_KEY, json);
            PlayerPrefs.Save();
        }

        // ============================================================
        // HIGH SCORES
        // ============================================================

        public void LoadHighScores()
        {
            string filePath = Path.Combine(savePath, "highscores.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var wrapper = JsonUtility.FromJson<HighScoreWrapper>(json);
                HighScores = wrapper?.ToDictionary() ?? new Dictionary<string, List<HighScoreEntry>>();
            }
            else
            {
                HighScores = new Dictionary<string, List<HighScoreEntry>>
                {
                    { "Big Forest", new List<HighScoreEntry>() },
                    { "Bay Bridge", new List<HighScoreEntry>() },
                    { "Acropolis", new List<HighScoreEntry>() }
                };
            }
        }

        public void SaveHighScores()
        {
            var wrapper = new HighScoreWrapper();
            wrapper.FromDictionary(HighScores);
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(Path.Combine(savePath, "highscores.json"), json);
        }

        public void AddHighScore(string trackName, string playerName, float time, int position)
        {
            if (!HighScores.ContainsKey(trackName))
                HighScores[trackName] = new List<HighScoreEntry>();

            var entry = new HighScoreEntry
            {
                playerName = playerName,
                totalTime = time,
                position = position,
                date = System.DateTime.Now.ToString("yyyy-MM-dd")
            };

            HighScores[trackName].Add(entry);
            HighScores[trackName].Sort((a, b) => a.totalTime.CompareTo(b.totalTime));

            // Keep top 10
            if (HighScores[trackName].Count > 10)
                HighScores[trackName].RemoveRange(10, HighScores[trackName].Count - 10);

            SaveHighScores();
        }

        public List<HighScoreEntry> GetHighScores(string trackName)
        {
            if (HighScores.TryGetValue(trackName, out var scores))
                return scores;
            return new List<HighScoreEntry>();
        }

        // ============================================================
        // BEST LAP TIMES (TIME TRIAL GHOST)
        // ============================================================

        public void LoadBestLaps()
        {
            string filePath = Path.Combine(savePath, "bestlaps.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var wrapper = JsonUtility.FromJson<BestLapWrapper>(json);
                BestLapTimes = wrapper?.ToDictionary() ?? new Dictionary<string, float>();
            }
            else
            {
                BestLapTimes = new Dictionary<string, float>
                {
                    { "Big Forest", float.MaxValue },
                    { "Bay Bridge", float.MaxValue },
                    { "Acropolis", float.MaxValue }
                };
            }
        }

        public void SaveBestLaps()
        {
            var wrapper = new BestLapWrapper();
            wrapper.FromDictionary(BestLapTimes);
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(Path.Combine(savePath, "bestlaps.json"), json);
        }

        public void UpdateBestLap(string trackName, float lapTime)
        {
            if (!BestLapTimes.ContainsKey(trackName) || lapTime < BestLapTimes[trackName])
            {
                BestLapTimes[trackName] = lapTime;
                SaveBestLaps();
            }
        }

        public float GetBestLap(string trackName)
        {
            if (BestLapTimes.TryGetValue(trackName, out float time))
                return time;
            return float.MaxValue;
        }

        // ============================================================
        // GHOST CAR DATA (for time trial)
        // ============================================================

        public void SaveGhostData(string trackName, List<GhostFrame> ghostFrames)
        {
            string filePath = Path.Combine(savePath, $"ghost_{trackName.Replace(" ", "_")}.json");
            var wrapper = new GhostDataWrapper { frames = ghostFrames };
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(filePath, json);
        }

        public List<GhostFrame> LoadGhostData(string trackName)
        {
            string filePath = Path.Combine(savePath, $"ghost_{trackName.Replace(" ", "_")}.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var wrapper = JsonUtility.FromJson<GhostDataWrapper>(json);
                return wrapper?.frames ?? new List<GhostFrame>();
            }
            return new List<GhostFrame>();
        }
    }

    // ============================================================
    // DATA STRUCTURES
    // ============================================================

    [System.Serializable]
    public class GameSettings
    {
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public float musicVolume = 0.8f;
        public float engineVolume = 0.7f;
        public float tiltSensitivity = 2f;
        public bool tiltEnabled = true;
        public bool useMPH = true;
        public bool defaultManualTransmission = false;
        public int graphicsQuality = 1; // 0=Low, 1=Medium, 2=High
        public int renderScale = 100;   // percentage
    }

    [System.Serializable]
    public class HighScoreEntry
    {
        public string playerName;
        public float totalTime;
        public int position;
        public string date;
    }

    [System.Serializable]
    public class GhostFrame
    {
        public float time;
        public Vector3 position;
        public Vector3 rotation;
        public float speed;
    }

    // JSON wrappers (Unity JsonUtility can't serialize Dictionary directly)
    [System.Serializable]
    public class HighScoreWrapper
    {
        public List<string> trackNames = new List<string>();
        public List<HighScoreListWrapper> scores = new List<HighScoreListWrapper>();

        public void FromDictionary(Dictionary<string, List<HighScoreEntry>> dict)
        {
            trackNames.Clear();
            scores.Clear();
            foreach (var kvp in dict)
            {
                trackNames.Add(kvp.Key);
                scores.Add(new HighScoreListWrapper { entries = kvp.Value });
            }
        }

        public Dictionary<string, List<HighScoreEntry>> ToDictionary()
        {
            var dict = new Dictionary<string, List<HighScoreEntry>>();
            for (int i = 0; i < Mathf.Min(trackNames.Count, scores.Count); i++)
            {
                dict[trackNames[i]] = scores[i].entries;
            }
            return dict;
        }
    }

    [System.Serializable]
    public class HighScoreListWrapper
    {
        public List<HighScoreEntry> entries = new List<HighScoreEntry>();
    }

    [System.Serializable]
    public class BestLapWrapper
    {
        public List<string> trackNames = new List<string>();
        public List<float> times = new List<float>();

        public void FromDictionary(Dictionary<string, float> dict)
        {
            trackNames.Clear();
            times.Clear();
            foreach (var kvp in dict)
            {
                trackNames.Add(kvp.Key);
                times.Add(kvp.Value);
            }
        }

        public Dictionary<string, float> ToDictionary()
        {
            var dict = new Dictionary<string, float>();
            for (int i = 0; i < Mathf.Min(trackNames.Count, times.Count); i++)
            {
                dict[trackNames[i]] = times[i];
            }
            return dict;
        }
    }

    [System.Serializable]
    public class GhostDataWrapper
    {
        public List<GhostFrame> frames = new List<GhostFrame>();
    }
}
