using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using KSP;
using KSP.UI.Screens;
using Strategies;

namespace Strategia
{
    /// <summary>
    /// Special MonoBehaviour to fix admin building UI.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class AdminResizer : MonoBehaviour
    {
        int ticks = 0;
        public void Awake()
        {
        }

        public void Update()
        {
            // Wait for the strategy system to get loaded
            if (KSP.UI.Screens.Administration.Instance == null)
            {
                ticks = 0;
                return;
            }

            if (ticks++ == 0)
            {
                // Resize the root element that handles the width
                Transform aspectFitter = KSP.UI.Screens.Administration.Instance.transform.FindDeepChild("bg and aspectFitter");
                if (aspectFitter != null)
                {
                    RectTransform rect = aspectFitter.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(Math.Min(1424f, Screen.width), rect.sizeDelta.y);
                }
            }
            else if ((ticks-1) == 1)
            {
                // Workaround for KSP 1.1 bug where the vertical scrollbars don't work
                RectTransform contents = KSP.UI.Screens.Administration.Instance.gameObject.transform.FindDeepChild("scroll list strategies").GetComponent<RectTransform>();
                float maxY = contents.sizeDelta.y;
                for (int i = 0; i < contents.transform.childCount; i++)
                {
                    Transform child = contents.transform.GetChild(i);
                    maxY = Math.Max(child.GetComponent<RectTransform>().sizeDelta.y, contents.sizeDelta.y);
                }
                contents.sizeDelta = new Vector2(contents.sizeDelta.x, maxY);
            }
        }
    }

    public static class TransformExtns
    {
        public static Transform FindDeepChild(this Transform parent, string name)
        {
            var result = parent.Find(name);
            if (result != null)
                return result;
            foreach (Transform child in parent)
            {
                result = child.FindDeepChild(name);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
