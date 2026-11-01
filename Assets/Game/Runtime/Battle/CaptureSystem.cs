using System;
using FusionAnyMonster.Save;
using UnityEngine;

namespace FusionAnyMonster.Battle
{
    public enum CaptureStatus { Success, Failed, Unavailable, SaveFailed }
    public struct CaptureResult
    {
        public CaptureStatus status;public string message;public float chance;public UnitSaveData unit;
    }
    public static class CaptureSystem
    {
        public delegate bool SaveWriter(GameSaveData data,out string error);
        public static CaptureResult TryCapture(GameSaveData source,UnitSaveData wild,float healthRatio,bool boss,
            bool attempted,int captured,System.Random random,CaptureRules rules,out GameSaveData committed,SaveWriter write=null)
        {
            committed=null;rules=rules??CaptureRules.Current;
            if(source==null || source.readOnly || source.schemaVersion>SaveSystem.CurrentSchemaVersion)
                return Unavailable("セーブを保護しているため、捕獲できません。");
            if(wild==null || string.IsNullOrWhiteSpace(wild.id) || string.IsNullOrWhiteSpace(wild.speciesId))return Unavailable("この敵は捕獲できません。");
            if(attempted)return Unavailable("この敵への捕獲は挑戦済みです。");
            if(captured>=Mathf.Max(1,rules.maximumCapturesPerBattle))return Unavailable("今回の捕獲上限に達しました。");
            if(source.units!=null && source.units.Exists(u=>u!=null && u.id==wild.id))return Unavailable("すでに仲間になっています。");
            float chance=rules.Chance(healthRatio,boss);
            if(chance<=0)return Unavailable("敵を弱らせてから捕獲してください。");
            if(random==null)throw new ArgumentNullException(nameof(random));
            if(random.NextDouble()>=chance)return new CaptureResult{status=CaptureStatus.Failed,chance=chance,message="捕獲に失敗しました。この敵には再挑戦できません。"};
            // Stage every change in a copy. A failed disk write must leave both the roster and enemy intact.
            var candidate=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(source));
            if(candidate.units==null)candidate.units=new System.Collections.Generic.List<UnitSaveData>();
            var recruit=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(wild));recruit.deployed=false;
            candidate.units.Add(recruit);SaveSystem.RecordOwnedSpecies(candidate);
            string error;var writer=write??SaveSystem.Save;
            if(!writer(candidate,out error))return new CaptureResult{status=CaptureStatus.SaveFailed,chance=chance,message="仲間を保存できませんでした。再試行してください。"};
            committed=candidate;
            return new CaptureResult{status=CaptureStatus.Success,chance=chance,unit=recruit,message=recruit.displayName+"が仲間になりました！"};
        }
        private static CaptureResult Unavailable(string message){return new CaptureResult{status=CaptureStatus.Unavailable,message=message};}
    }
}
