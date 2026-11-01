using System;
using System.Collections.Generic;
using System.IO;
using FusionAnyMonster;
using UnityEngine;

namespace FusionAnyMonster.Save
{
    public static class SaveSystem
    {
        public const string FileName = "fusion_any_monster_save.json";
        public const int CurrentSchemaVersion=7;
        public static string SavePath { get { return Path.Combine(Application.persistentDataPath, FileName); } }
        public static event Action<GameSaveData> Saved;
        public static event Action<GameSaveData> Loaded;

        // Only create a starter party when no save exists. An empty existing roster is legitimate.
        public static GameSaveData LoadOrCreate()
        {
            if(File.Exists(SavePath))return Load();
            var database=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            if(database==null)database=DemoDataFactory.CreateSpeciesDatabase();
            GameSaveData data;
            try { data=CreateStarterData(database); }
            catch(Exception exception)
            {
                Debug.LogError("Could not create starter party: "+exception.Message);
                return new GameSaveData {readOnly=true,readOnlyReason="初期仲間の種族データを読み込めませんでした。"};
            }
            if(!Save(data,out string error))Debug.LogError("Could not save starter party: "+error);
            return data;
        }

        public static GameSaveData CreateStarterData(SpeciesDatabase database)
        {
            if(database==null)throw new ArgumentNullException(nameof(database));
            string[] ids={"demo_00","species_F01_R0_A","species_F19_R0_A"};
            PersonalityType[] personalities={PersonalityType.Cautious,PersonalityType.Brave,PersonalityType.Gentle};
            var data=new GameSaveData();
            for(int i=0;i<ids.Length;i++)
            {
                var species=database.GetById(ids[i]);
                if(species==null)throw new InvalidDataException("Missing starter species: "+ids[i]);
                var unit=WildUnitFactory.Create(species,1,32001+i);
                unit.personality=personalities[i].ToString();unit.deployed=true;
                data.units.Add(unit);data.formationUnitIds.Add(unit.id);
            }
            RecordOwnedSpecies(data);
            return data;
        }

        public static GameSaveData Load()
        {
            GameSaveData data;
            try
            {
                if (!File.Exists(SavePath)) data = new GameSaveData();
                else
                {
                    data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));
                    if (data == null) throw new InvalidDataException("The save file does not contain game data.");
                    else data.readOnly = data.schemaVersion > CurrentSchemaVersion;
                    Normalize(data);
                    string magicError;
                    if(!ValidateMagicData(data,out magicError)){data.readOnly=true;data.readOnlyReason=magicError;}
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to load save data: " + exception.Message);
                // Preserve the original file: startup must not replace an unreadable save with demo data.
                data = new GameSaveData {readOnly=true,readOnlyReason="保存データを読み込めませんでした。元のファイルは保護されています。復元してから再読み込みしてください。"};
            }
            Loaded?.Invoke(data);
            return data;
        }

        public static bool Save(GameSaveData data, out string error)
        { return SaveCore(data,out error,false); }

        public static bool ResetToStarter(SpeciesDatabase database,out GameSaveData starter,out string error)
        {
            starter=null;error=null;
            GameSaveData candidate;
            try { candidate=CreateStarterData(database); }
            catch(Exception exception) { error=exception.Message;return false; }
            if(!SaveCore(candidate,out error,true))return false;
            starter=candidate;return true;
        }

