using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;
using Strategies;

namespace Strategia
{
    public static class StrategyExtensions
    {
        public static int Level(this Strategy strategy)
        {
            return strategy.HasFactorSlider ? (int)Math.Round(strategy.Factor * strategy.FactorSliderSteps) : 1;
        }

        public static T GetLeveledListItem<T>(this Strategy strategy, IEnumerable<T> list, int offset = 0)
        {
            int index = offset + strategy.Level() - 1;

            return index >= list.Count() ? list.Last() : index < 0 ? list.First() : list.ElementAt(index);
        }
    }
}
