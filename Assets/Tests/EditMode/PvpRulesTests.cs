using FusionAnyMonster.Battle;
using NUnit.Framework;

namespace FusionAnyMonster.Tests
{
    public sealed class PvpRulesTests
    {
        [TestCase(true,true,true)]
        [TestCase(true,false,false)]
        [TestCase(false,true,false)]
        [TestCase(false,false,true)]
        public void LocalSideDependsOnWhetherThisPeerIsHost(bool host,bool friendly,bool expected)
        {
            var method=typeof(BattleDemo).GetMethod("IsLocalPvpSide",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(method);Assert.AreEqual(expected,method.Invoke(null,new object[]{host,friendly}));
        }
        [Test] public void SurvivingUnitsTakePriorityOverDamage()
        {
            Assert.AreEqual(300f,PvpRules.TimeLimitSeconds);
            Assert.AreEqual("VICTORY",PvpRules.Resolve(1,0,1,1000));
            Assert.AreEqual("DEFEAT",PvpRules.Resolve(2,3,10000,1));
        }
        [Test] public void EqualSurvivorsUseActualDamageAndExactTiesDraw()
        {
            Assert.AreEqual("VICTORY",PvpRules.Resolve(2,2,100,99));
            Assert.AreEqual("DEFEAT",PvpRules.Resolve(0,0,99,100));
            Assert.AreEqual("DRAW",PvpRules.Resolve(1,1,100,100));
        }
    }
}
