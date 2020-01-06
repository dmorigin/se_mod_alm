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
                    // read system config
                    if (ini.ContainsSection("system"))
                    {
                        Program.DefaultAirlockTag = getIniString(ini, "system", "tag", "Airlock");
                        Program.DefaultAirlockInnerTag = getIniString(ini, "system", "innertag", "In");
                        Program.DefaultAirlockOuterTag = getIniString(ini, "system", "outertag", "Out");
                        Program.DefaultAirlockDelayTime = TimeSpan.FromSeconds(getIniDouble(ini, "system", "delay", 1.0));
                        Program.DefaultAutomaticMode = getIniAutomatic(ini, "system", "mode", Automatic.Full);

                        switch (getIniString(ini, "system", "interval", "10"))
                        {
                            case "1":
                                Program.DefualtUpdateFrequency = UpdateFrequency.Update1;
                                break;
                            case "100":
                                Program.DefualtUpdateFrequency = UpdateFrequency.Update100;
                                break;
                            default:
                                Program.DefualtUpdateFrequency = UpdateFrequency.Update10;
                                break;
                        }
                    }

                    // read airlock config
                    List<string> sections = new List<string>();
                    ini.GetSections(sections);

                    foreach (var section in sections)
                    {
                        if (section != "system")
                        {
                            Data data = new Data();
                            data.name_ = section;
                            data.delay_ = TimeSpan.FromSeconds(getIniDouble(ini, section, "delay", Program.DefaultAirlockDelayTime.Seconds));
                            data.automatic_ = getIniAutomatic(ini, section, "automatic", Program.DefaultAutomaticMode);

                            cfg.Add(data);
                        }
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
                data.delay_ = Program.DefaultAirlockDelayTime;

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


            private double getIniDouble(MyIni ini, string section, string key, double defaultValue)
            {
                if (!ini.ContainsKey(section, key))
                    return defaultValue;
                return ini.Get(section, key).ToDouble(defaultValue);
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
