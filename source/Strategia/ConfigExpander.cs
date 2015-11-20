using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace Strategia
{
    /// <summary>
    /// Special MonoBehaviour for expanding special config files into strategies.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ConfigExpander : MonoBehaviour
    {
        Dictionary<string, int> names = new Dictionary<string, int>();

        public void Awake()
        {
            Debug.Log("Strategia: Expanding configuration");
            DoLoad();
            Destroy(this);
        }

        public void DoLoad()
        {
            // Do Celestial Body expansion
            foreach (UrlDir.UrlConfig config in GameDatabase.Instance.GetConfigs("STRATEGY_BODY_EXPAND"))
            {
                ConfigNode node = config.config;
                Debug.Log("Strategia: Expanding  " + node.GetValue("id"));
                foreach (CelestialBody body in CelestialBodyUtil.GetBodiesForStrategy(node.GetValue("id")))
                {
                    // Duplicate the node
                    ConfigNode newStrategy = ExpandNode(node, body);
                    newStrategy.name = "STRATEGY";

                    // Name must be unique
                    string name = node.GetValue("name");
                    int current;
                    names.TryGetValue(name, out current);
                    names[name] = current + 1;
                    name = name + current;
                    newStrategy.SetValue("name", name);

                    // Duplicate effect nodes
                    foreach (ConfigNode effect in node.GetNodes("EFFECT"))
                    {
                        ConfigNode newEffect = ExpandNode(effect, body);
                        newStrategy.AddNode(newEffect);
                    }

                    // Add the cloned strategy to the config file
                    Debug.Log("Strategia: Generated strategy '" + newStrategy.GetValue("title") + "'");
                    config.parent.configs.Add(new UrlDir.UrlConfig(config.parent, newStrategy));
                }
            }

            // Do level-based expansion
            foreach (UrlDir.UrlConfig config in GameDatabase.Instance.GetConfigs("STRATEGY_LEVEL_EXPAND"))
            {
                ConfigNode node = config.config;
                Debug.Log("Strategia: Expanding  " + node.GetValue("name"));

                int count = ConfigNodeUtil.ParseValue<int>(node, "factorSliderSteps");
                for (int level = 1; level <= count; level++)
                {
                    // Duplicate the node
                    ConfigNode newStrategy = ExpandNode(node, level);
                    newStrategy.name = "STRATEGY";

                    // Name must be unique
                    newStrategy.SetValue("name", newStrategy.GetValue("name") + level);

                    // Set the title
                    newStrategy.SetValue("title", newStrategy.GetValue("title") + " " + StringUtil.IntegerToRoman(level));

                    // Set the factor slider
                    newStrategy.SetValue("factorSliderDefault", ((float)level / ConfigNodeUtil.ParseValue<int>(node, "factorSliderSteps")).ToString(), true);

                    if (newStrategy.HasValue("requiredReputation"))
                    {
                        float requiredReputation = ConfigNodeUtil.ParseValue<float>(newStrategy, "requiredReputation");
                        newStrategy.SetValue("requiredReputationMin", requiredReputation.ToString(), true);
                        newStrategy.SetValue("requiredReputationMax", requiredReputation.ToString(), true);
                    }

                    // Duplicate effect nodes
                    foreach (ConfigNode effect in node.GetNodes("EFFECT"))
                    {
                        ConfigNode newEffect = ExpandNode(effect, level);
                        newStrategy.AddNode(newEffect);
                    }

                    // Add the cloned strategy to the config file
                    Debug.Log("Strategia: Generated strategy '" + newStrategy.GetValue("title") + "'");
                    config.parent.configs.Add(new UrlDir.UrlConfig(config.parent, newStrategy));

                    foreach (ConfigNode.Value pair in newStrategy.values)
                    {
                        Debug.Log("    " + pair.name + " = " + pair.value);
                    }
                }
            }
        }

        public ConfigNode ExpandNode(ConfigNode node, int level)
        {
            ConfigNode newNode = new ConfigNode(node.name);

            foreach (ConfigNode.Value pair in node.values)
            {
                newNode.AddValue(pair.name, pair.value);
            }

            foreach (ConfigNode expandNode in node.GetNodes("EXPAND"))
            {
                string[] nodes = expandNode.GetValues();
                int index = level > nodes.Count() ? nodes.Count() - 1 : level - 1;
                newNode.AddValue(expandNode.values.DistinctNames().First(), nodes[index]);
            }

            return newNode;
        }

        public ConfigNode ExpandNode(ConfigNode node, CelestialBody body)
        {
            ConfigNode newNode = new ConfigNode(node.name);

            foreach (ConfigNode.Value pair in node.values)
            {
                string value = pair.value;
                if (node.HasNode(pair.name))
                {
                    ConfigNode overrideNode = node.GetNode(pair.name);
                    if (overrideNode.HasValue(body.name))
                    {
                        value = overrideNode.GetValue(body.name);
                    }
                }

                if (value.StartsWith("@"))
                {
                    foreach (string listValue in ExpandList(value, body))
                    {
                        newNode.AddValue(pair.name, listValue);
                    }
                }
                else
                {
                    newNode.AddValue(pair.name, FormatBodyString(value, body));
                }
            }

            return newNode;
        }
        
        public IEnumerable<string> ExpandList(string list, CelestialBody body)
        {
            if (list == "@bodies")
            {
                yield return body.name;
                foreach (CelestialBody child in body.orbitingBodies)
                {
                    yield return child.name;
                }
            }
            else
            {
                throw new Exception("Unhandled tag: " + list);
            }
        }

        public string FormatBodyString(string input, CelestialBody body)
        {
            string result = input.
                Replace("$body", body.name).
                Replace("$theBody", body.theName);

            if (result.Contains("$theBodies"))
            {
                result = result.Replace("$theBodies", CelestialBodyUtil.BodyList(Enumerable.Repeat(body, 1).Union(body.orbitingBodies), "and"));
            }
            if (result.Contains("$childBodies"))
            {
                result = result.Replace("$childBodies", CelestialBodyUtil.BodyList(body.orbitingBodies, "and"));
            }
            if (result.Contains("$childBodyCount"))
            {
                result = result.Replace("$childBodyCount", StringUtil.IntegerToRoman(body.orbitingBodies.Count()));
            }

            return result;
        }
    }
}
