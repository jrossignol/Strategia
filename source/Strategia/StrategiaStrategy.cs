using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Strategies;

namespace Strategia
{
    public class StrategiaStrategy : Strategy
    {
        protected override string GetText()
        {
            Debug.Log("StrategiaStrategy.GetText");
            return base.GetText();
        }

        protected override string GetEffectText()
        {
            Debug.Log("StrategiaStrategy.GetEffectText");

            // Write out the effect text
            string result = "<b><#feb200>Effects:</></>\n\n";
            foreach (StrategyEffect effect in Effects)
            {
                string effectText = effect.Description;
                if (!string.IsNullOrEmpty(effectText))
                {
                    result += "<#BEC2AE>* " + effectText + "</>\n";
                }
            }

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

                    string objectiveText = objectiveEffect.ObjectiveText();
                    if (!string.IsNullOrEmpty(objectiveText))
                    {
                        result += "<#BEC2AE>* " + objectiveText + "</>\n";
                    }
                }
            }

            // Calculate objective rewards/penalties
            double fundsAward = 0.0;
            float scienceAward = 0.0f;
            float reputationAward = 0.0f;
            double fundsPenalty = 0.0;
            float sciencePenalty = 0.0f;
            float reputationPenalty = 0.0f;
            foreach (StrategyEffect effect in Effects)
            {
                IObjectiveEffect objectiveEffect = effect as IObjectiveEffect;
                if (objectiveEffect != null)
                {
                    fundsAward += objectiveEffect.fundsAward;
                    scienceAward += objectiveEffect.scienceAward;
                    reputationAward += objectiveEffect.reputationAward;
                    fundsPenalty += objectiveEffect.fundsPenalty;
                    sciencePenalty += objectiveEffect.sciencePenalty;
                    reputationPenalty += objectiveEffect.reputationPenalty;
                }
            }

            // Write out objective rewards
            first = true;
            if (fundsAward > 0 || scienceAward > 0 || reputationAward > 0)
            {
                if (first)
                {
                    result += "\n";
                    first = false;
                }

                result += "<b><#8BED8B>Rewards: </></>";
                if (fundsAward > 0)
                {
                    result += "<#B4D455>£" + fundsAward.ToString("N0") + "    </>";
                }
                if (scienceAward > 0)
                {
                    result += "<#6DCFF6>©" + scienceAward.ToString("N0") + "    </>";
                }
                if (reputationAward > 0)
                {
                    result += "<#E0D503>¡" + reputationAward.ToString("N0") + "    </>";
                }
                result += "\n";
            }

            // Write out objective penalties
            if (fundsPenalty > 0 || sciencePenalty > 0 || reputationPenalty > 0)
            {
                if (first)
                {
                    result += "\n";
                    first = false;
                }

                result += "<b><#ED0B0B>Penalties: </></>";
                if (fundsPenalty > 0)
                {
                    result += "<#B4D455>£-" + fundsPenalty.ToString("N0") + "    </>";
                }
                if (sciencePenalty > 0)
                {
                    result += "<#6DCFF6>©-" + sciencePenalty.ToString("N0") + "    </>";
                }
                if (reputationPenalty > 0)
                {
                    result += "<#E0D503>¡-" + reputationPenalty.ToString("N0") + "    </>";
                }
                result += "\n";
            }

            // Write out stock-based requirements
            result += "\n<b><#feb200>Requirements:</></>\n\n";
            int minFacilityLevel = MinimumFacilityLevel();
            if (minFacilityLevel > 1)
            {
                int currentLevel = ScenarioUpgradeableFacilities.GetFacilityLevelCount(SpaceCenterFacility.Administration) + 1;
                result += RequirementText("Administration Facility Level " + minFacilityLevel,
                    currentLevel >= minFacilityLevel);
            }
            if (RequiredReputation > -1000)
            {
                float currentReputation = Reputation.Instance.reputation;
                result += RequirementText("At least " + RequiredReputation + " reputation",
                    currentReputation >= RequiredReputation);
            }

            // Requirements from strategies
            foreach (StrategyEffect effect in Effects)
            {
                IRequirementEffect requirementEffect = effect as IRequirementEffect;
                if (requirementEffect != null)
                {
                    result += RequirementText(requirementEffect.RequirementText(), requirementEffect.RequirementMet());
                }
            }

            return result;
        }

        public string RequirementText(string requirement, bool met)
        {
            string color = met ? "#8BED8B" : "#FF7512";
            return "<" + color + ">* " + requirement + "</>\n";
        }

        protected override void OnSave(ConfigNode node)
        {
            Debug.Log("StrategiaStrategy.OnSave");
        }

        protected override void OnLoad(ConfigNode node)
        {
            Debug.Log("StrategiaStrategy.OnLoad");
        }

        protected override bool CanActivate(ref string reason)
        {
            Debug.Log("StrategiaStrategy.CanActivate");

            foreach (StrategyEffect effect in Effects)
            {
                IRequirementEffect requirement = effect as IRequirementEffect;
                if (requirement != null)
                {
                    if (!requirement.RequirementMet())
                    {
                        reason = requirement.Reason;
                        return false;
                    }
                }
            }

            return base.CanActivate(ref reason);
        }

        protected override bool CanDeactivate(ref string reason)
        {
            Debug.Log("StrategiaStrategy.CanDeactivate");

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

        protected override void OnRegister()
        {
            Debug.Log("StrategiaStrategy.OnRegister");
        }

        protected int MinimumFacilityLevel()
        {
            if (!HasFactorSlider)
            {
                if (FactorSliderDefault > 0.70)
                {
                    return 3;
                }
                if (FactorSliderDefault > 0.40)
                {
                    return 2;
                }
            }

            return 1;
        }
    }
}
