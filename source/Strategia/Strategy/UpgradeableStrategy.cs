using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Strategies;

namespace Strategia
{
    public class UpgradeableStrategy : StrategiaStrategy
    {
        string _name;
        int _level;
        public string Name
        {
            get
            {
                if (!isInitialized)
                {
                    Initialize();
                }
                return _name;
            }
        }
        public int Level
        {
            get
            {
                if (!isInitialized)
                {
                    Initialize();
                }
                return _level;
            }
        }

        private bool isInitialized = false;
        private bool strategiesNeedRedraw  = false;
        private UpgradeableStrategy conflictStrategy;

        /// <summary>
        /// Initialization function.  Would do this in the constructor, but the base class's constructor
        /// doesn't set anything, this making inheritance rather difficult.
        /// </summary>
        private void Initialize()
        {
            // Reverse engineer the level as we can't pass that type of information through the classes we are given
            _name = Config.Name;
            char c = _name.Last();
            _level = c - '0';
            _name = _name.TrimEnd(new char[] { c });

            isInitialized = true;
        }

        protected override bool CanActivate(ref string reason)
        {
            // First do the basic checks
            if (!base.CanActivate(ref reason))
            {
                return false;
            }

            // If we are at max strategies, only allow activation if it would be an upgrade
            IEnumerable<Strategy> activeStrategies = StrategySystem.Instance.Strategies.Where(s => s.IsActive);
            int limit = GameVariables.Instance.GetActiveStrategyLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.Administration)) - 1;
            if (activeStrategies.Count() >= limit)
            {
                if (activeStrategies.OfType<UpgradeableStrategy>().Any(s => s.Name == Name))
                {
                    return true;
                }
                else
                {
                    reason = "The Administration Building cannot support more than " + limit + " active strategies at this level.";
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();

            // Check for upgrades
            IEnumerable<Strategy> activeStrategies = StrategySystem.Instance.Strategies.Where(s => s.IsActive);
            UpgradeableStrategy conflictStrategy = activeStrategies.OfType<UpgradeableStrategy>().Where(s => s.Name == Name && s.Level != Level).FirstOrDefault();
            if (conflictStrategy != null)
            {
                // Remove the other strategy
                conflictStrategy.ForceDeactivate();


                // Force a redraw, but not until the next update
                strategiesNeedRedraw = true;
            }

            // Register callbacks
            GameEvents.Modifiers.OnCurrencyModifierQuery.Add(new EventData<CurrencyModifierQuery>.OnEvent(OnEffectQuery));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            // Unregister callbacks
            GameEvents.Modifiers.OnCurrencyModifierQuery.Remove(new EventData<CurrencyModifierQuery>.OnEvent(OnEffectQuery));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (strategiesNeedRedraw)
            {
                Administration.Instance.RedrawPanels();
                strategiesNeedRedraw = false;
            }
        }

        protected override string GenerateEffectText()
        {
            string result = "";

            IEnumerable<Strategy> activeStrategies = StrategySystem.Instance.Strategies.Where(s => s.IsActive);
            UpgradeableStrategy conflictStrategy = activeStrategies.OfType<UpgradeableStrategy>().Where(s => s.Name == Name && s.Level != Level).FirstOrDefault();
            if (conflictStrategy != null)
            {
                result = "<i><#8BED8B>Can " + (conflictStrategy.Level > Level ? "downgrade" : "upgrade") + " from " + conflictStrategy.Title + " to " + Title + ".</></>\n\n";
            }

            return result + base.GenerateEffectText();
        }

        protected override string GenerateCostText()
        {
            string result = "";

            IEnumerable<Strategy> activeStrategies = StrategySystem.Instance.Strategies.Where(s => s.IsActive);
            conflictStrategy = activeStrategies.OfType<UpgradeableStrategy>().Where(s => s.Name == Name && s.Level != Level).FirstOrDefault();
            float fundsDiscount = 0.0f;
            float scienceDiscount = 0.0f;
            float reputationDiscount = 0.0f;
            if (conflictStrategy != null)
            {
                fundsDiscount = Math.Min(InitialCostFunds, conflictStrategy.InitialCostFunds);
                scienceDiscount = Math.Min(InitialCostScience, conflictStrategy.InitialCostScience);
                reputationDiscount = Math.Min(InitialCostReputation, conflictStrategy.InitialCostReputation);
            }

            // Write out the cost line
            string costLine = "";
            if (InitialCostFunds != 0)
            {
                costLine += "<#B4D455>£" + (InitialCostFunds - fundsDiscount).ToString("N0") + "</>";
                if (fundsDiscount > 0.0f)
                {
                    costLine += "<#B4D455> (-" + fundsDiscount.ToString("N0") + ")</>";
                }
                costLine += "    ";
            }
            if (InitialCostScience != 0)
            {
                costLine += "<#6DCFF6>©" + (InitialCostScience - scienceDiscount).ToString("N0") + "</>";
                if (scienceDiscount > 0.0f)
                {
                    costLine += "<#B4D455> (-" + scienceDiscount.ToString("N0") + ")</>";
                }
                costLine += "    ";
            }
            if (InitialCostReputation != 0)
            {
                costLine += "<#E0D503>¡" + (InitialCostReputation - reputationDiscount).ToString("N0") + "</>";
                if (reputationDiscount > 0.0f)
                {
                    costLine += "<#B4D455> (-" + reputationDiscount.ToString("N0") + ")</>";
                }
            }
            if (!string.IsNullOrEmpty(costLine))
            {
                result += "\n<b><#EDED8B>Setup Cost:</></> " + costLine + "\n";
            }

            return result;
        }

        private void OnEffectQuery(CurrencyModifierQuery qry)
        {
            // Check the reason is a match
            if (qry.reason != TransactionReasons.StrategySetup)
            {
                return;
            }

            if (lastActivationRequest != this || conflictStrategy == null)
            {
                return;
            }

            if (Math.Abs(qry.GetInput(Currency.Funds)) >= 0.01)
            {
                float fundsDiscount = Math.Min(InitialCostFunds, conflictStrategy.InitialCostFunds);
                qry.AddDelta(Currency.Funds, fundsDiscount);
            }

            if (Math.Abs(qry.GetInput(Currency.Science)) >= 0.01)
            {
                float scienceDiscount = Math.Min(InitialCostScience, conflictStrategy.InitialCostScience);
                qry.AddDelta(Currency.Science, scienceDiscount);
            }

            if (Math.Abs(qry.GetInput(Currency.Reputation)) >= 0.01)
            {
                float reputationDiscount = Math.Min(InitialCostReputation, conflictStrategy.InitialCostReputation);
                qry.AddDelta(Currency.Reputation, reputationDiscount);
            }
        }
    }
}
