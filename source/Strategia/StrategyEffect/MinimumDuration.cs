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
    public class MinimumDuration : StrategyEffect, IHiddenEffect, ICanDeactivateEffect, IExtraTextEffect
    {
        /// <summary>
        /// Seperate MonoBehaviour for checking, as the strategy system only gets update calls in flight.
        /// </summary>
        [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
        public class DurationChecker : MonoBehaviour
        {
            public static DurationChecker Instance;
            public List<MinimumDuration> durations = new List<MinimumDuration>();

            void Start()
            {
                Instance = this;
            }

            public void Register(MinimumDuration minimumDuration)
            {
                durations.AddUnique(minimumDuration);
            }

            public void Unregister(MinimumDuration minimumDuration)
            {
                durations.Remove(minimumDuration);
            }

            public void Update()
            {
                foreach (MinimumDuration minimumDuration in durations)
                {
                    if (minimumDuration.Parent.IsActive &&
                        minimumDuration.duration + minimumDuration.Parent.DateActivated < Planetarium.fetch.time &&
                        !string.IsNullOrEmpty(minimumDuration.failureMsg))
                    {
                        string penalties = "";
                        if (minimumDuration.reputationPenalty > 0 || minimumDuration.fundsPenalty > 0)
                        {
                            penalties = "\n\n<b><#ED0B0B>Penalties: </></>";
                        }
                        if (minimumDuration.fundsPenalty > 0 && Funding.Instance != null)
                        {
                            Funding.Instance.AddFunds(-minimumDuration.fundsPenalty, TransactionReasons.Strategies);
                            penalties += "<#B4D455>£-" + minimumDuration.fundsPenalty.ToString("N0") + "</>    ";
                        }
                        if (minimumDuration.reputationPenalty > 0 && Reputation.Instance != null)
                        {
                            Reputation.Instance.AddReputation((float)-minimumDuration.reputationPenalty, TransactionReasons.Strategies);
                            penalties += "<#E0D503>¡-" + minimumDuration.reputationPenalty.ToString("N0") + "</>    ";
                        }

                        MessageSystem.Instance.AddMessage(new MessageSystem.Message("Failed to complete strategy '" + minimumDuration.Parent.Title + "'",
                            minimumDuration.failureMsg + penalties, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.FAIL));

                        minimumDuration.Parent.Deactivate();
                    }
                }
            }
        }

        public double duration;
        public double reputationPenalty;
        public double fundsPenalty;
        public string failureMsg;

        public MinimumDuration(Strategy parent)
            : base(parent)
        {
        }

        public string ExtraText()
        {
            string text = string.IsNullOrEmpty(failureMsg) ?
                "Must be active for at least " :
                "Must complete objective within ";
            text += KSPUtil.PrintDateDelta((int)duration, false);

            return BuildText(text);
        }

        public string BuildText(string msg)
        {
            string penalties = "";
            if (reputationPenalty > 0 || fundsPenalty > 0)
            {
                penalties = "\n\n<b><#ED0B0B>Penalties: </></>";
                if (fundsPenalty > 0)
                {
                    penalties += "<#B4D455>£-" + fundsPenalty.ToString("N0") + "</>    ";
                }
                if (reputationPenalty > 0)
                {
                    penalties += "<#E0D503>¡-" + reputationPenalty.ToString("N0") + "</>    ";
                }
            }

            return "<" + XKCDColors.HexFormat.KSPNotSoGoodOrange + ">" + msg + "</>" + penalties + "\n";
        }

        protected string MinimumDurationText()
        {
            return (string.IsNullOrEmpty(failureMsg) ?
                "Cannot deactivate until after " :
                "Cannot deactivate, objective must be completed before ") +
                KSPUtil.PrintDateNew((int)(duration + Parent.DateActivated), false);
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            duration = ConfigNodeUtil.ParseValue<Duration>(node, "duration").Value;
            reputationPenalty = ConfigNodeUtil.ParseValue<double>(node, "reputationPenalty");
            fundsPenalty = ConfigNodeUtil.ParseValue<double>(node, "fundsPenalty");
            failureMsg = ConfigNodeUtil.ParseValue<string>(node, "failureMsg");
        }

        protected override void OnRegister()
        {
            DurationChecker.Instance.Register(this);
        }

        protected override void OnUnregister()
        {
            DurationChecker.Instance.Unregister(this);
        }

        public bool CanDeactivate(ref string reason)
        {
            if (Parent.DateActivated + duration > Planetarium.fetch.time)
            {
                reason = MinimumDurationText();
                return false;
            }

            return true;
        }
    }
}
