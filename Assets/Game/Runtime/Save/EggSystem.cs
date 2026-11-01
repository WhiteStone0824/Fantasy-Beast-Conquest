using System;
using System.Collections.Generic;
using UnityEngine;

namespace FusionAnyMonster.Save
{
    public static class EggSystem
    {
        public delegate bool SaveWriter(GameSaveData data, out string error);

        public static bool TryAward(GameSaveData source, UnitSaveData boss, out GameSaveData committed, out string error, SaveWriter writer = null)
        {
            committed = null;
            error = null;
            if (source == null || source.readOnly || source.schemaVersion > SaveSystem.CurrentSchemaVersion)
            { error = "セーブデータが書き込み禁止のため、卵を保存できません。"; return false; }
            if (boss == null || string.IsNullOrWhiteSpace(boss.id) || string.IsNullOrWhiteSpace(boss.speciesId))
            { error = "敵の個体データがないため、卵を作れません。"; return false; }
            if (source.eggs != null && source.eggs.Exists(e => e != null && e.id == boss.id))
            { error = "この敵の卵は獲得済みです。"; return false; }
            var candidate = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(source));
            if (candidate.eggs == null) candidate.eggs = new List<EggSaveData>();
            var offspring = JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(boss));
            offspring.deployed = false;
            candidate.eggs.Add(new EggSaveData { id = boss.id, offspring = offspring, acquiredAtUtcTicks = DateTime.UtcNow.Ticks });
            if (!(writer ?? SaveSystem.Save)(candidate, out error)) return false;
            committed = candidate;
            return true;
        }

        public static bool TryHatch(GameSaveData source, string eggId, out GameSaveData committed, out UnitSaveData hatched, out string error, SaveWriter writer = null)
        {
            committed = null; hatched = null; error = null;
            if (source == null || source.readOnly || source.schemaVersion > SaveSystem.CurrentSchemaVersion)
            { error = "セーブデータが書き込み禁止のため、孵化できません。"; return false; }
            EggSaveData egg = source.eggs == null ? null : source.eggs.Find(e => e != null && e.id == eggId);
            if (egg == null || egg.offspring == null || string.IsNullOrWhiteSpace(egg.offspring.speciesId))
            { error = "卵のデータを読み込めません。"; return false; }
            var candidate = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(source));
            if (candidate.units == null) candidate.units = new List<UnitSaveData>();
            var unit = JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(egg.offspring));
            unit.id = Guid.NewGuid().ToString("N"); unit.deployed = false; unit.level = 1; unit.experience = 0;
            candidate.units.Add(unit);
            candidate.eggs.RemoveAll(e => e != null && e.id == eggId);
            SaveSystem.RecordOwnedSpecies(candidate);
            if (!(writer ?? SaveSystem.Save)(candidate, out error)) return false;
            committed = candidate; hatched = unit;
            return true;
        }
    }
}
