using UnityEngine;

namespace FusionAnyMonster
{
    [CreateAssetMenu(menuName="Fusion Any Monster/Personality Database")]
    public sealed class PersonalityDatabase : ScriptableObject
    {
        public PersonalityDefinition[] definitions = new PersonalityDefinition[0];
        public PersonalityDefinition Get(PersonalityType type)
        {
            if (definitions == null) return null;
            for (int i = 0; i < definitions.Length; i++)
                if (definitions[i] != null && definitions[i].type == type) return definitions[i];
            return null;
        }
    }
}
