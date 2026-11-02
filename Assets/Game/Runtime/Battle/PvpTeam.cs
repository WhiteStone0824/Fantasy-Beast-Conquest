using System;
using System.Collections.Generic;
using FusionAnyMonster.Save;
using UnityEngine;

namespace FusionAnyMonster.Battle
{
    // A detached snapshot: receiving a team must never mutate the owner's save.
    [Serializable]
    public sealed class PvpTeam
    {
        public const int MaxUnits = 5;
        public int playerRank;
        public List<UnitSaveData> units = new List<UnitSaveData>();

        public static bool TryCreate(GameSaveData save, SpeciesDatabase species, out PvpTeam team, out string error)
        {
            team = null;
            if (save == null || save.units == null) { error = "仲間データがありません。"; return false; }
            var candidate = new PvpTeam { playerRank=PlayerRankCalculator.Calculate(save).Total };
            foreach (var unit in save.units)
                if (unit != null && unit.deployed)
                    candidate.units.Add(JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(unit)));
            if (!candidate.Validate(species, out error)) return false;
            team = candidate;
            return true;
        }

        public bool Validate(SpeciesDatabase species, out string error)
        {
            error = null;
            if(playerRank<0){error="プレイヤーランクが不正です。";return false;}
            if (species == null || units == null || units.Count < 1 || units.Count > MaxUnits)
            { error = "対戦には1〜5体の仲間が必要です。"; return false; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var speciesIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var unit in units)
            {
                if (unit == null || string.IsNullOrWhiteSpace(unit.id) || !ids.Add(unit.id) ||
                    string.IsNullOrWhiteSpace(unit.speciesId) || species.GetById(unit.speciesId) == null ||
                    unit.level < 1 || unit.level > 99 || unit.individualValues == null || unit.individualValues.Length != IndividualValues.StatCount ||
                    unit.attributeDamageReduction == null || unit.attributeDamageReduction.Length != 7 ||
                    unit.attributeAilmentResistance == null || unit.attributeAilmentResistance.Length != 7 ||
                    unit.debuffResistance == null || unit.debuffResistance.Length != 8 || unit.baseStats == null || unit.baseStats.Length != 10)
                { error = "編成に無効な個体データがあります。"; return false; }
                if(!speciesIds.Add(unit.speciesId))
                { var duplicate=species.GetById(unit.speciesId);error="通信対戦では同じ種族は1体までです："+(duplicate==null?unit.speciesId:duplicate.displayName);return false; }
                foreach (int iv in unit.individualValues) if (iv < 0 || iv > IndividualValues.MaxValue)
                { error = "個体値が有効範囲外です。"; return false; }
                if (!EquipmentCatalog.IsValid(unit.equipment)) { error = "装備データが不正です。"; return false; }
                foreach (var slots in new[] { unit.magicGenomeJson, unit.magicSlotGenomeJson })
                {
                    if (slots == null) continue;
                    if (slots.Length > MaxUnits) { error = "魔法スロット数が不正です。"; return false; }
                    foreach (string json in slots)
                        if (!string.IsNullOrWhiteSpace(json) && !MagicGenome.TryParseJson(json, out _))
                        { error = "魔法データを読み込めません。"; return false; }
                }
            }
            return true;
        }
    }
}
