using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FusionAnyMonster.Art
{
    /// <summary>Local ComfyUI API client. Generation failure is recorded and never blocks placeholder art.</summary>
    public sealed class ComfyUIGenerationService : MonoBehaviour
    {
        [SerializeField] private ComfyUISettings settings;
        public event Action<ComfyPromptRecord> GenerationCompleted;
        public event Action<ComfyPromptRecord> GenerationFailed;
        public IReadOnlyList<ComfyPromptRecord> History => history;
        private readonly List<ComfyPromptRecord> history = new List<ComfyPromptRecord>();
        private string ClientId => "fusion-any-monster-" + SystemInfo.deviceUniqueIdentifier;

        private void Awake() { LoadHistory(); }

        public void Generate(string speciesId, string positivePrompt, string negativePrompt, long seed)
        {
            if (settings == null) { Debug.LogWarning("ComfyUI settings are missing. Placeholder art remains active."); return; }
            StartCoroutine(GenerateRoutine(speciesId, positivePrompt, negativePrompt, seed));
        }

        public void SetSettings(ComfyUISettings value) { settings = value; history.Clear(); LoadHistory(); }

        private IEnumerator GenerateRoutine(string speciesId, string positive, string negative, long seed)
        {
            var record = new ComfyPromptRecord
            {
                id = Guid.NewGuid().ToString("N"), createdUtc = DateTime.UtcNow.ToString("o"),
                speciesId = speciesId ?? string.Empty, positive = positive ?? string.Empty,
                negative = negative ?? string.Empty, seed = seed, status = "queued"
            };
            history.Insert(0, record);
            SavePrompts(record);
            SaveHistory();

            string workflowPath = null;
            string workflow = null;
            string workflowReadError = null;
            try
            {
                workflowPath = Path.IsPathRooted(settings.WorkflowJsonPath)
                    ? settings.WorkflowJsonPath
                    : Path.Combine(Application.dataPath, "..", settings.WorkflowJsonPath);
                if (!File.Exists(workflowPath)) workflowReadError = "Workflow JSON not found: " + workflowPath;
                else workflow = File.ReadAllText(workflowPath, Encoding.UTF8);
            }
            catch (Exception exception) { workflowReadError = "Could not read workflow JSON: " + exception.Message; }
            if (!string.IsNullOrEmpty(workflowReadError)) { Fail(record, workflowReadError); yield break; }

            if (!TrySetWorkflowValue(ref workflow, settings.PositivePromptNodeId, settings.PositivePromptInput, positive) ||
                !TrySetWorkflowValue(ref workflow, settings.NegativePromptNodeId, settings.NegativePromptInput, negative))
            { Fail(record, "Configured prompt node/input was not found in the API workflow."); yield break; }
            TrySetWorkflowValue(ref workflow, settings.SeedNodeId, settings.SeedInput, seed.ToString());

            string body = "{\"prompt\":" + workflow + ",\"client_id\":" + JsonString(ClientId) + "}";
            using (var request = new UnityWebRequest(settings.Endpoint + "/prompt", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 30;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) { Fail(record, "Queue request failed: " + request.error); yield break; }
                string promptId = JsonField(request.downloadHandler.text, "prompt_id");
                if (string.IsNullOrEmpty(promptId)) { Fail(record, "ComfyUI did not return prompt_id: " + request.downloadHandler.text); yield break; }
                record.promptId = promptId;
                record.status = "running";
                SaveHistory();
            }

            float deadline = Time.realtimeSinceStartup + settings.TimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForSecondsRealtime(settings.PollIntervalSeconds);
                using (var request = UnityWebRequest.Get(settings.Endpoint + "/history/" + UnityWebRequest.EscapeURL(record.promptId)))
                {
                    request.timeout = 15;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) continue;
                    string json = request.downloadHandler.text;
                    string outputs = JsonObjectForKey(json, record.promptId);
                    if (string.IsNullOrEmpty(outputs)) continue;
                    if (json.Contains("\"status\":{\"status_str\":\"error\"") || json.Contains("\"status_str\":\"error\""))
                    { Fail(record, "ComfyUI reported a workflow error."); yield break; }
                    string filename = JsonField(outputs, "filename");
                    if (string.IsNullOrEmpty(filename)) continue;
                    string subfolder = JsonField(outputs, "subfolder");
                    string type = JsonField(outputs, "type");
                    string imageUrl = settings.Endpoint + "/view?filename=" + UnityWebRequest.EscapeURL(filename) + "&subfolder=" + UnityWebRequest.EscapeURL(subfolder ?? string.Empty) + "&type=" + UnityWebRequest.EscapeURL(type ?? "output");
                    yield return DownloadAndImport(record, imageUrl);
                    yield break;
                }
            }
            Fail(record, "Timed out while waiting for ComfyUI output.");
        }

        private IEnumerator DownloadAndImport(ComfyPromptRecord record, string url)
        {
            using (var request = UnityWebRequestTexture.GetTexture(url))
            {
                request.timeout = 60;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) { Fail(record, "Output download failed: " + request.error); yield break; }
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                byte[] png = null;
                string encodeError = null;
                try { png = texture.EncodeToPNG(); }
                catch (Exception exception) { encodeError = "Could not encode generated image: " + exception.Message; }
                Destroy(texture);
                if (!string.IsNullOrEmpty(encodeError)) { Fail(record, encodeError); yield break; }
                string safeSpecies = SafeName(string.IsNullOrWhiteSpace(record.speciesId) ? "unassigned" : record.speciesId);
                string fileName = safeSpecies + "_" + record.seed + "_" + record.id.Substring(0, 8) + ".png";
                string fullPath = null;
                string writeError = null;
                try
                {
                    string folder = Path.IsPathRooted(settings.GeneratedAssetsFolder) ? settings.GeneratedAssetsFolder : Path.Combine(Directory.GetCurrentDirectory(), settings.GeneratedAssetsFolder);
                    Directory.CreateDirectory(folder);
                    fullPath = Path.Combine(folder, fileName);
                    File.WriteAllBytes(fullPath, png);
                }
                catch (Exception exception) { writeError = "Could not save generated image: " + exception.Message; }
                if (!string.IsNullOrEmpty(writeError)) { Fail(record, writeError); yield break; }
                record.outputAssetPath = fullPath;
                record.status = "completed";
                SaveHistory();
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
                string assetPath = fullPath.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/');
                var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
                if (importer != null) { importer.textureType = UnityEditor.TextureImporterType.Sprite; importer.alphaIsTransparency = true; importer.SaveAndReimport(); }
#endif
                GenerationCompleted?.Invoke(record);
            }
        }

        private void Fail(ComfyPromptRecord record, string reason)
        {
            record.status = "failed"; record.error = reason;
            SaveHistory();
            Debug.LogWarning("ComfyUI generation failed; placeholder art remains available. " + reason);
            GenerationFailed?.Invoke(record);
        }

        private void SavePrompts(ComfyPromptRecord record)
        {
            try
            {
                string path = ResolveUserPath(settings.PromptLogPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.AppendAllText(path, JsonUtility.ToJson(record) + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception exception) { Debug.LogWarning("Could not save ComfyUI prompt log: " + exception.Message); }
        }

        private void SaveHistory()
        {
            try
            {
                string path = ResolveUserPath(settings.HistoryPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(new ComfyRecordList { records = history }, true), Encoding.UTF8);
            }
            catch (Exception exception) { Debug.LogWarning("Could not save ComfyUI generation history: " + exception.Message); }
        }

        private void LoadHistory()
        {
            if (settings == null) return;
            string path = ResolveUserPath(settings.HistoryPath);
            if (!File.Exists(path)) return;
            try { var loaded = JsonUtility.FromJson<ComfyRecordList>(File.ReadAllText(path)); if (loaded != null && loaded.records != null) history.AddRange(loaded.records); }
            catch (Exception exception) { Debug.LogWarning("Could not load ComfyUI history: " + exception.Message); }
        }

        private static string ResolveUserPath(string path) => Path.IsPathRooted(path) ? path : Path.Combine(Application.persistentDataPath, path);
        private static string SafeName(string value) { foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_'); return value.Replace(' ', '_'); }
        private static string JsonString(string value)
        {
            string json = JsonUtility.ToJson(new StringBox { value = value });
            const string prefix = "{\"value\":";
            return json.Substring(prefix.Length, json.Length - prefix.Length - 1);
        }
        [Serializable] private sealed class StringBox { public string value; }

        private static string JsonField(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            string token = "\"" + key + "\"";
            int i = json.IndexOf(token, StringComparison.Ordinal); if (i < 0) return null;
            i = json.IndexOf(':', i + token.Length); if (i < 0) return null;
            i++; while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length || json[i] != '"') return null;
            i++; var result = new StringBuilder(); bool escape = false;
            for (; i < json.Length; i++) { char c = json[i]; if (escape) { result.Append(c); escape = false; } else if (c == '\\') escape = true; else if (c == '"') break; else result.Append(c); }
            return result.ToString();
        }

        private static string JsonObjectForKey(string json, string key)
        {
            string token = "\"" + key + "\""; int i = json.IndexOf(token, StringComparison.Ordinal); if (i < 0) return null;
            i = json.IndexOf('{', i + token.Length); if (i < 0) return null;
            int start = i, depth = 0; bool quoted = false, escape = false;
            for (; i < json.Length; i++) { char c = json[i]; if (quoted) { if (escape) escape = false; else if (c == '\\') escape = true; else if (c == '"') quoted = false; continue; } if (c == '"') quoted = true; else if (c == '{') depth++; else if (c == '}' && --depth == 0) return json.Substring(start, i - start + 1); }
            return null;
        }

        private static bool TrySetWorkflowValue(ref string workflow, string nodeId, string inputName, string value)
        {
            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(inputName)) return false;
            string nodeToken = "\"" + nodeId + "\""; int nodeStart = workflow.IndexOf(nodeToken, StringComparison.Ordinal); if (nodeStart < 0) return false;
            int nodeEnd = workflow.IndexOf("},", nodeStart, StringComparison.Ordinal); if (nodeEnd < 0) nodeEnd = workflow.Length;
            string segment = workflow.Substring(nodeStart, nodeEnd - nodeStart + 1);
            string inputToken = "\"" + inputName + "\""; int inputIndex = segment.IndexOf(inputToken, StringComparison.Ordinal); if (inputIndex < 0) return false;
            int valueStart = segment.IndexOf(':', inputIndex + inputToken.Length); if (valueStart < 0) return false; valueStart++;
            while (valueStart < segment.Length && char.IsWhiteSpace(segment[valueStart])) valueStart++;
            int valueEnd;
            if (valueStart < segment.Length && segment[valueStart] == '"') { valueEnd = valueStart + 1; bool escaped = false; for (; valueEnd < segment.Length; valueEnd++) { if (escaped) escaped = false; else if (segment[valueEnd] == '\\') escaped = true; else if (segment[valueEnd] == '"') { valueEnd++; break; } } }
            else { valueEnd = valueStart; while (valueEnd < segment.Length && segment[valueEnd] != ',' && segment[valueEnd] != '}') valueEnd++; }
            string replacement = inputName == "seed" || inputName.Contains("seed") ? value : JsonString(value);
            segment = segment.Substring(0, valueStart) + replacement + segment.Substring(valueEnd);
            workflow = workflow.Substring(0, nodeStart) + segment + workflow.Substring(nodeEnd + 1);
            return true;
        }
    }
}
