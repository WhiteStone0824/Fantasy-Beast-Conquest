using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FusionAnyMonster.Editor
{
    public static class FamilyBreedingSetup
    {
        [MenuItem("Fusion Any Monster/Breeding/Apply Family Combination Table")]
        public static void ApplyAndExport()
        {
            var rules=FamilyBreedingCatalog.CreateRules();
            foreach(string path in new[]{"Assets/Game/Resources/BreedingRuleDatabase.asset","Assets/Game/Data/Breeding/BreedingRuleDatabase.asset"})
            {
                var database=AssetDatabase.LoadAssetAtPath<BreedingRuleDatabase>(path);
                if(database==null){database=ScriptableObject.CreateInstance<BreedingRuleDatabase>();AssetDatabase.CreateAsset(database,path);}
                database.familyRules=FamilyBreedingCatalog.CreateRules();EditorUtility.SetDirty(database);AssetDatabase.SaveAssetIfDirty(database);
            }
            var markdown=new StringBuilder("# 系統の配合一覧（全496通り）\n\nA＋BとB＋Aは同じ結果です。既存30系統の465通りに真理系との31通りを追加しています。\n\n真理系を親に使うと真理系になります。特別な祖父母レシピでも真理系の個体が誕生します。\n\n[31×31の早見表（CSV）](BreedingFamilyMatrix.csv)／[496レシピのCSV](BreedingFamilyRecipes.csv)\n\n");
            markdown.AppendLine("既存30系統の組み合わせは各系統15～16通りになるよう調整しています。[偏りの比較・設計意図](BreedingFamilyBalance.md)。\n");
            var recipes=new StringBuilder("親系統A,親系統B,子の系統,考え方\n");
            var matrix=new StringBuilder("親A／親B");var families=(SpeciesFamily[])Enum.GetValues(typeof(SpeciesFamily));
            foreach(var family in families)matrix.Append(',').Append(FamilyBreedingCatalog.DisplayName(family));matrix.AppendLine();
            foreach(var a in families)
            {
                string name=FamilyBreedingCatalog.DisplayName(a);markdown.Append("## ").Append(name).AppendLine("との組み合わせ\n\nこの見出しより前の系統との組み合わせは、その系統の表に掲載しています。\n\n| 親A | 親B | 子の系統 | 考え方 |\n|---|---|---|---|");
                matrix.Append(name);
                foreach(var b in families)
                {
                    string second=FamilyBreedingCatalog.DisplayName(b),result=FamilyBreedingCatalog.DisplayName(FamilyBreedingCatalog.Resolve(a,b));
                    matrix.Append(',').Append(result);
                    if((int)b<(int)a)continue;
                    string reason=FamilyBreedingExplanation.Explain(a,b,FamilyBreedingCatalog.Resolve(a,b));
                    markdown.Append('|').Append(name).Append('|').Append(second).Append('|').Append(result).Append('|').Append(reason).AppendLine("|");
                    recipes.Append(name).Append(',').Append(second).Append(',').Append(result).Append(',').AppendLine(reason);
                }
                markdown.AppendLine();matrix.AppendLine();
            }
            Directory.CreateDirectory("Docs");WriteDocument("Docs/BreedingFamilyTable.md",markdown.ToString(),new UTF8Encoding(false));
            var utf8=new UTF8Encoding(true);WriteDocument("Docs/BreedingFamilyRecipes.csv",recipes.ToString(),utf8);WriteDocument("Docs/BreedingFamilyMatrix.csv",matrix.ToString(),utf8);
            Debug.Log("Family breeding table applied and exported: "+families.Length+" families, "+rules.Length+" unordered recipes.");
        }

        private static void WriteDocument(string path,string contents,Encoding encoding)
        {
            // Allow an IDE or spreadsheet preview to keep reading the exported table.
            try
            {
                using(var stream=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.ReadWrite))
                using(var writer=new StreamWriter(stream,encoding))writer.Write(contents);
            }
            catch(IOException exception)
            {
                string alternate=Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+".updated"+Path.GetExtension(path));
                File.WriteAllText(alternate,contents,encoding);
                Debug.LogWarning(path+" を更新できなかったため、最新版を "+alternate+" に出力しました。ファイルを開いているアプリを閉じて再実行してください。 "+exception.Message);
            }
        }
    }
}
