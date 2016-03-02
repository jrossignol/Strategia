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
    public class StrategiaStrategy : Strategy
    {
        private bool forcedDeactivation = false;
        public static StrategiaStrategy lastActivationRequest = null;

        protected override string GetText()
        {
            return base.GetText();
        }

        protected override string GetEffectText()
        {
            string result = GenerateEffectText();
            result += GenerateCostText();
            result += GenerateObjectiveText();
            result += GenerateRequirementText();

            return result;
        }

        protected virtual string GenerateEffectText()
        {
            // Write out the effect text
            string result = "<b><#feb200>Effects:</></>\n\n";
            foreach (StrategyEffect effect in Effects)
            {
                IMultipleEffects multiEffect = effect as IMultipleEffects;
                if (multiEffect != null)
                {
                    foreach (string effectText in (multiEffect.EffectText()))
                    {
                        if (!string.IsNullOrEmpty(effectText))
                        {
                            result += "<#BEC2AE>* " + effectText + "</>\n";
                        }
                    }
                }
                else
                {
                    string effectText = effect.Description;
                    if (!string.IsNullOrEmpty(effectText))
                    {
                        result += "<#BEC2AE>* " + effectText + "</>\n";
                    }
                }
            }

            return result;
        }

        protected virtual string GenerateCostText()
        {
            string result = "";

            // Write out the cost line
            string costLine = "";
            if (InitialCostFunds != 0)
            {
                costLine += "<#B4D455>£" + InitialCostFunds.ToString("N0") + "    </>";
            }
            if (InitialCostScience != 0)
            {
                costLine += "<#6DCFF6>©" + InitialCostScience.ToString("N0") + "    </>";
            }
            if (InitialCostReputation != 0)
            {
                costLine += "<#E0D503>¡" + InitialCostReputation.ToString("N0") + "    </>";
            }
            if (!string.IsNullOrEmpty(costLine))
            {
                result += "\n<b><#EDED8B>Setup Cost:</></> " + costLine + "\n";
            }
            
            return result;
        }

        protected virtual string GenerateObjectiveText()
        {
            string result = "";

            // Write out objectives
            bool first = true;
            foreach (StrategyEffect effect in Effects)
            {
                IObjectiveEffect objectiveEffect = effect as IObjectiveEffect;
                if (objectiveEffect != null)
                {
                    if (first)
                    {
                        result += "\n<b><#feb200>Objectives:</></>\n\n";
                        first = false;
                    }

                    foreach (string objectiveText in objectiveEffect.ObjectiveText())
                    {
                        if (!string.IsNullOrEmpty(objectiveText))
                        {
                            result += "<#BEC2AE>* " + objectiveText + "</>\n";
                        }
                    }
                }
            }

            // Calculate objective rewards/penalties
            double advanceFunds = 0.0;
            float advanceScience = 0.0f;
            float advanceReputation = 0.0f;
            double rewardFunds = 0.0;
            float rewardScience = 0.0f;
            float rewardReputation = 0.0f;
            double failureFunds = 0.0;
            float failureScience = 0.0f;
            float failureReputation = 0.0f;
            foreach (StrategyEffect effect in Effects)
            {
                IObjectiveEffect objectiveEffect = effect as IObjectiveEffect;
                if (objectiveEffect != null)
                {
                    advanceFunds += objectiveEffect.advanceFunds;
                    advanceScience += objectiveEffect.advanceScience;
                    advanceReputation += objectiveEffect.advanceReputation;
                    rewardFunds += objectiveEffect.rewardFunds;
                    rewardScience += objectiveEffect.rewardScience;
                    rewardReputation += objectiveEffect.rewardReputation;
                    failureFunds += objectiveEffect.failureFunds + objectiveEffect.advanceFunds;
                    failureScience += objectiveEffect.failureScience;
                    failureReputation += objectiveEffect.failureReputation;
                }
            }

            // Write out objective advances
            first = true;
            if (advanceFunds > 0 || advanceScience > 0 || advanceReputation > 0)
            {
                if (first)
                {
                    result += "\n";
                    first = false;
                }

                result += "<b><#8BED8B>Advances: </></>";
                if (advanceFunds > 0)
                {
                    result += "<#B4D455>£" + advanceFunds.ToString("N0") + "    </>";
                }
                if (advanceScience > 0)
                {
                    result += "<#6DCFF6>©" + advanceScience.ToString("N0") + "    </>";
                }
                if (advanceReputation > 0)
                {
                    result += "<#E0D503>¡" + advanceReputation.ToString("N0") + "    </>";
                }
                result += "\n";
            }

            if (rewardFunds > 0 || rewardScience > 0 || rewardReputation > 0)
            {
                if (first)
                {
                    result += "\n";
                    first = false;
                }

                result += "<b><#8BED8B>Rewards: </></>";
                if (rewardFunds > 0)
                {
                    result += "<#B4D455>£" + rewardFunds.ToString("N0") + "    </>";
                }
                if (rewardScience > 0)
                {
                    result += "<#6DCFF6>©" + rewardScience.ToString("N0") + "    </>";
                }
                if (rewardReputation > 0)
                {
                    result += "<#E0D503>¡" + rewardReputation.ToString("N0") + "    </>";
                }
                result += "\n";
            }

            // Write out objective penalties
            if (failureFunds > 0 || failureScience > 0 || failureReputation > 0)
            {
                if (first)
                {
                    result += "\n";
                    first = false;
                }

                result += "<b><#ED0B0B>Penalties: </></>";
                if (failureFunds > 0)
                {
                    result += "<#B4D455>£-" + failureFunds.ToString("N0") + "    </>";
                }
                if (failureScience > 0)
                {
                    result += "<#6DCFF6>©-" + failureScience.ToString("N0") + "    </>";
                }
                if (failureReputation > 0)
                {
                    result += "<#E0D503>¡-" + failureReputation.ToString("N0") + "    </>";
                }
                result += "\n";
            }

            return result;
        }

        protected virtual string GenerateRequirementText()
        {
            string result = "";

            // Write out stock-based requirements
            result += "\n<b><#feb200>Requirements:</></>\n\n";
            if (InitialCostFunds > 0)
            {
                double currentFunds = Funding.Instance.Funds;
                bool fundsMet = Math.Round(currentFunds) >= Math.Round(InitialCostFunds);
                string text = "At least " + InitialCostFunds.ToString("N0") + " funds";
                if (!fundsMet)
                {
                    text += " (Current funds: " + Math.Round(currentFunds) + ")";
                }
                result += RequirementText(text, fundsMet);
            }
            if (InitialCostScience > 0)
            {
                double currentScience = ResearchAndDevelopment.Instance.Science;
                bool scienceMet = Math.Round(currentScience) >= Math.Round(InitialCostScience);
                string text = "At least " + InitialCostScience.ToString("N0") + " science";
                if (!scienceMet)
                {
                    text += " (Current science: " + Math.Round(currentScience) + ")";
                }
                result += RequirementText(text, scienceMet);
            }
            if (RequiredReputation > -1000 || InitialCostReputation > 0)
            {
                float reputationNeeded = Math.Max(RequiredReputation, InitialCostReputation > 0 ? InitialCostReputation : -1000);
                float currentReputation = Reputation.Instance.reputation;
                bool repMet = Math.Round(currentReputation) >= Math.Round(reputationNeeded);
                string text = "At least " + reputationNeeded.ToString("N0") + " reputation";
                if (!repMet)
                {
                    text += " (Current reputation: " + Math.Round(currentReputation) + ")";
                }
                result += RequirementText(text, repMet);
            }
            int minFacilityLevel = MinimumFacilityLevel();
            if (minFacilityLevel > 1)
            {
                int currentLevel = (int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.Administration) *
                    ScenarioUpgradeableFacilities.GetFacilityLevelCount(SpaceCenterFacility.Administration)) + 1;
                result += RequirementText("Administration Facility Level " + minFacilityLevel,
                    currentLevel >= minFacilityLevel);
            }

            // Requirements from strategies
            foreach (StrategyEffect effect in Effects)
            {
                IRequirementEffect requirementEffect = effect as IRequirementEffect;
                if (requirementEffect != null)
                {
                    string unmetReason;
                    bool requirementMet = requirementEffect.RequirementMet(out unmetReason);
                    string text = requirementEffect.RequirementText();
                    if (!requirementMet && !string.IsNullOrEmpty(unmetReason))
                    {
                        text += " (" + unmetReason + ")";
                    }
                    result += RequirementText(text, requirementMet);
                }
            }

            return result;
        }

        public string RequirementText(string requirement, bool met)
        {
            string color = met ? "#8BED8B" : "#FF7512";
            return "<" + color + ">* " + requirement + "</>\n";
        }

        protected override bool CanActivate(ref string reason)
        {
            lastActivationRequest = this;

            // If we are at max strategies, only allow activation if it would be an upgrade
            IEnumerable<Strategy> activeStrategies = StrategySystem.Instance.Strategies.Where(s => s.IsActive);
            int limit = GameVariables.Instance.GetActiveStrategyLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.Administration)) - 1;
            if (activeStrategies.Count() >= limit)
            {
                UpgradeableStrategy upgradeable = this as UpgradeableStrategy;
                if (upgradeable != null && activeStrategies.OfType<UpgradeableStrategy>().Any(s => s.Name == upgradeable.Name))
                {
                    return true;
                }
                else
                {
                    reason = "The Administration Building cannot support more than " + limit + " active strategies at this level.";
                    return false;
                }
            }

            // Special requirements
            foreach (StrategyEffect effect in Effects)
            {
                IRequirementEffect requirement = effect as IRequirementEffect;
                if (requirement != null)
                {
                    string unmetReason;
                    if (!requirement.RequirementMet(out unmetReason))
                    {
                        reason = requirement.RequirementText();
                        if (!string.IsNullOrEmpty(unmetReason))
                        {
                            reason += " (" + unmetReason + ")";
                        }

                        return false;
                    }
                }
            }

            return base.CanActivate(ref reason);
        }

        public void ForceDeactivate()
        {
            forcedDeactivation = true;
            Deactivate();
            forcedDeactivation = false;
        }

        protected override bool CanDeactivate(ref string reason)
        {
            if (forcedDeactivation)
            {
                return true;
            }

            foreach (StrategyEffect effect in Effects)
            {
                ICanDeactivateEffect canDeactivateEffect = effect as ICanDeactivateEffect;
                if (canDeactivateEffect != null)
                {
                    if (!canDeactivateEffect.CanDeactivate(ref reason))
                    {
                        return false;
                    }
                }
            }

            return base.CanDeactivate(ref reason);
        }

        public int MinimumFacilityLevel()
        {
            if (FactorSliderDefault > 0.70)
            {
                return 3;
            }
            if (FactorSliderDefault > 0.40)
            {
                return 2;
            }

            return 1;
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            if (!IsActive)
            {
                foreach (StrategyEffect effect in Effects)
                {
                    IOnDeactivateEffect onDeactivateEffect = effect as IOnDeactivateEffect;
                    if (onDeactivateEffect != null)
                    {
                        onDeactivateEffect.OnDeactivate();
                    }
                }
            }
        }

        /// <summary>
        /// Workaround for stock bug where strategy effects OnLoad function is not called.  Verify if fixed in KSP in 1.1.
        /// </summary>
        FieldInfo effectsField = typeof(Strategy).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(fi => fi.FieldType == typeof(List<StrategyEffect>)).First();
        protected override void OnLoad(ConfigNode node)
        {
            List<StrategyEffect> effects = (List<StrategyEffect>)effectsField.GetValue(this);

            List<ConfigNode> list = new List<ConfigNode>((IEnumerable<ConfigNode>)node.GetNodes("EFFECT"));
            for (int index1 = 0; index1 < effects.Count; ++index1)
            {
                ConfigNode node1 = (ConfigNode)null;
                for (int index2 = list.Count - 1; index2 >= 0; --index2)
                {
                    if (list[index2].GetValue("name") == effects[index1].GetType().Name)
                    {
                        node1 = list[index2];
                        list.RemoveAt(index2);
                        break;
                    }
                }
                if (node1 != null)
                {
                    effects[index1].Load(node1);
                }
            }
        }
    }
}
