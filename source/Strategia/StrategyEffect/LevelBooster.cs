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
    /// Strategy for increasing the a Kerbal's level while the strategy is active.
    /// </summary>
    public class LevelBooster : StrategyEffect
    {
        class CallbackDetail
        {
            public Part part;
            public EventValueComparison<int>.OnEvent callback;
        }

        List<int> levels;
        ProtoCrewMember.Gender? gender;
        string trait;

        private List<CallbackDetail> registeredCallbacks = new List<CallbackDetail>();

        public LevelBooster(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            string genderStr = gender != null ? gender.Value.ToString().ToLower() + " " : "";
            string astronautStr = string.IsNullOrEmpty(trait) ? "astronauts" : (trait.ToLower() + "s");

            int level = Parent.GetLeveledListItem<int>(levels);
            return "All " + genderStr + astronautStr + " perform as if they had " + level + " more level" + (level == 1 ? "" : "s") + " of experience.";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            levels = ConfigNodeUtil.ParseValue<List<int>>(node, "level");
            gender = ConfigNodeUtil.ParseValue<ProtoCrewMember.Gender?>(node, "gender", null);
            trait = ConfigNodeUtil.ParseValue<string>(node, "trait");
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
            HandleVessel(FlightGlobals.ActiveVessel);
        }

        private void OnVesselChange(Vessel vessel)
        {
            HandleVessel(vessel);
        }

        private void HandleVessel(Vessel vessel)
        {
            // Get the level
            int level = Parent.GetLeveledListItem<int>(levels);

            // Update the level for all crew that match up
            foreach (ProtoCrewMember pcm in GetVesselCrew(vessel).
                Where(p =>
                    string.IsNullOrEmpty(trait) || p.experienceTrait.Config.Name == trait &&
                    gender == null || p.gender == gender
                ))
            {
                pcm.experienceLevel = KerbalRoster.CalculateExperienceLevel(pcm.experience) + level;
            }

            return;
        }

        /// <summary>
        /// Gets the vessel crew and works for EVAs as well
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        protected static IEnumerable<ProtoCrewMember> GetVesselCrew(Vessel v)
        {
            if (v == null)
            {
                yield break;
            }

            // EVA vessel
            if (v.vesselType == VesselType.EVA)
            {
                if (v.parts == null)
                {
                    yield break;
                }

                foreach (Part p in v.parts)
                {
                    foreach (ProtoCrewMember pcm in p.protoModuleCrew)
                    {
                        yield return pcm;
                    }
                }
            }
            else
            {
                // Vessel with crew
                foreach (ProtoCrewMember pcm in v.GetVesselCrew())
                {
                    yield return pcm;
                }
            }
        }
    }
}
