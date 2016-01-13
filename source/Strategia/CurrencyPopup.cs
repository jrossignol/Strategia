using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Upgradeables;

namespace Strategia
{
    public enum PopupType
    {
        Generic,
        FacilityUpgrade,
    }

    /// <summary>
    /// MonoBehaviour to show text for currency being gained/spent.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class CurrencyPopup : MonoBehaviour
    {
        public CurrencyPopup Instance { get; private set; }

        private GUIStyle popupStyle = null;

        private UpgradeableFacility lastFacility = null;
        private float lastFacilityTime = 0.0f;

        const float DURATION = 1.0f;
        Color fundsColor = new Color(0xB4 / 255.0f, 0xD4 / 255.0f, 0x55 / 255.0f);

        class Popup
        {
            public Transform referencePosition;
            public Currency currency;
            public double amount;
            public string reason;
            public PopupType popupType;

            public float startTime = 0.0f;
            public bool initialized = true;
        }
        List<Popup> popups = new List<Popup>();

        void Awake()
        {
            // Destroy if we're not in a scene with a camera
            if (FlightCamera.fetch.mainCamera == null)
            {
                DestroyImmediate(this);
                return;
            }

            if (FlightCamera.fetch.mainCamera.gameObject.GetComponent<CurrencyPopup>() == null)
            {
                FlightCamera.fetch.mainCamera.gameObject.AddComponent<CurrencyPopup>();

                // Destroy this object - otherwise we'll have two
                DestroyImmediate(this);
                return;
            }

            Instance = this;

            GameEvents.OnKSCFacilityUpgraded.Add(new EventData<UpgradeableFacility, int>.OnEvent(OnKSCFacilityUpgraded));
            GameEvents.Modifiers.OnCurrencyModified.Add(new EventData<CurrencyModifierQuery>.OnEvent(OnCurrencyModified));
        }
        
        void Destroy()
        {
            Instance = null;

            GameEvents.OnKSCFacilityUpgraded.Remove(new EventData<UpgradeableFacility, int>.OnEvent(OnKSCFacilityUpgraded));
            GameEvents.Modifiers.OnCurrencyModified.Remove(new EventData<CurrencyModifierQuery>.OnEvent(OnCurrencyModified));
        }

        public void AddPopup(Currency currency, double amount, string reason, PopupType popupType = PopupType.Generic)
        {
            Popup popup = new Popup();
            popup.currency = currency;
            popup.amount = amount;
            popup.reason = reason;
            popup.popupType = popupType;
            popups.Add(popup);

            // Special stuff
            if (popupType == PopupType.FacilityUpgrade)
            {
                popup.initialized = false;
            }
        }

        void OnGUI()
        {
            // TODO - re-enable
            return;

            SetupStyles();

            foreach (Popup popup in popups.ToList())
            {
                // Set up the facility popup
                if (!popup.initialized && popup.popupType == PopupType.FacilityUpgrade)
                {
                    if (Time.time < lastFacilityTime + 1.0f && lastFacility != null)
                    {
                        popup.referencePosition = lastFacility.transform;
                        popup.initialized = true;
                    }
                    else
                    {
                        continue;
                    }
                }

                // Initialize popup time
                if (popup.startTime == 0.0)
                {
                    popup.startTime = Time.time;
                }

                // Remove the popup after a time delay
                if (Time.time - popup.startTime > DURATION)
                {
                    popups.Remove(popup);
                    continue;
                }

                Vector3 screenPos = FlightCamera.fetch.mainCamera.WorldToScreenPoint(popup.referencePosition.position);

                // Set up position and alpha
                float alpha = Mathf.Clamp(Mathf.Lerp(1.0f, 0.0f, Mathf.InverseLerp(popup.startTime + DURATION - 0.35f, popup.startTime + DURATION, Time.time)), 0.0f, 1.0f);
                float yoffset = Mathf.Lerp(20.0f, 60.0f, Mathf.InverseLerp(popup.startTime, popup.startTime + DURATION, Time.time));
                Rect origin = new Rect(screenPos.x - 100f, Screen.height - screenPos.y - yoffset - 28f, 200f, 28f);

                // Discount stroke/outline effect
                foreach (int x in new int[] {-1, 1, 0})
                {
                    foreach (int y in new int[] {-1, 1, 0})
                    {
                        // Setup styles, position and alpha
                        Color c = (x == 0 && y == 0) ? fundsColor : Color.black;
                        popupStyle.normal.textColor = new Color(c.r, c.g, c.b, alpha);
                        Rect rect = new Rect(origin.xMin + x, origin.yMin + y, origin.width, origin.height);

                        // Draw the text
                        GUI.Box(rect, "£ " + popup.amount.ToString("N0") + " (" + popup.reason + ")", popupStyle);
                    }
                }

            }
        }

        protected void SetupStyles()
        {
            if (popupStyle != null)
            {
                return;
            }

            // Use this to find the right font when KSP 1.1 comes out
            /*Font[] fonts = UnityEngine.Resources.FindObjectsOfTypeAll<Font>();
            foreach (Font f in fonts)
            {
                Debug.Log("font = " + f.name);
            }*/

            popupStyle = new GUIStyle(HighLogic.Skin.label)
            {
                normal =
                {
                    textColor = Color.black
                },
                margin = new RectOffset(),
                padding = new RectOffset(5, 0, 0, 0),
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }

        void OnKSCFacilityUpgraded(UpgradeableFacility facility, int level)
        {
            lastFacility = facility;
            lastFacilityTime = Time.time;

            Popup popup = popups.LastOrDefault();
            if (popup != null)
            {
                popup.referencePosition = facility.transform;
                popup.initialized = true;
            }
        }

        void OnCurrencyModified(CurrencyModifierQuery qry)
        {
            if (qry.reason == TransactionReasons.StructureConstruction)
            {
                AddPopup(Currency.Funds, qry.GetInput(Currency.Funds), "Upgrade", PopupType.FacilityUpgrade);
            }
        }
    }
}
