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
            foreach (UrlDir.UrlConfig config in GameDatabase.Instance.GetConfigs("STRATEGY_BODY_EXPAND"))
            {
                ConfigNode node = config.config;
                Debug.Log("    doing " + node.GetValue("name"));
                foreach (CelestialBody body in CelestialBodyUtil.GetBodiesForStrategy(node.GetValue("id")))
                {
                    Debug.Log("        doing " + body.name);

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
                    Debug.Log("Generated strategy '" + newStrategy.GetValue("title") + "'");
                    config.parent.configs.Add(new UrlDir.UrlConfig(config.parent, newStrategy));
                }
            }
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

                newNode.AddValue(pair.name, FormatBodyString(value, body));
            }

            return newNode;
        }

        public string FormatBodyString(string input, CelestialBody body)
        {
            return input.
                Replace("$body", body.name).
                Replace("$theBody", body.theName);
        }
    }
}
