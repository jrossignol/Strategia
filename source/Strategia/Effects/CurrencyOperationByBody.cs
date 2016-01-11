using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Strategies;
using Strategies.Effects;

namespace Strategia
{
    public class CurrencyOperationByBody : CurrencyOperation
    {
        private List<CelestialBody> bodies;

        private CelestialBody lastBody;

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
            GameEvents.OnProgressComplete.Add(new EventData<ProgressNode>.OnEvent(OnProgressComplete));
        }

        protected override void OnUnregister()
        {
            GameEvents.Modifiers.OnCurrencyModifierQuery.Remove(new EventData<CurrencyModifierQuery>.OnEvent(OnEffectQuery));
            GameEvents.OnProgressComplete.Remove(new EventData<ProgressNode>.OnEvent(OnProgressComplete));
        }

        private void OnProgressComplete(ProgressNode node)
        {
            CelestialBodySubtree cbs = node as CelestialBodySubtree;
            if (cbs != null)
            {
                lastBody = cbs.Body;
            }
            else
            {
                // Reflection hack time.  There is a member that is private that stores the celestial body
                FieldInfo cbField = node.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(fi => fi.FieldType == typeof(CelestialBody)).FirstOrDefault();
                if (cbField != null)
                {
                    lastBody = (CelestialBody)cbField.GetValue(node);
                }
                else
                {
                    lastBody = null;
                }
            }
        }

        private void OnEffectQuery(CurrencyModifierQuery qry)
        {
            // Check if it's for our body 
            if (lastBody == null || !bodies.Contains(lastBody))
            {
                return;
            }

            MethodInfo oeqMethod = typeof(CurrencyOperation).GetMethod("OnEffectQuery", BindingFlags.Instance | BindingFlags.NonPublic);
            oeqMethod.Invoke(this, new object[] { qry });
        }
    }
}
