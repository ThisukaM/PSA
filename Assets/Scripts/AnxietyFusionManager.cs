using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

// Bridge that fuses face emotion (Python) + voice anxiety (C#) and drives AudienceManager
public class AnxietyFusionManager : MonoBehaviour
{
    [Header("Python Emotion Bridge")]
    public bool launchPythonBridge = true;           // Launch python on play (Editor/Standalone only)
    public string pythonExecutable = "python";       // Or absolute path to python.exe
    public string bridgeScriptPath = "emotion_bridge.py"; // Relative to project root
    public int cameraIndex = 1;                      // Webcam index for face capture

    [Tooltip("Where the python bridge writes the latest face result JSON")]
    public string faceJsonPath = "RuntimeData/face_emotion.json"; // Relative to project root
    [Tooltip("Where to append 10s window logs (NDJSON)")]
    public string scoreLogPath = "RuntimeData/psa_log.ndjson"; // Relative to project root

    [Header("Unity Components")]
    public AudienceManager audience;                 // Target to drive
    public VoiceAnxietySystem.VoiceAnxietyAnalyzer voiceAnalyzer; // From VoiceAnxietySystem

    [Header("Fusion Settings")]
    [Range(0.1f, 2f)] public float pollInterval = 0.25f; // How often to read the JSON (sec)
    [Range(0f, 1f)] public float smoothing = 0.3f;       // Lerp smoothing for final 1-10 score

    [Header("Debug")]
    public float faceScore1to10 = 5f;              // Latest face-derived score (1-10)
    public float voiceScore1to10 = 5f;             // Latest voice-derived score (1-10)
    public float fusedScore1to10 = 5f;             // Smoothed average (1-10)
    public int faceDetections = 0;                 // Faces detected in last JSON

    public UnityEvent<float> OnFusedScoreUpdated;  // Optional: UI bindings
    public AnxietyHud hud;                         // Optional HUD

    private Process _pythonProc;
    private float _nextPoll;
    private string _projectRoot;

    // Logging (10s windows)
    [System.Serializable]
    private class SampleRecord
    {
        public string ts;             // local time string dd/MM/yyyy HH:mm:ss
        public float face1to10;
        public float voice1to10;
        public float fused1to10;
        public int psa;               // 1..10
        public int faces;
    }

    [System.Serializable]
    private class EventRecord
    {
        public string ts;             // local time string dd/MM/yyyy HH:mm:ss
        public string type;           // e.g., "animation_change"
        public int fromScore;
        public int toScore;
    }

    [System.Serializable]
    private class WindowRecord
    {
        public string window_start;   // local time string dd/MM/yyyy HH:mm:ss
        public string window_end;     // local time string dd/MM/yyyy HH:mm:ss
        public SampleRecord[] samples;
        public EventRecord[] eventsArr; // JsonUtility dislikes "events" sometimes
    }

    private List<SampleRecord> _samplesWindow = new List<SampleRecord>();
    private List<EventRecord> _eventsWindow = new List<EventRecord>();
    private float _windowStart;            // unscaled seconds
    private string _windowStartStr;        // formatted local time
    private int _lastSentScore;

    [Serializable]
    private class FaceAnxietyDto
    {
        public double timestamp;
        public string emotion;
        public float confidence;
        public int faces_detected;
        public int coarse_anxiety_1_5; // 1-5 (legacy)
        public int coarse_anxiety_1_10; // 1-10 (preferred)
    }

    void Awake()
    {
        if (!audience) audience = FindFirstObjectByType<AudienceManager>();
        if (!voiceAnalyzer) voiceAnalyzer = FindFirstObjectByType<VoiceAnxietySystem.VoiceAnxietyAnalyzer>();
        if (!hud) hud = FindFirstObjectByType<AnxietyHud>();

        // Auto-spawn HUD prefab if none present
        if (!hud)
        {
            var prefab = Resources.Load<GameObject>("AnxietyHudCanvas");
            if (prefab)
            {
                var inst = Instantiate(prefab);
                hud = inst.GetComponentInChildren<AnxietyHud>();
                if (hud) hud.fusion = this;
            }
        }

        // Resolve project root to build absolute paths that work in Editor/Standalone
        _projectRoot = Application.dataPath; // Assets
        _projectRoot = Directory.GetParent(_projectRoot).FullName.Replace('\\', '/');

        _windowStart = Time.unscaledTime;
        _windowStartStr = FormatNowLocal();
        _lastSentScore = 0;
    }

    void Start()
    {
        TryLaunchPython();
        _nextPoll = Time.unscaledTime;
    }

    void OnDestroy()
    {
        TryKillPython();
        try { FlushWindow(force: true); } catch { }
    }

    void Update()
    {
        if (Time.unscaledTime >= _nextPoll)
        {
            _nextPoll = Time.unscaledTime + pollInterval;
            ReadFaceJson();
            ReadVoiceValue();
            FuseAndApply();
        }
    }

