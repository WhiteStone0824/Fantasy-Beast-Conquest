using UnityEngine;

namespace FusionAnyMonster
{
    [CreateAssetMenu(menuName="Fusion Any Monster/Rank Magic Slot Database")]
    public sealed class RankMagicSlotDatabase : ScriptableObject
    {
        [Min(0)] public int[] magicSlots = { 1, 1, 2, 2, 3, 4, 5 };
        public int GetSlots(SpeciesRank rank)
        {
            int index = Mathf.Clamp((int)rank, 0, 6);
            return magicSlots == null || magicSlots.Length <= index ? new[] { 1, 1, 2, 2, 3, 4, 5 }[index] : Mathf.Max(0, magicSlots[index]);
        }
    }
}
