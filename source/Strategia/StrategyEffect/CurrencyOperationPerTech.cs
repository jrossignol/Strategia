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
    /// Special CurrencyOperation that multiplies by the number of unresearched technologies.
    /// </summary>
    public class CurrencyOperationPerTech : StrategyEffect
    {
        private static List<string> allTech = null;

        Currency currency;
        string effectDescription;
        List<TransactionReasons> affectReasons;

        List<float> multipliers;

        public CurrencyOperationPerTech(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            float multiplier = Parent.GetLeveledListItem(multipliers);
            float currentValue = CurrentMultiplier();

            return (multiplier > 0.0 ? "+" :"") + multiplier.ToString("F1") + " " + currency + " " + effectDescription +
                " per unresearched technology (currently adds " + (multiplier > 0.0 ? "+" :"") + currentValue.ToString("F1") + " " + currency + ").";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            currency = ConfigNodeUtil.ParseValue<Currency>(node, "currency");
            effectDescription = ConfigNodeUtil.ParseValue<string>(node, "effectDescription");
            affectReasons = ConfigNodeUtil.ParseValue<List<TransactionReasons>>(node, "affectReason");
            multipliers = ConfigNodeUtil.ParseValue<List<float>>(node, "multiplier");
        }

        private static bool SetupTech()
        {
            if (HighLogic.CurrentGame == null)
            {
                return false;
            }

            // Cache the tech tree
            if (allTech == null)
            {
                ConfigNode techTreeRoot = ConfigNode.Load(HighLogic.CurrentGame.Parameters.Career.TechTreeUrl);
                ConfigNode techTree = null;
                if (techTreeRoot != null)
                {
                    techTree = techTreeRoot.GetNode("TechTree");
                }

                if (techTreeRoot == null || techTree == null)
                {
                    Debug.LogError("Strategia: Couldn't load tech tree from " + HighLogic.CurrentGame.Parameters.Career.TechTreeUrl);
                    return false;
                }

                // Get a listing of all tech with parts
                IEnumerable<AvailablePart> parts = PartLoader.Instance.parts;
                allTech = new List<string>();
                foreach (ConfigNode techNode in techTree.GetNodes("RDNode"))
                {
                    string techId = techNode.GetValue("id");
                    if (parts.Any(p => p.TechRequired == techId))
                    {
                        allTech.Add(techId);
                    }
                }
            }

            return true;
        }

        protected override void OnRegister()
        {
            GameEvents.Modifiers.OnCurrencyModifierQuery.Add(new EventData<CurrencyModifierQuery>.OnEvent(OnEffectQuery));
        }

        protected override void OnUnregister()
        {
            GameEvents.Modifiers.OnCurrencyModifierQuery.Remove(new EventData<CurrencyModifierQuery>.OnEvent(OnEffectQuery));
        }

        protected float CurrentMultiplier()
        {
            SetupTech();

            int count = 0;
            foreach (string techId in allTech)
            {
                ProtoTechNode techNode = ResearchAndDevelopment.Instance.GetTechState(techId);
                if (techNode == null || techNode.state != RDTech.State.Available)
                {
                    count++;
                }
            }

            return Parent.GetLeveledListItem(multipliers) * count;
        }

        private void OnEffectQuery(CurrencyModifierQuery qry)
        {
            // Check the reason is a match
            if (!affectReasons.Contains(qry.reason))
            {
                return;
            }

            // Check if it's non-zero
            float value = qry.GetInput(currency);
            if (Math.Abs(value) < 0.01)
            {
                return;
            }

            // Calculate the delta
            qry.AddDelta(currency, CurrentMultiplier());
        }
    }
}
