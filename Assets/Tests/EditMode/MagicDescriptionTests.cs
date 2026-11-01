using FusionAnyMonster.Spells;
using FusionAnyMonster.UI;
using NUnit.Framework;
namespace FusionAnyMonster.Tests
{
    public sealed class MagicDescriptionTests
    {
        [Test]public void DescriptionsFollowExpressedStatusAndParameters()
        {
            foreach(DebuffType status in System.Enum.GetValues(typeof(DebuffType)))
            {
                var g=SpellProgramBuilder.Projectile(effect:SpellNodeKind.ApplyStatus);
                foreach(var p in new[]{g.chromosomeA,g.chromosomeB}){p.nodes[2].parameters.status=status;p.nodes[2].parameters.probability=.6f;p.nodes[2].parameters.seconds=4;}
                var magic=MagicFactory.Build(SpellSlotStorage.Prepare(new MagicGenome{programGenes=g},0));
                StringAssert.Contains("60%",MagicDescription.EffectDetails(magic));
                StringAssert.DoesNotContain("遺伝子",MagicDescription.EffectDetails(magic));
            }
        }
        [Test]public void SupportDescriptionsFollowTargetAndBuffNodes()
        {
            var g=SpellProgramBuilder.Projectile(effect:SpellNodeKind.ApplyBuff,target:SpellTarget.LowestHealthAlly);
            foreach(var p in new[]{g.chromosomeA,g.chromosomeB}){p.nodes[2].parameters.multiplier=.25f;p.nodes[2].parameters.seconds=6;}
            var magic=MagicFactory.Build(SpellSlotStorage.Prepare(new MagicGenome{programGenes=g},0));
            StringAssert.Contains("味方",MagicDescription.Behavior(magic));StringAssert.Contains("25%",MagicDescription.EffectDetails(magic));
            StringAssert.Contains("6秒",MagicDescription.EffectDetails(magic));
        }
    }
}
