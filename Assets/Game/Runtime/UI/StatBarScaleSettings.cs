using UnityEngine;

namespace FusionAnyMonster.UI
{
    [CreateAssetMenu(menuName="Fusion Any Monster/UI/Stat Bar Scale Settings")]
    public sealed class StatBarScaleSettings : ScriptableObject
    {
        // Shared display axes, independent of species, rank, current selection and roster contents.
        private static readonly float[] Defaults={1050,400,200,250,200,200,120,100,120,100};
        [Tooltip("全キャラ共通の棒グラフ表示上限。能力値そのものの上限ではありません。")]
        public UnitStats maximumValues=new UnitStats {
            hp=1050,mp=400,physicalAttack=200,magicAttack=250,physicalDefense=200,magicDefense=200,
            moveSpeed=120,attackSpeed=100,searchRange=120,mpRegen=100
        };
        public float GetMaximum(int statIndex)
        {
            int index=Mathf.Clamp(statIndex,0,Defaults.Length-1);
            float maximum=maximumValues==null?Defaults[index]:maximumValues.Get(index);
            return float.IsNaN(maximum) || float.IsInfinity(maximum) || maximum<=0?Defaults[index]:maximum;
        }
        public static float Normalize(int statIndex,float value,StatBarScaleSettings settings=null)
        {
            float maximum=settings==null?Defaults[Mathf.Clamp(statIndex,0,Defaults.Length-1)]:settings.GetMaximum(statIndex);
            return float.IsNaN(value)?0:Mathf.Clamp01(value/maximum);
        }
    }
}
