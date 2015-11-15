using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;

namespace Strategia
{
    public class StrategiaStrategy : Strategy
    {
        protected List<StrategyEffect> hiddenEffects = new List<StrategyEffect>();
        IEnumerable<StrategyEffect> AllEffects
        {
            get
            {
                return Effects.Union(hiddenEffects);
            }
        }

        protected override string GetText()
        {
            Debug.Log("StrategiaStrategy.GetText");
            return base.GetText();
        }

        protected override string GetEffectText()
        {
            Debug.Log("StrategiaStrategy.GetEffectText");

            RemoveHiddenEffects();

            string result = base.GetEffectText();

            foreach (StrategyEffect effect in AllEffects)
            {
                IExtraTextEffect extraTextEffect = effect as IExtraTextEffect;
                if (extraTextEffect != null)
                {
                    string extraText = extraTextEffect.ExtraText();
                    if (!string.IsNullOrEmpty(extraText))
                    {
                        result += "\n" + extraText;
                    }
                }
            }

            return result;
        }

        private void RemoveHiddenEffects()
        {
            bool modified = false;
            foreach (StrategyEffect effect in Effects)
            {
                if (effect is IHiddenEffect)
                {
                    hiddenEffects.Add(effect);
                    modified = true;
                }
            }

            if (modified)
            {
                Effects.RemoveAll(se => hiddenEffects.Contains(se));
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            Debug.Log("StrategiaStrategy.OnSave");

            foreach (StrategyEffect hiddenEffect in hiddenEffects)
            {
                ConfigNode child = new ConfigNode("EFFECT");
                node.AddNode(child);
                hiddenEffect.Save(child);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            Debug.Log("StrategiaStrategy.OnLoad");
        }

        protected override bool CanActivate(ref string reason)
        {
            Debug.Log("StrategiaStrategy.CanActivate");

            foreach (StrategyEffect effect in AllEffects)
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

            foreach (StrategyEffect effect in AllEffects)
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

            foreach (StrategyEffect effect in hiddenEffects)
            {
                effect.Register();
            }
        }
    }
}
