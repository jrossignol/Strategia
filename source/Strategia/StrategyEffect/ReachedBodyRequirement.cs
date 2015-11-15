using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Strategies;
using Strategies.Effects;

namespace Strategia
{
    public class ReachedBodyRequirement : StrategyEffect, IHiddenEffect, IRequirementEffect
    {
        private CelestialBody body;
        public bool invert;
        public string Reason
        {
            get;
            set;
        }

        public ReachedBodyRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            body = ConfigNodeUtil.ParseValue<CelestialBody>(node, "body");
            invert = ConfigNodeUtil.ParseValue<bool>(node, "invert");
            Reason = ConfigNodeUtil.ParseValue<string>(node, "reason");
        }

        public bool RequirementMet()
        {
            return ProgressTracking.Instance.celestialBodyNodes.Single(node => node.Body == body).IsReached ^ invert;
        }
    }
}
