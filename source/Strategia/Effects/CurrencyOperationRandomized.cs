using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Strategies;
using Strategies.Effects;
using ContractConfigurator.Util;

namespace Strategia
{
    /// <summary>
    /// Special CurrencyOperation that gives a random modifier.
    /// </summary>
    public class CurrencyOperationRandomized : StrategyEffect
    {
        static System.Random random = new System.Random();

        List<Currency> currencies;
        string effectDescription;
        List<TransactionReasons> affectReasons;
        List<float> lowerValues;
        List<float> upperValues;

        LRUCache<string, float> valueCache = new LRUCache<string, float>(100);

        public CurrencyOperationRandomized(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            float lowerValue = Parent.GetLeveledListItem(lowerValues);
            float upperValue = Parent.GetLeveledListItem(upperValues);

            string lowerPct = ToPercentage(lowerValue);
            string upperPct = ToPercentage(upperValue);

            string currencyStr = currencies.Count() > 1 ? "" : (currencies.First() + " ");

            return "Randomly modifies " + currencyStr + effectDescription + " by between " + lowerPct + " and " + upperPct + ".";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            Debug.Log("CurrencyOperationRandomized.OnLoadFromConfig");
            base.OnLoadFromConfig(node);

            currencies = ConfigNodeUtil.ParseValue<List<Currency>>(node, "currency");
            effectDescription = ConfigNodeUtil.ParseValue<string>(node, "effectDescription");
            affectReasons = ConfigNodeUtil.ParseValue<List<TransactionReasons>>(node, "AffectReason");
            lowerValues = ConfigNodeUtil.ParseValue<List<float>>(node, "lowerValue");
            upperValues = ConfigNodeUtil.ParseValue<List<float>>(node, "upperValue");
        }

        protected override void OnSave(ConfigNode node)
        {
            Debug.Log("CurrencyOperationRandomized.OnSave");
            base.OnSave(node);

            ConfigNode values = new ConfigNode("VALUES");
            node.AddNode(values);
            valueCache.Save(values);
        }

        protected override void OnLoad(ConfigNode node)
        {
            Debug.Log("CurrencyOperationRandomized.OnLoad");
            base.OnLoad(node);

            valueCache.Load(node.GetNode("VALUES"));
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                GameEvents.Modifiers.OnCurrencyModifierQuery.Add(new EventData<CurrencyModifierQuery>.OnEvent(OnEffectQuery));
                GameEvents.Contract.onDeclined.Add(new EventData<Contract>.OnEvent(OnContractChange));
                GameEvents.Contract.onOffered.Add(new EventData<Contract>.OnEvent(OnContractChange));
            }
        }

        protected override void OnUnregister()
        {
            GameEvents.Modifiers.OnCurrencyModifierQuery.Remove(new EventData<CurrencyModifierQuery>.OnEvent(OnEffectQuery));
            GameEvents.Contract.onDeclined.Remove(new EventData<Contract>.OnEvent(OnContractChange));
            GameEvents.Contract.onOffered.Remove(new EventData<Contract>.OnEvent(OnContractChange));
        }

        private void OnContractChange(Contract ignored)
        {
            // Build the mission control text for active contracts, this will force them back up to the top of the LRU cache
            foreach (Contract c in ContractSystem.Instance.Contracts.Where(c => c.ContractState == Contract.State.Active))
            {
                c.MissionControlTextRich();
            }
        }

        private void OnEffectQuery(CurrencyModifierQuery qry)
        {
            // Check the reason is a match
            if (!affectReasons.Contains(qry.reason))
            {
                return;
            }

            // Check if it's non-zero
            float total = 0.0f;
            foreach (Currency currency in currencies)
            {
                total += Math.Abs(qry.GetInput(currency));
            }
            if (total < 0.01f)
            {
                return;
            }

            string hash = string.Join("|", new string[]{
                qry.GetInput(Currency.Funds).ToString("F0"),
                qry.GetInput(Currency.Science).ToString("F0"),
                qry.GetInput(Currency.Reputation).ToString("F0"),
                qry.reason.ToString()
            });

            // Get the multiplier
            float multiplier = 0.0f;
            if (!valueCache.ContainsKey(hash))
            {
                float lowerValue = Parent.GetLeveledListItem(lowerValues);
                float upperValue = Parent.GetLeveledListItem(upperValues);

                multiplier = (float)(random.NextDouble() * (upperValue - lowerValue) + lowerValue);
                valueCache[hash] = multiplier;
            }
            multiplier = valueCache[hash];

            foreach (Currency currency in currencies)
            {
                qry.AddDelta(currency, multiplier * qry.GetInput(currency) - qry.GetInput(currency));
            }
        }
    }
}
