using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        /*!
         * Not fully implemented!
         */
        private class Config
        {
            public enum Automatic
            {
                Manually,
                Half,
                Full
            };


            public struct Data
            {
                public string name_;
                public Automatic automatic_;
                public TimeSpan delay_;
            }


            public bool parse(IMyTerminalBlock block, out List<Data> cfg)
            {
                cfg = new List<Data>();

                MyIni ini = new MyIni();
                if (ini.TryParse(block.CustomData))
                {
                    List<string> sections = new List<string>();
                    ini.GetSections(sections);

                    foreach (var section in sections)
                    {
                        Data data = new Data();
                        data.name_ = section;
                        data.delay_ = new TimeSpan(getIniLong(ini, section, "delay", Program.DefaultAirlockDelayTime) * Program.MStoTick);
                        data.automatic_ = getIniAutomatic(ini, section, "automatic", Program.DefaultAutomaticMode);

                        cfg.Add(data);
                    }

                    return true;
                }

                return false;
            }


            public static Data getDefault(string name = "")
            {
                var data = new Data();
                data.name_ = name;
                data.automatic_ = Program.DefaultAutomaticMode;
                data.delay_ = new TimeSpan(Program.DefaultAirlockDelayTime * Program.MStoTick);

                return data;
            }


            #region INI Helper
            private string getIniString(MyIni ini, string section, string key, string defaultValue)
            {
                if (!ini.ContainsKey(section, key))
                    return defaultValue;
                return ini.Get(section, key).ToString();
            }


            private int getIniInteger(MyIni ini, string section, string key, int defaultValue)
            {
                if (!ini.ContainsKey(section, key))
                    return defaultValue;
                return ini.Get(section, key).ToInt32();
            }


            private long getIniLong(MyIni ini, string section, string key, long defaultValue)
            {
                if (!ini.ContainsKey(section, key))
                    return defaultValue;
                return ini.Get(section, key).ToInt64();
            }


            private Config.Automatic getIniAutomatic(MyIni ini, string section, string key, Config.Automatic defaultValue)
            {
                string val = getIniString(ini, section, key, "");

                Config.Automatic result;
                if (!Enum.TryParse<Config.Automatic>(val, out result))
                    return defaultValue;
                return result;
            }
            #endregion // INI Hlper
        }
    }
}
