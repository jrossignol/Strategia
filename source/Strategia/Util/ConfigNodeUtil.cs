using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Contracts.Agents;
using ContractConfigurator;

namespace Strategia
{
    /// <summary>
    /// Utility class for dealing with ConfigNode objects.
    /// </summary>
    public static class ConfigNodeUtil
    {
        /// <summary>
        /// Parses a value from a config node.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="configNode">The ConfigNode to read from</param>
        /// <param name="key">The key to examine.</param>
        /// <returns>The parsed value</returns>
        public static T ParseValue<T>(ConfigNode configNode, string key)
        {
            // Check for required value
            if (!configNode.HasValue(key))
            {
                throw new ArgumentException("Missing required value '" + key + "'.");
            }

            // Special cases
            if (typeof(T).Name == "List`1")
            {
                // Create the list instance
                T list = (T)Activator.CreateInstance(typeof(T));

                // Create the generic methods
                MethodInfo parseValueMethod = typeof(ConfigNodeUtil).GetMethods().Where(m => m.Name == "ParseSingleValue").Single();
                parseValueMethod = parseValueMethod.MakeGenericMethod(typeof(T).GetGenericArguments());
                MethodInfo addMethod = typeof(T).GetMethod("Add");

                // Populate the list
                int count = configNode.GetValues(key).Count();
                for (int i = 0; i < count; i++)
                {
                    string strVal = configNode.GetValue(key, i);
                    addMethod.Invoke(list, new object[] { parseValueMethod.Invoke(null, new object[] { key, strVal }) });
                }

                return list;
            }
            else if (typeof(T).Name == "Nullable`1")
            {
                // Create the generic method
                MethodInfo parseValueMethod = typeof(ConfigNodeUtil).GetMethod("ParseValue",
                    BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(ConfigNode), typeof(string) }, null);
                parseValueMethod = parseValueMethod.MakeGenericMethod(typeof(T).GetGenericArguments());

                // Call it
                return (T)parseValueMethod.Invoke(null, new object[] { configNode, key });
            }

            // Get string value, pass to parse single value function
            string stringValue = configNode.GetValue(key);
            return ParseSingleValue<T>(key, stringValue);
        }

        public static T ParseSingleValue<T>(string key, string stringValue)
        {
            T value;

            // Handle nullable
            if (typeof(T).Name == "Nullable`1")
            {
                if (typeof(T).GetGenericArguments()[0].IsEnum)
                {
                    value = (T)Enum.Parse(typeof(T).GetGenericArguments()[0], stringValue);
                }
                else
                {
                    value = (T)Convert.ChangeType(stringValue, typeof(T).GetGenericArguments()[0]);
                }
            }
            // Enum parsing logic
            else if (typeof(T).IsEnum)
            {
                value = (T)Enum.Parse(typeof(T), stringValue);
            }
            else if (typeof(T) == typeof(AvailablePart))
            {
                value = (T)(object)ParsePartValue(stringValue);
            }
            else if (typeof(T) == typeof(CelestialBody))
            {
                value = (T)(object)ParseCelestialBodyValue(stringValue);
            }
            else if (typeof(T) == typeof(PartResourceDefinition))
            {
                value = (T)(object)ParseResourceValue(stringValue);
            }
            else if (typeof(T) == typeof(Agent))
            {
                value = (T)(object)ParseAgentValue(stringValue);
            }
            else if (typeof(T) == typeof(Duration))
            {
                value = (T)(object)new Duration(DurationUtil.ParseDuration(stringValue));
            }
            else if (typeof(T) == typeof(Guid))
            {
                value = (T)(object)new Guid(stringValue);
            }
            else if (typeof(T) == typeof(Vector3))
            {
                string[] vals = stringValue.Split(new char[] { ',' });
                float x = (float)Convert.ChangeType(vals[0], typeof(float));
                float y = (float)Convert.ChangeType(vals[1], typeof(float));
                float z = (float)Convert.ChangeType(vals[2], typeof(float));
                value = (T)(object)new Vector3(x, y, z);
            }
            else if (typeof(T) == typeof(Vector3d))
            {
                string[] vals = stringValue.Split(new char[] { ',' });
                double x = (double)Convert.ChangeType(vals[0], typeof(double));
                double y = (double)Convert.ChangeType(vals[1], typeof(double));
                double z = (double)Convert.ChangeType(vals[2], typeof(double));
                value = (T)(object)new Vector3d(x, y, z);
            }
            else if (typeof(T) == typeof(ScienceSubject))
            {
                value = (T)(object)(ResearchAndDevelopment.Instance != null ? ResearchAndDevelopment.GetSubjectByID(stringValue) : null);
            }
            else if (typeof(T) == typeof(Color))
            {
                if ((stringValue.Length != 7 && stringValue.Length != 9) || stringValue[0] != '#')
                {
                    throw new ArgumentException("Invalid color code '" + stringValue + "': Must be # followed by 6 or 8 hex digits (ARGB or RGB).");
                }
                stringValue = stringValue.Replace("#", "");
                int a = 255;
                if (stringValue.Length == 8)
                {
                    a = byte.Parse(stringValue.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    stringValue = stringValue.Substring(2, 6);
                }
                int r = byte.Parse(stringValue.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                int g = byte.Parse(stringValue.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                int b = byte.Parse(stringValue.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

                value = (T)(object)(new Color(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f));
            }
            // Do newline conversions
            else if (typeof(T) == typeof(string))
            {
                value = (T)(object)stringValue.Replace("\\n", "\n");
            }
            // Try a basic type
            else
            {
                value = (T)Convert.ChangeType(stringValue, typeof(T));
            }

            return value;
        }

        /// <summary>
        /// Attempts to parse a value from the config node.  Returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type of value to convert to.</typeparam>
        /// <param name="configNode">The ConfigNode to read from.</param>
        /// <param name="key">The key to examine.</param>
        /// <param name="defaultValue">The default value to return.</param>
        /// <returns>The parsed value (or default value if not found)</returns>
        public static T ParseValue<T>(ConfigNode configNode, string key, T defaultValue)
        {
            if (configNode.HasValue(key) || configNode.HasNode(key))
            {
                return ParseValue<T>(configNode, key);
            }
            else
            {
                return defaultValue;
            }
        }

        public static CelestialBody ParseCelestialBodyValue(string celestialName)
        {
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.name.Equals(celestialName))
                {
                    return body;
                }
            }

            throw new ArgumentException("'" + celestialName + "' is not a valid CelestialBody.");
        }

        private static AvailablePart ParsePartValue(string partName)
        {
            // Underscores in part names get replaced with spaces.  Nobody knows why.
            partName = partName.Replace('_', '.');

            // Get the part
            AvailablePart part = PartLoader.getPartInfoByName(partName);
            if (part == null)
            {
                throw new ArgumentException("'" + partName + "' is not a valid Part.");
            }

            return part;
        }

        private static PartResourceDefinition ParseResourceValue(string name)
        {
            PartResourceDefinition resource = PartResourceLibrary.Instance.resourceDefinitions.Where(prd => prd.name == name).First();
            if (resource == null)
            {
                throw new ArgumentException("'" + name + "' is not a valid resource.");
            }

            return resource;
        }

        private static Agent ParseAgentValue(string name)
        {
            Agent agent = AgentList.Instance.GetAgent(name);
            if (agent == null)
            {
                throw new ArgumentException("'" + name + "' is not a valid agent.");
            }

            return agent;
        }
    }
}
