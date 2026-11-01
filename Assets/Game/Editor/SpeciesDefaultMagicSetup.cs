using System;
using System.IO;
using System.Text;
using FusionAnyMonster.Spells;
using UnityEditor;
using UnityEngine;

namespace FusionAnyMonster.Editor
{
    public static class SpeciesDefaultMagicSetup
    {
        [MenuItem("Fusion Any Monster/Spells/Fill Missing Species Default Magic")]
        public static void FillMissing()
        {
            int speciesCount=0,slots=0;
            var report=new StringBuilder("# 全種族のデフォルト魔法\n\n種族マスターに保存したGP魔法です。内部5枠のうちランクに対応する1～5枠を敵が使用し、捕獲後も引き継ぎます。既存の所持個体・配合結果は変更しません。\n\n| 種族ID | 種族 | 系統 | ランク | 使用する魔法 |\n|---|---|---|---|---|\n");
            foreach(string guid in AssetDatabase.FindAssets("t:SpeciesDefinition",new[]{"Assets/Game"}))
            {
                var species=AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var old=species.defaultMagicGenomes;var defaults=new MagicGenome[SpellSlotStorage.SlotCount];bool changed=old==null || old.Length!=defaults.Length;
                for(int i=0;i<defaults.Length;i++)
                {
                    var existing=old!=null && i<old.Length?old[i]:null;
                    if(existing!=null && existing.HasProgram){defaults[i]=existing;continue;}
                    defaults[i]=SpeciesDefaultMagicCatalog.Create(species,i);changed=true;slots++;
                }
                species.defaultMagicGenomes=defaults;
                foreach(var genome in defaults)MagicFactory.Build(genome,species);
                if(changed){EditorUtility.SetDirty(species);AssetDatabase.SaveAssetIfDirty(species);}
                speciesCount++;report.Append('|').Append(species.speciesId).Append('|').Append(species.displayName).Append('|').Append(species.family).Append('|').Append(species.rank).Append('|');
                for(int i=0;i<RankRules.MagicSlots(species.rank);i++){if(i>0)report.Append(" / ");report.Append(MagicFactory.Build(defaults[i],species).displayName);}
                report.AppendLine("|");
            }
            Directory.CreateDirectory("Docs");File.WriteAllText("Docs/SpeciesDefaultMagic.md",report.ToString());
            Debug.Log("Species default magic assigned: "+speciesCount+" species, "+slots+" new GP slots.");
        }
    }
}
