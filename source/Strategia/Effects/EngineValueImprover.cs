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

namespace Strategia
{
    /// <summary>
    /// Strategy effect that improves engine values.
    /// </summary>
    public class EngineValueImprover : StrategyEffect
    {
        string trait;
        List<float> multipliers;

        private static Dictionary<string, float> originalValues = new Dictionary<string, float>();

        public EngineValueImprover(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            float multiplier = Parent.GetLeveledListItem(multipliers);
            string multiplierStr = ToPercentage(multiplier, "F1");

            return "Engine ISP increased by " + multiplierStr + " when " + StringUtil.ATrait(trait) + " is on board.";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);

            multipliers = ConfigNodeUtil.ParseValue<List<float>>(node, "multiplier");
            trait = ConfigNodeUtil.ParseValue<string>(node, "trait");
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }

        protected override void OnRegister()
        {
            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onFlightReady.Add(new EventVoid.OnEvent(OnFlightReady));
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onFlightReady.Remove(new EventVoid.OnEvent(OnFlightReady));
        }

        private void OnFlightReady()
        {
            Debug.Log("Strategia.EngineValueImprover.OnFlightReady");
            HandleVessel(FlightGlobals.ActiveVessel);
        }

        private void OnVesselChange(Vessel vessel)
        {
            Debug.Log("Strategia.EngineValueImprover.OnVesselChange");
            HandleVessel(vessel);
        }

        private void HandleVessel(Vessel vessel)
        {
            Debug.Log("Strategia.EngineValueImprover.HandleVessel");

            // Check for our trait
            bool needsIncrease = false;
            foreach (ProtoCrewMember pcm in VesselUtil.GetVesselCrew(vessel))
            {
                if (pcm.experienceTrait.Config.Name == trait)
                {
                    needsIncrease = true;
                    break;
                }
            }

            // Multiplier to use
            float multiplier = Parent.GetLeveledListItem(multipliers);

            // Find all engines
            foreach (Part p in vessel.parts)
            {
                foreach (PartModule m in p.Modules)
                {
                    ModuleEngines engine = m as ModuleEngines;
                    if (engine != null)
                    {
                        Debug.Log("Got an engine in part " + p.partName);
                        FloatCurve curve = engine.atmosphereCurve;
                        ConfigNode node = new ConfigNode();
                        curve.Save(node);

                        // Find and adjust the vacuum ISP
                        ConfigNode newNode = new ConfigNode();
                        foreach (ConfigNode.Value pair in node.values)
                        {
                            string[] values = pair.value.Split(new char[] { ' ' });
                            if (values[0] == "0")
                            {
                                // Cache the original value
                                if (!originalValues.ContainsKey(p.partName))
                                {
                                    originalValues[p.partName] = float.Parse(values[1]);
                                }

                                values[1] = (originalValues[p.partName] * (needsIncrease ? multiplier : 1.0f)).ToString("F1");
                                newNode.AddValue(pair.name, string.Join(" ", values));
                            }
                            else
                            {
                                newNode.AddValue(pair.name, pair.value);
                            }
                            Debug.Log("    node data " + pair.name + " = " + pair.value);
                        }
                        curve.Load(newNode);
                    }
                }
            }
        }
    }
}
