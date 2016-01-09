using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Strategies.Effects;
using ContractConfigurator;
using ContractConfigurator.Util;

namespace Strategia
{
    /// <summary>
    /// Strategy effect for boosting science for certain conditions. 
    /// </summary>
    public class ScienceBooster : StrategyEffect, IMultipleEffects
    {
        float KSCScienceMultiplier;
        float nonKSCScienceMultiplier;

        public ScienceBooster(Strategy parent)
            : base(parent)
        {
        }

        public IEnumerable<string> EffectText()
        {
            if (KSCScienceMultiplier > 0.0)
            {
                yield return ToPercentage(KSCScienceMultiplier, "N0") + " bonus to KSC Science.";
            }
            if (nonKSCScienceMultiplier > 0.0)
            {
                CelestialBody home = FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).First();
                yield return ToPercentage(nonKSCScienceMultiplier, "N0") + " bonus to " + home.theName + " Science.";
            }
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            KSCScienceMultiplier = ConfigNodeUtil.ParseValue<float>(node, "KSCScienceMultiplier", 0.0f);
            nonKSCScienceMultiplier = ConfigNodeUtil.ParseValue<float>(node, "nonKSCScienceMultiplier", 0.0f);
        }

        protected override void OnRegister()
        {
            GameEvents.OnScienceRecieved.Add(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
        }

        protected override void OnUnregister()
        {
            GameEvents.OnScienceRecieved.Remove(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
        }

        private void OnScienceReceived(float amount, ScienceSubject subject, ProtoVessel vessel, bool reverseEngineered)
        {
            Biome biome = Science.GetBiome(subject);

            if (biome == null || !biome.body.isHomeWorld)
            {
                return;
            }

            bool isKSC = biome.IsKSC();
            if (KSCScienceMultiplier > 0.0f && isKSC)
            {
                ResearchAndDevelopment.Instance.AddScience(KSCScienceMultiplier * amount - amount, TransactionReasons.Strategies);
            }
            else if (nonKSCScienceMultiplier > 0.0f && !isKSC)
            {
                ResearchAndDevelopment.Instance.AddScience(nonKSCScienceMultiplier * amount - amount, TransactionReasons.Strategies);
            }
        }
    }
}
