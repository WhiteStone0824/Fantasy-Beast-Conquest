using System;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class IndividualValueInheritanceTests
    {
        private static IndividualValues Values(int value)
        {
            var values = new IndividualValues();
            for (int stat = 0; stat < IndividualValues.StatCount; stat++) values.Set(stat, value);
            return values;
        }

        [Test] public void ZeroDeviationUsesEachParentsMeanAndRoundsHalfUp()
        {
            var a = Values(20); var b = Values(80); b.Set(1, 81);
            var child = IndividualValueInheritance.Inherit(a, b, new System.Random(1), 0);
            for (int stat = 0; stat < 10; stat++) Assert.AreEqual(stat == 1 ? 51 : 50, child.Get(stat));
        }

        [Test] public void SamplesFollowTheConfiguredNormalDistribution()
        {
            var random = new System.Random(8128); var a = Values(20); var b = Values(80);
            const int samples = 20000;
            double sum = 0, squares = 0; int withinTen = 0, withinTwenty = 0;
            for (int sample = 0; sample < samples; sample++)
            {
                int value = IndividualValueInheritance.Inherit(a, b, random, 10).hp;
                sum += value; squares += value * value;
                if (Math.Abs(value - 50) <= 10) withinTen++;
                if (Math.Abs(value - 50) <= 20) withinTwenty++;
            }
            double mean = sum / samples;
            double deviation = Math.Sqrt(squares / samples - mean * mean);
            Assert.That(mean, Is.InRange(49.7, 50.3));
            Assert.That(deviation, Is.InRange(9.7, 10.3));
            // Integer rounding widens these intervals by half a point on either side.
            Assert.That(withinTen / (double)samples, Is.InRange(.69, .72));
            Assert.That(withinTwenty / (double)samples, Is.InRange(.95, .97));
        }

        [Test] public void SeedAndParentOrderReproduceResultsWithoutMutatingParents()
        {
            var a = Values(15); var b = Values(85);
            string beforeA = JsonUtility.ToJson(a), beforeB = JsonUtility.ToJson(b);
            var first = IndividualValueInheritance.Inherit(a, b, new System.Random(42));
            var second = IndividualValueInheritance.Inherit(b, a, new System.Random(42));
            Assert.AreEqual(JsonUtility.ToJson(first), JsonUtility.ToJson(second));
            Assert.AreEqual(beforeA, JsonUtility.ToJson(a)); Assert.AreEqual(beforeB, JsonUtility.ToJson(b));
            Assert.AreNotEqual(JsonUtility.ToJson(first), JsonUtility.ToJson(IndividualValueInheritance.Inherit(a, b, new System.Random(43))));
            Assert.Greater(new System.Collections.Generic.HashSet<int> {first.hp,first.mp,first.physicalAttack,first.magicAttack,first.moveSpeed}.Count, 1);
        }

        [TestCase(0)][TestCase(100)]
        public void EvenExtremeSamplesStayWithinZeroToOneHundred(int parentValue)
        {
            var random = new System.Random(100); var a = Values(parentValue); bool sawZero = false, sawHundred = false;
            for (int sample = 0; sample < 1000; sample++)
            {
                var child = IndividualValueInheritance.Inherit(a, a, random, 100);
                for (int stat = 0; stat < 10; stat++)
                {int value = child.Get(stat); Assert.That(value, Is.InRange(0,100)); sawZero |= value == 0; sawHundred |= value == 100;}
            }
            Assert.IsTrue(sawZero && sawHundred);
        }

        [TestCase(-1f)][TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(101f)]
        public void InvalidDeviationIsRejected(float deviation)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => IndividualValueInheritance.Inherit(Values(50), Values(50), new System.Random(1), deviation));
        }
    }
}