        private static bool SaveCore(GameSaveData data, out string error, bool resetPlayTime)
        {
            error = null;
            if (data == null) { error = "Save data is null."; return false; }
            if (data.readOnly || data.schemaVersion > CurrentSchemaVersion) { error = string.IsNullOrEmpty(data.readOnlyReason)?"This save was created by a newer game version and is read-only.":data.readOnlyReason; return false; }
            if(!ValidateMagicData(data,out error))return false;
            string temporary = SavePath + ".tmp";
            double previousPlayTime=data.playTimeSeconds;
            try
            {
                Normalize(data);
                data.playTimeSeconds=resetPlayTime?0:PlayerPlayTime.Current(data);
                data.schemaVersion = CurrentSchemaVersion;
                data.savedAtUtcTicks = DateTime.UtcNow.Ticks;
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(SavePath)) File.Replace(temporary, SavePath, null);
                else File.Move(temporary, SavePath);
                if(resetPlayTime)PlayerPlayTime.Restart();else PlayerPlayTime.Commit(data.playTimeSeconds);
                Saved?.Invoke(data);
                return true;
            }
            catch (Exception exception)
            {
                data.playTimeSeconds=previousPlayTime;
                error = exception.Message;
                Debug.LogError("Failed to save game: " + error);
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
                return false;
            }
        }

        public static GameSaveData CreateDemoData()
        {
            var data = new GameSaveData();
            string[] names = { "炎スライム", "苔牙獣", "疾風フィンチ", "岩甲ガメ", "潮トカゲ", "歯車の子", "鬼火", "茨っ子", "影小鬼", "星竜" };
            string[] families = { "Slime", "Beast", "Bird", "Crustacean", "Amphibian", "Machine", "Spirit", "Plant", "Demon", "Dragon" };
            string[] ranks = { "F", "E", "D", "C", "D", "C", "B", "C", "B", "A" };
            for (int i = 0; i < names.Length; i++)
            {
                var unit = new UnitSaveData { id = "demo-" + i, speciesId = "demo_species_" + i, displayName = names[i], family = families[i], rank = ranks[i], level = 1 + (i % 7), personality = i % 2 == 0 ? "Brave" : "Cautious", deployed = i < 3 };
                for (int stat = 0; stat < IndividualValues.StatCount; stat++) { unit.individualValues[stat] = (i * 7 + stat * 3) % (IndividualValues.MaxValue + 1); unit.baseStats[stat] = 20 + i * 2 + stat * 3; }
                var genome=DemoDataFactory.DemoGenome(i);
                if(i==4 || i==6 || i==7 || i==8)
                {
                    var genes=Spells.SpellProgramBuilder.Projectile(AttributeElement.Light,24,1,Spells.SpellTrajectory.Straight,
                        i==4?Spells.SpellNodeKind.Heal:Spells.SpellNodeKind.ApplyBuff,Spells.SpellTarget.LowestHealthAlly,true);
                    if(i!=4)foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB})
                    {p.nodes[2].parameters.buff=i==6?BuffType.AttackBoost:i==7?BuffType.DefenseBoost:BuffType.Haste;p.nodes[2].parameters.multiplier=.2f;p.nodes[2].parameters.seconds=6;}
                    genome=Spells.SpellSlotStorage.Prepare(new MagicGenome{programGenes=genes},i);
                }
                unit.magicGenomeJson = new[] { JsonUtility.ToJson(genome) };
                data.units.Add(unit);
                if (unit.deployed) data.formationUnitIds.Add(unit.id);
            }
            return data;
        }

        private static bool ValidateMagicData(GameSaveData data,out string error)
        {
            error=null;
            var records=new List<UnitSaveData>();
            if(data.units!=null) records.AddRange(data.units);
            if(data.eggs!=null) foreach(var egg in data.eggs) if(egg!=null && egg.offspring!=null) records.Add(egg.offspring);
            foreach(var unit in records)
            {
                if(unit==null)continue;
                foreach(var slots in new[]{unit.magicGenomeJson,unit.magicSlotGenomeJson})
                {
                    if(slots==null)continue;
                    for(int i=0;i<slots.Length;i++)
                        if(!string.IsNullOrWhiteSpace(slots[i]) && !MagicGenome.TryParseJson(slots[i],out _))
                        {error="GP形式ではない、または破損した魔法データがあるため、セーブを保護しています。個体ID: "+unit.id+" / 魔法 "+(i+1);return false;}
                }
            }
            return true;
        }

        // Persist acquisition history separately from the living roster (breeding consumes parents).
        public static bool RecordOwnedSpecies(GameSaveData data)
        {
            if (data == null) return false;
            bool changed = data.discoveredSpeciesIds == null;
            var known = new HashSet<string>(StringComparer.Ordinal);
            var history = new List<string>();
            if (data.discoveredSpeciesIds != null)
                foreach (string id in data.discoveredSpeciesIds)
                    if (!string.IsNullOrWhiteSpace(id) && known.Add(id)) history.Add(id);
                    else changed = true;
            if (data.units != null)
                foreach (UnitSaveData unit in data.units)
                    if (unit != null && !string.IsNullOrWhiteSpace(unit.speciesId) && known.Add(unit.speciesId))
                    { history.Add(unit.speciesId); changed = true; }
            if (changed) data.discoveredSpeciesIds = history;
            return changed;
        }

        private static void Normalize(GameSaveData data)
        {
            if(data.equipmentInventory==null)data.equipmentInventory=new List<EquipmentItem>();
            if(!data.equipmentInitialized)
            {
                for(int i=0;i<3;i++)data.equipmentInventory.Add(EquipmentCatalog.Roll(2100+i,1));
                data.equipmentInitialized=true;
            }
            RecordOwnedSpecies(data);
            if (data.units == null) data.units = new System.Collections.Generic.List<UnitSaveData>();
            if (data.eggs == null) data.eggs = new List<EggSaveData>();
            if (data.formationUnitIds == null) data.formationUnitIds = new System.Collections.Generic.List<string>();
            else data.formationUnitIds.Clear();
            if (string.IsNullOrEmpty(data.comfyEndpoint)) data.comfyEndpoint = "http://127.0.0.1:8188";
            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = data.units.Count - 1; index >= 0; index--)
            {
                UnitSaveData unit = data.units[index];
                if (unit == null) { data.units.RemoveAt(index); continue; }
                if(!EquipmentCatalog.HasItem(unit.equipment))unit.equipment=null;
                UpdateReassignedSpeciesName(unit);
                if (string.IsNullOrWhiteSpace(unit.id) || !usedIds.Add(unit.id))
                {
                    do { unit.id = Guid.NewGuid().ToString("N"); }
                    while (!usedIds.Add(unit.id));
                }
                unit.individualValues = Resize(unit.individualValues, 10);
                for (int stat = 0; stat < unit.individualValues.Length; stat++) unit.individualValues[stat] = IndividualValues.Clamp(unit.individualValues[stat]);
                unit.baseStats = Resize(unit.baseStats, 10);
                unit.attributeDamageReduction = Resize(unit.attributeDamageReduction, 7);
                unit.attributeAilmentResistance = Resize(unit.attributeAilmentResistance, 7);
                unit.debuffResistance = Resize(unit.debuffResistance, 8);
                if (unit.magicGenomeJson == null) unit.magicGenomeJson = Array.Empty<string>();
                Spells.SpellSlotStorage.EnsureSavedSlots(unit);
                unit.level = UnityEngine.Mathf.Clamp(unit.level, 1, 99);
                unit.experience = Math.Max(0, unit.experience);
                if (unit.level >= 99) unit.experience = 0;
                SpeciesRank rank;
                if (!Enum.TryParse(unit.rank, true, out rank) || !Enum.IsDefined(typeof(SpeciesRank), rank)) unit.rank = SpeciesRank.F.ToString();
                PersonalityType personality;
                if (!Enum.TryParse(unit.personality, true, out personality) || !Enum.IsDefined(typeof(PersonalityType), personality)) unit.personality = PersonalityType.Brave.ToString();
            }
            for (int i = 0; i < data.units.Count; i++)
                if (data.units[i].deployed) data.formationUnitIds.Add(data.units[i].id);
            foreach(var unit in data.units)AncestrySystem.Record(data,unit);
        }

        private static void UpdateReassignedSpeciesName(UnitSaveData unit)
        {
            // Preserve custom names; only replace the previous default for this exact species ID.
            if(unit.speciesId=="species_F11_R2_B" && unit.displayName=="溶岩タイタン")unit.displayName="雷鳴の巨兵";
            else if(unit.speciesId=="species_F11_R4_A" && unit.displayName=="雷鳴の巨兵")unit.displayName="溶岩タイタン";
            else if(unit.speciesId=="species_F21_R4_B" && unit.displayName=="白銀の熾天使")unit.displayName="暁光の大天使";
            else if(unit.speciesId=="species_F21_R5_A" && unit.displayName=="暁光の大天使")unit.displayName="白銀の熾天使";
        }
        private static int[] Resize(int[] input, int length) { var result = new int[length]; if (input != null) Array.Copy(input, result, Math.Min(input.Length, length)); return result; }
        private static float[] Resize(float[] input, int length) { var result = new float[length]; if (input != null) Array.Copy(input, result, Math.Min(input.Length, length)); return result; }
    }
}

