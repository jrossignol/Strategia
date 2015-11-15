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
    public class CurrencyOperationByBody : CurrencyOperation
    {
        private List<CelestialBody> bodies;

        public CurrencyOperationByBody(Strategy parent)
            : base(parent)
        {
        }

        public CurrencyOperationByBody(Strategy parent, float minValue, float maxValue, Currency currency, CurrencyOperation.Operator op, TransactionReasons AffectReasons, string description)
            : base(parent, minValue, maxValue, currency, op, AffectReasons, description)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);

            List<CelestialBody> includeBodies = ConfigNodeUtil.ParseValue<List<CelestialBody>>(node, "includeBody", new List<CelestialBody>());
            List<CelestialBody> excludeBodies = ConfigNodeUtil.ParseValue<List<CelestialBody>>(node, "excludeBody", new List<CelestialBody>());
            
            // Add the included bodies
            if (includeBodies.Any())
            {
                bodies = includeBodies;
            }
            else
            {
                bodies = FlightGlobals.Bodies.ToList();
            }

            // Remove the excluded ones
            if (excludeBodies.Any())
            {
                bodies.RemoveAll(cb => excludeBodies.Contains(cb));
            }
        }

        protected override void OnRegister()
        {
            GameEvents.Modifiers.OnCurrencyModifierQuery.Add(new EventData<CurrencyModifierQuery>.OnEvent(OnEffectQuery));
        }

        protected override void OnUnregister()
        {
            GameEvents.Modifiers.OnCurrencyModifierQuery.Remove(new EventData<CurrencyModifierQuery>.OnEvent(OnEffectQuery));
        }

        private void OnEffectQuery(CurrencyModifierQuery qry)
        {
            // Check if it's for our body 
            if (FlightGlobals.currentMainBody == null || !bodies.Contains(FlightGlobals.currentMainBody))
            {
                return;
            }

            MethodInfo oeqMethod = typeof(CurrencyOperation).GetMethod("OnEffectQuery", BindingFlags.Instance | BindingFlags.NonPublic);
            oeqMethod.Invoke(this, new object[] { qry });
        }
    }
}
