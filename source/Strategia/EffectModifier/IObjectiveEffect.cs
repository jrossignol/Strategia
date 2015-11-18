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
    public interface IObjectiveEffect
    {
        string ObjectiveText();

        double fundsAward { get; }
        float scienceAward { get; }
        float reputationAward { get; }
        double fundsPenalty { get; }
        float sciencePenalty { get; }
        float reputationPenalty { get; }
    }
}
