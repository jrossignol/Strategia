using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Strategies.Effects;

namespace Strategia
{
    /// <summary>
    /// Special CurrencyOperation that only acts on non-zero values.
    /// </summary>
    public class CurrencyOperationTextOverride : CurrencyOperation
    {
        string description;

        public CurrencyOperationTextOverride(Strategy parent)
            : base(parent)
        {
        }

        public CurrencyOperationTextOverride(Strategy parent, float minValue, float maxValue, Currency currency, CurrencyOperation.Operator op, TransactionReasons AffectReasons, string description)
            : base(parent, minValue, maxValue, currency, op, AffectReasons, description)
        {
        }

        protected override string GetDescription()
        {
            return description;
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);

            description = ConfigNodeUtil.ParseValue<string>(node, "description");
        }
    }
}
