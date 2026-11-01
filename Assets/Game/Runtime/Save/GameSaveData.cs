using System;
using System.Collections.Generic;

namespace FusionAnyMonster.Save
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int schemaVersion = SaveSystem.CurrentSchemaVersion;
        [NonSerialized] public bool readOnly;
        [NonSerialized] public string readOnlyReason;
        public int randomSeed = 12345;
        public List<UnitSaveData> units = new List<UnitSaveData>();
        public List<EggSaveData> eggs = new List<EggSaveData>();
        public List<EquipmentItem> equipmentInventory = new List<EquipmentItem>();
        public bool equipmentInitialized;
        public bool autoDiscardOldEquipment;
        public List<string> formationUnitIds = new List<string>();
        public List<string> discoveredSpeciesIds = new List<string>();
        public List<AncestorRecord> ancestry = new List<AncestorRecord>();
        public string comfyEndpoint = "http://127.0.0.1:8188";
        public string comfyWorkflowPath = "";
        public int battlesCompleted;
        public int unlockedRegularDifficulty;
        public int unlockedBossRushDifficulty;
        public double playTimeSeconds;
        public string limitedStageDate;
        public int weekdayStageAttempts;
        public int dailyStageAttempts;
        public int dailyBossAttempts;
        public int dailyExperienceAttempts;
        public int dailySuperExperienceAttempts;
        public int limitedStageCountersVersion;
        public long savedAtUtcTicks;
    }

    [Serializable]
    public sealed class AncestorRecord
    {
        public string id, speciesId, displayName, family, rank, fatherId, motherId;
        public int generation;
    }

    [Serializable]
    public sealed class EggSaveData
    {
        public string id;
        public UnitSaveData offspring;
        public long acquiredAtUtcTicks;
    }

    [Serializable]
    public sealed class UnitSaveData
    {
        public string id;
        public string speciesId;
        public string displayName;
        public string generatedSpritePath;
        public string family;
        public string rank;
        public int level = 1;
        public long experience;
        public string personality;
        public string lineageJson = "{}";
        public int[] individualValues = new int[10];
        public float[] baseStats = new float[10];
        public float[] attributeDamageReduction = new float[7];
        public float[] attributeAilmentResistance = new float[7];
        public float[] debuffResistance = new float[8];
        public string[] magicGenomeJson = Array.Empty<string>();
        public string[] magicSlotGenomeJson = Array.Empty<string>();
        public string fatherId;
        public string motherId;
        public int generation;
        public string mutationHistory = "";
        public bool deployed;
        public EquipmentItem equipment;
    }
}
