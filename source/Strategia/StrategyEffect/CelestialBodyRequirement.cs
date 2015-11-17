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
    public abstract class CelestialBodyRequirement : StrategyEffect, IHiddenEffect, IRequirementEffect
    {
        private IEnumerable<CelestialBody> bodies;
        private string id;
        public bool invert;
        public string Reason
        {
            get
            {
                return "Cannot activate " + (invert ? "after" : "before") + " " + Verb() + " " + CelestialBodyUtil.BodyList(bodies, "or") + ".";
            }
        }

        public CelestialBodyRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            id = ConfigNodeUtil.ParseValue<string>(node, "id", "");
            if (!string.IsNullOrEmpty(id))
            {
                bodies = CelestialBodyUtil.GetBodiesForStrategy(id);
            }
            else if (node.HasValue("body"))
            {
                bodies = ConfigNodeUtil.ParseValue<List<CelestialBody>>(node, "body");
            }
            else
            {
                bodies = FlightGlobals.Bodies.Where(cb => cb.isHomeWorld);
            }
            invert = ConfigNodeUtil.ParseValue<bool>(node, "invert", false);
        }

        public bool RequirementMet()
        {
            return ProgressTracking.Instance.celestialBodyNodes.Where(node => bodies.Contains(node.Body)).Any(cbs => Check(cbs) ^ invert);
        }

        protected abstract bool Check(CelestialBodySubtree cbs);
        protected abstract string Verb();
    }

    public class ReachedBodyRequirement : CelestialBodyRequirement
    {
        public ReachedBodyRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.IsReached;
        }

        protected override string Verb()
        {
            return "reaching";
        }
    }

    public class OrbitBodyRequirement : CelestialBodyRequirement
    {
        public OrbitBodyRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.orbit.IsReached;
        }

        protected override string Verb()
        {
            return "orbiting";
        }
    }

    public class LandedBodyRequirement : CelestialBodyRequirement
    {
        public LandedBodyRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.landing.IsReached;
        }

        protected override string Verb()
        {
            return "landing on";
        }
    }

    public class ReturnFromOrbitRequirement : CelestialBodyRequirement
    {
        public ReturnFromOrbitRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.returnFromOrbit.IsReached;
        }

        protected override string Verb()
        {
            return "returning from orbit of ";
        }
    }

    public class ReturnFromSurfaceRequirement : CelestialBodyRequirement
    {
        public ReturnFromSurfaceRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.returnFromSurface.IsReached;
        }

        protected override string Verb()
        {
            return "returning from the surface of ";
        }
    }
}
