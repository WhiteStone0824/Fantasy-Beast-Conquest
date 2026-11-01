using UnityEngine;
namespace FusionAnyMonster.Spells
{
    [CreateAssetMenu(menuName="Fusion Any Monster/Spell Limits")]
    public sealed class SpellSettings:ScriptableObject
    {
        [Range(2,128)]public int maxNodes=24;
        [Range(2,12)]public int maxDepth=6;
        [Range(1,256)]public int maxProjectiles=48;
        [Range(0,8)]public int maxGeneration=3;
        [Range(8,1024)]public int maxEvents=128;
        [Range(.1f,20)]public float maxLifetime=6;
        [Range(0,1)]public float crossoverChance=.7f,parameterMutationChance=.15f,nodeMutationChance=.06f,structureMutationChance=.08f;
        [Tooltip("対応する同種ノードの内部を交叉する確率。数値は混合し、種類・属性などは双方から選びます。")]
        [Range(0,1)]public float nodeParameterCrossoverChance=.7f;
        [Tooltip("配合時に魔法1枠が子の種族の標準魔法に変異する確率")]
        [Range(0,1)]public float speciesDefaultMutationChance=.01f;
        private static SpellSettings current;
        public static SpellSettings Current {get{if(current==null){current=Resources.Load<SpellSettings>("SpellSettings");if(current==null)current=CreateInstance<SpellSettings>();}return current;}}
    }
}
