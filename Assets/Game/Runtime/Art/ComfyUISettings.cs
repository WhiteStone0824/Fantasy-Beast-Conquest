using System;
using System.Collections.Generic;
using UnityEngine;

namespace FusionAnyMonster.Art
{
    [CreateAssetMenu(menuName = "Fusion Any Monster/ComfyUI Settings", fileName = "ComfyUISettings")]
    public sealed class ComfyUISettings : ScriptableObject
    {
        [SerializeField] private string endpoint = "http://127.0.0.1:8188";
        [SerializeField] private string workflowJsonPath = "Assets/Game/Art/workflow_api.json";
        [SerializeField] private string promptLogPath = "FusionAnyMonster/ComfyUI/prompts.jsonl";
        [SerializeField] private string historyPath = "FusionAnyMonster/ComfyUI/history.json";
        [SerializeField] private string generatedAssetsFolder = "Assets/Game/Sprites/Generated";
        [SerializeField] private string positivePromptNodeId = "6";
        [SerializeField] private string positivePromptInput = "text";
        [SerializeField] private string negativePromptNodeId = "7";
        [SerializeField] private string negativePromptInput = "text";
        [SerializeField] private string seedNodeId = "3";
        [SerializeField] private string seedInput = "seed";
        [SerializeField] private float pollIntervalSeconds = 1f;
        [SerializeField] private int timeoutSeconds = 600;

        public string Endpoint => string.IsNullOrWhiteSpace(endpoint) ? "http://127.0.0.1:8188" : endpoint.TrimEnd('/');
        public void SetEndpoint(string value) { if (!string.IsNullOrWhiteSpace(value)) endpoint = value.TrimEnd('/'); }
        public string WorkflowJsonPath => workflowJsonPath;
        public string PromptLogPath => promptLogPath;
        public string HistoryPath => historyPath;
        public string GeneratedAssetsFolder => generatedAssetsFolder;
        public string PositivePromptNodeId => positivePromptNodeId;
        public string PositivePromptInput => positivePromptInput;
        public string NegativePromptNodeId => negativePromptNodeId;
        public string NegativePromptInput => negativePromptInput;
        public string SeedNodeId => seedNodeId;
        public string SeedInput => seedInput;
        public float PollIntervalSeconds => Mathf.Max(.25f, pollIntervalSeconds);
        public int TimeoutSeconds => Mathf.Clamp(timeoutSeconds, 30, 3600);
    }

    [Serializable]
    public sealed class ComfyPromptRecord
    {
        public string id;
        public string createdUtc;
        public string speciesId;
        public string positive;
        public string negative;
        public long seed;
        public string promptId;
        public string status;
        public string outputAssetPath;
        public string error;
    }

    [Serializable]
    internal sealed class ComfyRecordList { public List<ComfyPromptRecord> records = new List<ComfyPromptRecord>(); }
}