    private void TryLaunchPython()
    {
        if (!launchPythonBridge) return;

        try
        {
            string absScript = ResolveProjectPath(bridgeScriptPath);
            string absJson = ResolveProjectPath(faceJsonPath);

            // Ensure output directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(absJson));

            var psi = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                Arguments = $"-u \"{absScript}\" --camera {cameraIndex} --json \"{absJson}\" --nogui",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = false
            };
            psi.WorkingDirectory = Path.GetDirectoryName(absScript);
            _pythonProc = Process.Start(psi);
            if (_pythonProc != null)
            {
                _pythonProc.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        UnityEngine.Debug.LogWarning($"[AnxietyFusion][py] {e.Data}");
                    }
                };
                _pythonProc.BeginErrorReadLine();
            }
            UnityEngine.Debug.Log($"[AnxietyFusion] Launched python bridge: {psi.FileName} {psi.Arguments}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[AnxietyFusion] Could not launch python: {ex.Message}. You can run the bridge manually.");
            _pythonProc = null;
        }
    }

    private void TryKillPython()
    {
        try
        {
            if (_pythonProc != null && !_pythonProc.HasExited)
            {
                _pythonProc.Kill();
            }
        }
        catch { }
        finally { _pythonProc = null; }
    }

    private void ReadFaceJson()
    {
        try
        {
            string absJson = ResolveProjectPath(faceJsonPath);
            if (!File.Exists(absJson)) return;

            // Read last write with retry (in case python is writing)
            string json = null;
            for (int i = 0; i < 2; i++)
            {
                try { json = File.ReadAllText(absJson); break; }
                catch (IOException) { System.Threading.Thread.Sleep(5); }
            }
            if (string.IsNullOrWhiteSpace(json)) return;

            var dto = JsonUtility.FromJson<FaceAnxietyDto>(json);
            if (dto != null)
            {
                // Prefer 1-10 if present; otherwise scale legacy 1-5
                int face10 = dto.coarse_anxiety_1_10 >= 1 && dto.coarse_anxiety_1_10 <= 10
                    ? dto.coarse_anxiety_1_10
                    : Mathf.Clamp(dto.coarse_anxiety_1_5 * 2, 1, 10);
                faceScore1to10 = Mathf.Clamp(face10, 1, 10);
                faceDetections = dto.faces_detected;
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[AnxietyFusion] Face JSON read error: {ex.Message}");
        }
    }

    private string ResolveProjectPath(string relativeOrAbsolute)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return _projectRoot;
        string normalized = relativeOrAbsolute.Replace('\\', '/');
        if (Path.IsPathRooted(normalized)) return normalized;

        try
        {
            string projectFolderName = new DirectoryInfo(_projectRoot).Name;
            string prefix = projectFolderName + "/";
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(prefix.Length);
            }
        }
        catch { }

        return Path.Combine(_projectRoot, normalized).Replace('\\', '/');
    }

    private void ReadVoiceValue()
    {
        try
        {
            // Voice analyzer returns 0..1
            // Map discretely by thresholds:
            //   v < 0.2  -> 1 (good / green)
            //   0.2..0.5 -> 3 (yellow)
            //   > 0.5    -> 9 (terrible / red)
            float v = voiceAnalyzer ? voiceAnalyzer.GetAnxietyLevel() : 0.5f; // default mid
            if (v < 0.2f)
            {
                voiceScore1to10 = 1f;
            }
            else if (v <= 0.5f)
            {
                voiceScore1to10 = 3f;
            }
            else
            {
                voiceScore1to10 = 9f;
            }
        }
        catch
        {
            voiceScore1to10 = 5f;
        }
    }

    private void FuseAndApply()
    {
        float rawAvg = (faceScore1to10 + voiceScore1to10) * 0.5f;
        fusedScore1to10 = Mathf.Lerp(fusedScore1to10, rawAvg, 1f - Mathf.Clamp01(1f - smoothing));

        // Drive audience on 1..10 scale
        if (audience)
        {
            int score1to10 = Mathf.Clamp(Mathf.RoundToInt(fusedScore1to10), 1, 10);
            // Log animation change event if score bucket changes
            if (_lastSentScore != 0 && score1to10 != _lastSentScore)
            {
                _eventsWindow.Add(new EventRecord
                {
                    ts = FormatNowLocal(),
                    type = "animation_change",
                    fromScore = _lastSentScore,
                    toScore = score1to10
                });
            }
            _lastSentScore = score1to10;
            audience.UpdateScore(score1to10);
        }

        OnFusedScoreUpdated?.Invoke(fusedScore1to10);

        // Push to HUD if present (it pulls values in Update, so this is optional)
        if (hud && !hud.enabled)
        {
            hud.enabled = true;
        }

        // Append sample and maybe flush
        _samplesWindow.Add(new SampleRecord
        {
            ts = FormatNowLocal(),
            face1to10 = faceScore1to10,
            voice1to10 = voiceScore1to10,
            fused1to10 = fusedScore1to10,
            psa = Mathf.Clamp(Mathf.RoundToInt(fusedScore1to10), 1, 10),
            faces = faceDetections
        });

        if (Time.unscaledTime - _windowStart >= 10f)
        {
            FlushWindow(force: false);
        }
    }

    private void FlushWindow(bool force)
    {
        if (!force && _samplesWindow.Count == 0 && _eventsWindow.Count == 0) return;

        var record = new WindowRecord
        {
            window_start = _windowStartStr,
            window_end = FormatNowLocal(),
            samples = _samplesWindow.ToArray(),
            eventsArr = _eventsWindow.ToArray()
        };

        string json = JsonUtility.ToJson(record);
        try
        {
            string absPath = ResolveProjectPath(scoreLogPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absPath));
            File.AppendAllText(absPath, json + "\n");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[AnxietyFusion] Log write error: {ex.Message}");
        }
        finally
        {
            _samplesWindow.Clear();
            _eventsWindow.Clear();
            _windowStart = Time.unscaledTime;
            _windowStartStr = FormatNowLocal();
        }
    }

    private string FormatNowLocal()
    {
        return System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    }
}
