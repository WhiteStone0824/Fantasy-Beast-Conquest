using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class SpeciesSpriteReferenceTests
    {
        [Test]
        public void EverySpeciesReferencesItsMainSpriteRatherThanASmallSlicedFragment()
        {
            SpeciesDatabase database=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            Assert.IsNotNull(database);Assert.AreEqual(420,database.species.Length);
            foreach(SpeciesDefinition species in database.species)
            {
                Assert.IsNotNull(species.sprite,species.displayName);
                Sprite largest=species.sprite;
                foreach(Object asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(species.sprite)))
                {
                    Sprite candidate=asset as Sprite;
                    if(candidate!=null && candidate.rect.width*candidate.rect.height>largest.rect.width*largest.rect.height)largest=candidate;
                }
                Assert.AreSame(largest,species.sprite,species.displayName+" must use the character body instead of a detached fragment.");
                Assert.GreaterOrEqual(species.sprite.rect.width*species.sprite.rect.height,4096,species.displayName+" must have a visible portrait region.");
            }
        }
    }
}
