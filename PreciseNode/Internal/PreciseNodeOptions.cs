using KSP.IO;
using System.IO;
using UnityEngine;


/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met: 
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 ******************************************************************************/

namespace RegexKSP
{
    internal class PreciseNodeOptions
    {
        internal Rect mainWindowPos = new Rect(Screen.width / 10, 20, 0, 0);
        internal Rect optionsWindowPos = new Rect(Screen.width / 3, 20, 0, 0);
        internal Rect keymapperWindowPos = new Rect(Screen.width / 5, 20, 0, 0);
        internal Rect clockWindowPos = new Rect(Screen.width / 3, Screen.height / 2, 0, 0);
        internal Rect conicsWindowPos = new Rect(Screen.width / 5, Screen.height / 2, 0, 0);
        internal Rect tripWindowPos = new Rect(Screen.width / 5, Screen.height / 5, 0, 0);

        internal bool showManeuverPager = true;
        internal bool showConics = true;
        internal bool showAngleInput = true;
        internal bool showConicsAlways;
        internal bool showClock;
        internal bool showTrip;
        internal bool showUTControls;
        internal bool showEAngle = true;
        internal bool showOrbitInfo;
        internal bool intuitiveManeuverGizmos;

        internal bool showTooltips = true;
       // internal bool ???
#if NODE_CLEANUP
		internal bool removeUsedNodes;
#endif

        internal bool largeUTIncrement;

        internal KeyCode progInc = KeyCode.Keypad8;
        internal KeyCode progDec = KeyCode.Keypad5;
        internal KeyCode normInc = KeyCode.Keypad9;
        internal KeyCode normDec = KeyCode.Keypad7;
        internal KeyCode radiInc = KeyCode.Keypad6;
        internal KeyCode radiDec = KeyCode.Keypad4;
        internal KeyCode timeInc = KeyCode.Keypad3;
        internal KeyCode timeDec = KeyCode.Keypad1;
        internal KeyCode pageIncrement = KeyCode.Keypad0;
        internal KeyCode pageConics = KeyCode.KeypadEnter;
        internal KeyCode hideWindow = KeyCode.P;
        internal KeyCode addWidget = KeyCode.O;
        internal double increment = 1.0;
        internal double usedNodeThreshold = 0.5;
        internal int conicsMode = 3;
        internal int angleRefIndex = 0; // 0=Periapsis, 1=AN, 2=VernalEquinox

        internal PreciseNodeOptions()
        {
            LoadSettings();
        }
        internal void downIncrement()
        {
            if (increment == 0.01)
            {
                increment = 0.1;
            }
            else if (increment == 0.1)
            {
                increment = 1;
            }
            else if (increment == 1)
            {
                increment = 10;
            }
            else if (increment == 10)
            {
                increment = 100;
            }
            else if (increment == 100)
            {
                increment = 0.01;
            }
            else
            {
                increment = 1;
            }
        }

        internal void upIncrement()
        {
            if (increment == 0.01)
            {
                increment = 100;
            }
            else if (increment == 0.1)
            {
                increment = 0.01;
            }
            else if (increment == 1)
            {
                increment = 0.1;
            }
            else if (increment == 10)
            {
                increment = 1;
            }
            else if (increment == 100)
            {
                increment = 10;
            }
            else
            {
                increment = 1;
            }
        }

        internal void setConicsMode(int mode)
        {
            conicsMode = mode;
            NodeTools.changeConicsMode(conicsMode);
        }

        internal void pageConicsMode()
        {
            conicsMode++;
            if (conicsMode < 0 || conicsMode > 4)
            {
                conicsMode = 0;
            }
            NodeTools.changeConicsMode(conicsMode);
        }


        string SETTINGS_FILE { get { return ( KSPUtil.ApplicationRootPath + "GameData/PreciseNode/PluginData/settings.cfg").Replace("\\", "/"); } }

