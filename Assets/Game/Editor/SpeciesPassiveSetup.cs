using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FusionAnyMonster.Editor
{
    public static class SpeciesPassiveSetup
    {
        [MenuItem("Fusion Any Monster/Setup/Assign Species Passives")]
        public static void Assign()
        {
            var species=new List<SpeciesDefinition>();var passives=new List<SpeciesPassive>();var names=new HashSet<string>();
            foreach(string guid in AssetDatabase.FindAssets("t:SpeciesDefinition",new[]{"Assets/Game/Data/Species"}))
            {
                var definition=AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if(definition==null)continue;
                SpeciesPassive passive=SpeciesPassiveCatalog.Create(definition);
                if(!names.Add(passive.displayName))throw new InvalidOperationException("Duplicate passive name: "+passive.displayName);
                species.Add(definition);passives.Add(passive);
            }
            if(species.Count!=420)throw new InvalidOperationException("Expected 420 species, found "+species.Count);
            for(int i=0;i<species.Count;i++){species[i].passive=passives[i];EditorUtility.SetDirty(species[i]);}
            AssetDatabase.SaveAssets();
            UpdateSharedAxes(species);
            species.Sort((a,b)=>{int family=a.family.CompareTo(b.family);if(family!=0)return family;int rank=a.rank.CompareTo(b.rank);return rank!=0?rank:string.CompareOrdinal(a.speciesId,b.speciesId);});
            var table=new StringBuilder("# 種族パッシブ一覧\n\n全420種。同じ種族の個体は共通のパッシブを持ち、配合した子は子の種族のパッシブを持ちます。\n\n属性耐性・状態異常抵抗の加算は百分率ポイント、上限100%。属性耐性は属性ダメージ軽減にのみ加算します。MP軽減は対象属性だけに適用し、最大MPによる割引は行いません。\n\n| 系統 | ランク | 種族ID | 種族 | パッシブ | 効果 |\n|---|---|---|---|---|---|\n");
            foreach(var definition in species)table.Append('|').Append(definition.family).Append('|').Append(definition.rank).Append('|').Append(definition.speciesId).Append('|').Append(definition.displayName).Append('|').Append(definition.passive.displayName).Append('|').Append(definition.passive.Description.Replace("\n","<br>")).AppendLine("|");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs"));Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"SpeciesPassives.md"),table.ToString(),new UTF8Encoding(false));
            Debug.Log("Assigned 420 species passives and exported Docs/SpeciesPassives.md.");
        }
        private static void UpdateSharedAxes(List<SpeciesDefinition> species)
        {
            var settings=Resources.Load<UI.StatBarScaleSettings>("StatBarScaleSettings");if(settings==null)return;
            var personalities=Resources.Load<PersonalityDatabase>("PersonalityDatabase");var unit=new UnitInstance {level=99,iv=new IndividualValues()};
            for(int i=0;i<10;i++)unit.iv.Set(i,100);
            foreach(var definition in species)foreach(PersonalityType personality in Enum.GetValues(typeof(PersonalityType)))
            {
                unit.speciesId=definition.speciesId;unit.personality=personality;UnitStats stats=UnitStatsCalculator.Calculate(definition,unit,null,personalities);
                for(int i=0;i<10;i++)if(stats.Get(i)>settings.GetMaximum(i))
                {float step=i<2?100:i<6?50:20;settings.maximumValues.Set(i,Mathf.Ceil(stats.Get(i)/step)*step);EditorUtility.SetDirty(settings);}
            }
            AssetDatabase.SaveAssets();
        }
    }
}
