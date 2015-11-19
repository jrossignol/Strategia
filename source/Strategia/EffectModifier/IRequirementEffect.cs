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
    public interface IRequirementEffect
    {
        bool RequirementMet(out string unmetReason);
        string RequirementText();
    }
}