        internal void SaveSettings()
        {
            ConfigNode root = new ConfigNode("SETTINGS");
            ConfigNode node = root.AddNode("SETTINGS");
            SaveRect(node, "mainWindowPos", mainWindowPos);
            SaveRect(node, "optionsWindowPos", optionsWindowPos);
            SaveRect(node, "keymapperWindowPos", keymapperWindowPos);
            SaveRect(node, "clockWindowPos", clockWindowPos);
            SaveRect(node, "conicsWindowPos", conicsWindowPos);
            SaveRect(node, "tripWindowPos", tripWindowPos);

            node.AddValue("showManeuverPager", showManeuverPager);
            node.AddValue("showConics", showConics);
            node.AddValue("showAngleInput", showAngleInput);
            node.AddValue("showConicsAlways", showConicsAlways);
            node.AddValue("showClock", showClock);
            node.AddValue("showTrip", showTrip);
            node.AddValue("showUTControls", showUTControls);
            node.AddValue("showEAngle", showEAngle);
            node.AddValue("showOrbitInfo", showOrbitInfo);
            node.AddValue("intuitiveManeuverGizmos", intuitiveManeuverGizmos);
            node.AddValue("showTooltips", showTooltips);
#if NODE_CLEANUP
		    node.AddValue("removeUsedNodes", removeUsedNodes);
#endif

            root.AddNode(node);
            string dir = Path.GetDirectoryName(SETTINGS_FILE);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            root.Save(SETTINGS_FILE);
        }

        internal void LoadSettings()
        {
            if (!System.IO.File.Exists(SETTINGS_FILE))
                return;

            ConfigNode root = ConfigNode.Load(SETTINGS_FILE);
            if (root == null)
                return;
            ConfigNode node = root.GetNode("SETTINGS");
            if (node == null)
                return; 

            mainWindowPos = LoadRect(node, "mainWindowPos", mainWindowPos);
            optionsWindowPos = LoadRect(node, "optionsWindowPos", optionsWindowPos);
            keymapperWindowPos = LoadRect(node, "keymapperWindowPos", keymapperWindowPos);
            clockWindowPos = LoadRect(node, "clockWindowPos", clockWindowPos);
            conicsWindowPos = LoadRect(node, "conicsWindowPos", conicsWindowPos);
            tripWindowPos = LoadRect(node, "tripWindowPos", tripWindowPos);

            showManeuverPager =
                LoadBool(node, "showManeuverPager", showManeuverPager);     
            showConics =
                LoadBool(node, "showConics", showConics);

            showAngleInput =
                LoadBool(node, "showAngleInput", showAngleInput);

            showConicsAlways =
                LoadBool(node, "showConicsAlways", showConicsAlways);

            showClock =
                LoadBool(node, "showClock", showClock);

            showTrip =
                LoadBool(node, "showTrip", showTrip);

            showUTControls =
                LoadBool(node, "showUTControls", showUTControls);

            showEAngle =
                LoadBool(node, "showEAngle", showEAngle);

            showOrbitInfo =
                LoadBool(node, "showOrbitInfo", showOrbitInfo);

            intuitiveManeuverGizmos =
                LoadBool(node, "intuitiveManeuverGizmos", intuitiveManeuverGizmos);

            showTooltips =
                LoadBool(node, "showTooltips", showTooltips);
#if NODE_CLEANUP
            removeUsedNodes =
                LoadBool(node, "removeUsedNodes", removeUsedNodes);
#endif
        }

        private void SaveRect(ConfigNode node, string name, Rect rect)
        {
            ConfigNode r = node.AddNode(name);

            r.AddValue("x", rect.x);
            r.AddValue("y", rect.y);
            r.AddValue("w", rect.width);
            r.AddValue("h", rect.height);
        }

        private Rect LoadRect(ConfigNode node, string name, Rect defaultRect)
        {
            ConfigNode r = node.GetNode(name);

            if (r == null)
                return defaultRect;

            return new Rect(
                LoadFloat(r, "x", defaultRect.x),
                LoadFloat(r, "y", defaultRect.y),
                LoadFloat(r, "w", defaultRect.width),
                LoadFloat(r, "h", defaultRect.height)
            );
        }

        private float LoadFloat(ConfigNode node, string name, float defaultValue)
        {
            if (!node.HasValue(name))
                return defaultValue;

            float value;
            if (float.TryParse(node.GetValue(name), out value))
                return value;

            return defaultValue;
        }

        private bool LoadBool(ConfigNode node, string name, bool defaultValue)
        {
            if (!node.HasValue(name))
                return defaultValue;

            bool value;
            if (bool.TryParse(node.GetValue(name), out value))
                return value;

            return defaultValue;
        }


    }
}
