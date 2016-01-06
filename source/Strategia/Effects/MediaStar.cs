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
    /// Strategy for giving an extra experience level for media stars.
    /// </summary>
    public class MediaStar : StrategyEffect
    {
        public const string MEDIA_STAR_XP = "MediaStar";

        /// <summary>
        /// Static initializer to hack the kerbal experience/flight log system to add our entries.
        /// </summary>
        static MediaStar()
        {
            Debug.Log("Strategia: Setting up Media Star Experience");

            FieldInfo[] fields = typeof(KerbalRoster).GetFields(BindingFlags.NonPublic | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(null);
                IEnumerable<string> strValues = value as IEnumerable<string>;
                if (strValues != null)
                {
                    // We're looking for the non-Kerbin lists that contain Training, and PlantFlag
                    if (strValues.Contains("Training") && strValues.Contains("PlantFlag"))
                    {
                        List<string> newValues = strValues.ToList();
                        newValues.Add(MEDIA_STAR_XP);
                        field.SetValue(null, newValues.ToArray());
                    }
                    // Also there's the printed version
                    else if (strValues.Contains("Train at") && strValues.Contains("Plant flag on"))
                    {
                        List<string> newValues = strValues.ToList();
                        newValues.Add("Media star from");
                        field.SetValue(null, newValues.ToArray());
                    }

                    continue;
                }

                IEnumerable<float> floatValues = value as IEnumerable<float>;
                if (floatValues != null)
                {
                    if (floatValues.Contains(2.3f))
                    {
                        // Get the list of experience points for the above string entries
                        List<float> newValues = floatValues.ToList();

                        // Add the extra level - worth 3.5 base XP
                        newValues.Add(3.5f);
                        field.SetValue(null, newValues.ToArray());
                    }

                    continue;
                }
            }
        }

        public MediaStar(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            return "Kerbals returning from the surface of other bodies get bonus experience";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
        }

        protected override void OnRegister()
        {
            GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
        }

        private void OnVesselRecovered(ProtoVessel vessel)
        {
            foreach (ProtoCrewMember pcm in VesselUtil.GetVesselCrew(vessel.vesselRef))
            {
                // Award the media star XP for each planet landed on
                foreach (string target in pcm.flightLog.Entries.
                    Where(fle => fle.type == FlightLog.EntryType.Land.ToString()).
                    Select(fle => fle.target).ToList())
                {
                    pcm.flightLog.AddEntry(MEDIA_STAR_XP, target);
                }
            }
        }
    }
}
