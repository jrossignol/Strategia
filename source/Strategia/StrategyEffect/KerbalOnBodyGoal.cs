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
    public class KerbalOnBodyGoal : StrategyEffect, IHiddenEffect, IExtraTextEffect
    {
        private List<CelestialBody> bodies;
        private List<CelestialBody> landedBodies = new List<CelestialBody>();
        public double reputationAward;
        public double fundsAward;
        public string requirementMsg;
        public string successMsg;

        public KerbalOnBodyGoal(Strategy parent)
            : base(parent)
        {
        }

        public string ExtraText()
        {
            return BuildText(requirementMsg);
        }

        public string BuildText(string msg)
        {
            string extras = "";
            if (reputationAward > 0 || fundsAward > 0)
            {
                extras = "\n\n<b><#8BED8B>Rewards: </></>";
                if (fundsAward > 0)
                {
                    extras += "<#B4D455>£" + fundsAward.ToString("N0") + "</>    ";
                }
                if (reputationAward > 0)
                {
                    extras += "<#E0D503>¡" + reputationAward.ToString("N0") + "</>    ";
                }
            }

            return "<" + XKCDColors.HexFormat.KSPBadassGreen + ">" + msg + "</>" + extras + "\n";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            bodies = ConfigNodeUtil.ParseValue<List<CelestialBody>>(node, "body");
            reputationAward = ConfigNodeUtil.ParseValue<double>(node, "reputationAward");
            fundsAward = ConfigNodeUtil.ParseValue<double>(node, "fundsAward");
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
            if (reputationAward > 0 && Reputation.Instance != null)
            {
                Reputation.Instance.AddReputation((float)reputationAward, TransactionReasons.Strategies);
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
