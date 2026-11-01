using System;

namespace FusionAnyMonster
{
    /// <summary>Each stat independently samples N(parent mean, standard deviation squared).</summary>
    public static class IndividualValueInheritance
    {
        public const float DefaultStandardDeviation = 10f;

        public static IndividualValues Inherit(IndividualValues parentA, IndividualValues parentB,
            Random random, float standardDeviation = DefaultStandardDeviation)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (float.IsNaN(standardDeviation) || float.IsInfinity(standardDeviation) ||
                standardDeviation < 0 || standardDeviation > IndividualValues.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(standardDeviation));

            var child = new IndividualValues();
            for (int stat = 0; stat < IndividualValues.StatCount; stat++)
            {
                double mean = ((parentA == null ? 0 : parentA.Get(stat)) +
                    (parentB == null ? 0 : parentB.Get(stat))) * .5;
                double value = mean;
                if (standardDeviation > 0)
                {
                    // Box-Muller: 1 - NextDouble() is strictly positive, so Log never receives zero.
                    double radius = Math.Sqrt(-2 * Math.Log(1 - random.NextDouble()));
                    double angle = 2 * Math.PI * random.NextDouble();
                    value += standardDeviation * radius * Math.Cos(angle);
                }
                value = Math.Max(IndividualValues.MinValue, Math.Min(IndividualValues.MaxValue, value));
                child.Set(stat, (int)Math.Round(value, MidpointRounding.AwayFromZero));
            }
            return child;
        }
    }
}
