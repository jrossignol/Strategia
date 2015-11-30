using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Strategies.Effects;

namespace Strategia
{
    public class KerbalOnBodyGoal : StrategyEffect, IObjectiveEffect
    {
        private List<CelestialBody> bodies;
        private List<CelestialBody> landedBodies = new List<CelestialBody>();
        public double fundsAward { get; private set; }
        public float scienceAward { get; private set; }
        public float reputationAward { get; private set; }
        public double fundsPenalty { get; private set; }
        public float sciencePenalty { get; private set; }
        public float reputationPenalty { get; private set; }
        public string requirementMsg;
        public string successMsg;

        public KerbalOnBodyGoal(Strategy parent)
            : base(parent)
        {
        }

        public string ObjectiveText()
        {
            return requirementMsg;
        }

        public string BuildText(string msg)
        {
            string extras = "";
            if (reputationAward > 0 || fundsAward > 0)
            {
                extras = "\n\n<b><#8BED8B>Rewards: </></>";
                if (fundsAward > 0)
                {
                    extras += "<#B4D455>£" + fundsAward.ToString("N0") + "    </>";
                }
                if (scienceAward > 0)
                {
                    extras += "<#6DCFF6>©" + scienceAward.ToString("N0") + "    </>";
                }
                if (reputationAward > 0)
                {
                    extras += "<#E0D503>¡" + reputationAward.ToString("N0") + "    </>";
                }
            }

            return msg + extras;
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            bodies = ConfigNodeUtil.ParseValue<List<CelestialBody>>(node, "body");
            fundsAward = ConfigNodeUtil.ParseValue<double>(node, "fundsAward", 0.0);
            scienceAward = ConfigNodeUtil.ParseValue<float>(node, "scienceAward", 0.0f);
            reputationAward = ConfigNodeUtil.ParseValue<float>(node, "reputationAward", 0.0f);
            requirementMsg = ConfigNodeUtil.ParseValue<string>(node, "requirementMsg");
            successMsg = ConfigNodeUtil.ParseValue<string>(node, "successMsg");
        }

        protected override void OnRegister()
        {
            GameEvents.onVesselSituationChange.Add(new EventData<GameEvents.HostedFromToAction<Vessel, Vessel.Situations>>.OnEvent(OnVesselSituationChange));
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselSituationChange.Remove(new EventData<GameEvents.HostedFromToAction<Vessel, Vessel.Situations>>.OnEvent(OnVesselSituationChange));
        }

        public void OnVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> fta)
        {
            if (fta.host == null || !(fta.to == Vessel.Situations.LANDED || fta.to == Vessel.Situations.SPLASHED))
            {
                return;
            }

            if (fta.host.vesselType == VesselType.EVA)
            {
                CheckCompletion(fta.host.mainBody);
            }
        }

        private void CheckCompletion(CelestialBody body)
        {
            landedBodies.AddUnique(body);

            if (landedBodies.Count() == bodies.Count())
            {
                DoCompletion();
            }
        }

        private void DoCompletion()
        {
            if (fundsAward > 0 && Funding.Instance != null)
            {
                Funding.Instance.AddFunds(fundsAward, TransactionReasons.Strategies);
            }
            if (scienceAward > 0 && ResearchAndDevelopment.Instance != null)
            {
                ResearchAndDevelopment.Instance.AddScience(scienceAward, TransactionReasons.Strategies);
            }
            if (reputationAward > 0 && Reputation.Instance != null)
            {
                Reputation.Instance.AddReputation(reputationAward, TransactionReasons.Strategies);
            }

            string msg = BuildText(successMsg);
            MessageSystem.Instance.AddMessage(new MessageSystem.Message("Completed strategy '" + Parent.Title + "'",
                msg, MessageSystemButton.MessageButtonColor.GREEN, MessageSystemButton.ButtonIcons.ACHIEVE));

            Parent.Deactivate();
        }

        protected override void OnSave(ConfigNode node)
        {
            foreach (CelestialBody body in landedBodies)
            {
                node.AddValue("landedBody", body.name);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            landedBodies = ConfigNodeUtil.ParseValue<List<CelestialBody>>(node, "landedBody", new List<CelestialBody>());
        }
    }
}
