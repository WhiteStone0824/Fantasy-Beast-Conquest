using UnityEngine;
using FusionAnyMonster.Battle;

namespace FusionAnyMonster
{


    /// <summary>Same-kind buffs refresh duration and use the strongest magnitude; they never multiply repeatedly.</summary>
    public sealed class CombatantBuffState
    {
        private readonly float[] strengths=new float[3];
        private readonly float[] remaining=new float[3];
        public float AttackMultiplier { get { return 1+strengths[0]; } }
        public float DamageMultiplier { get { return 1-strengths[1]; } }
        public float SpeedMultiplier { get { return 1+strengths[2]; } }
        public bool Active { get { return remaining[0]>0 || remaining[1]>0 || remaining[2]>0; } }
        public void Clear() { for(int i=0;i<remaining.Length;i++){remaining[i]=0;strengths[i]=0;} }
        public Color Tint { get { return remaining[1]>0?new Color(.3f,.85f,1f):remaining[0]>0?new Color(1f,.55f,.32f):new Color(1f,.85f,.3f); } }
        private static int Index(BuffType effect) { return effect==BuffType.AttackBoost?0:effect==BuffType.DefenseBoost?1:effect==BuffType.Haste?2:-1; }
        public void Apply(BuffType effect,float strength,float seconds)
        {
            int index=Index(effect);if(index<0)return;
            strengths[index]=Mathf.Max(strengths[index],Mathf.Clamp(strength,.05f,.5f));
            remaining[index]=Mathf.Max(remaining[index],Mathf.Clamp(seconds,1,20));
        }
        public bool Needs(BuffType effect,float strength,float duration)
        {
            int index=Index(effect);
            return index>=0 && (remaining[index]<=duration*.25f || strengths[index]<strength);
        }
        public void Tick(float delta)
        {
            for(int i=0;i<3;i++)
            {
                remaining[i]=Mathf.Max(0,remaining[i]-Mathf.Max(0,delta));
                if(remaining[i]<=0)strengths[i]=0;
            }
        }
    }
}
