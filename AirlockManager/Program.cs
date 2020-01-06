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
    partial class Program : MyGridProgram
    {
        const string VERSION = "0.5 Alpha";

        // config values
        static string DefaultAirlockTag = "Airlock";
        static string DefaultAirlockInnerTag = "In";
        static string DefaultAirlockOuterTag = "Out";
        static TimeSpan DefaultAirlockDelayTime = TimeSpan.FromSeconds(1);
        static Config.Automatic DefaultAutomaticMode = Config.Automatic.Full;
        static UpdateFrequency DefualtUpdateFrequency = UpdateFrequency.Update10;

        private delegate bool AirlockPairCallback(Airlock pair);

        // variables
        List<Airlock> airlocks = new List<Airlock>();
        List<Config.Data> configs_ = new List<Config.Data>();
        IMyTextSurface surface_ = null;
        TimeSpan timer = new TimeSpan(0);
        Statistics statistics_ = new Statistics();

        #region Tools
        private string getAirlockName(string customName)
        {
            return customName
                .Replace(DefaultAirlockTag, "")
                .Replace(DefaultAirlockInnerTag, "")
                .Replace(DefaultAirlockOuterTag, "")
                .Trim();
        }


        private bool findAirlockPair(AirlockPairCallback callback, string name)
        {
            foreach (var pair in airlocks)
            {
                if (pair.Name == name)
                    if (callback(pair))
                        return true;
            }

            return false;
        }


        private Config.Data getConfigData(string name)
        {
            foreach (var config in configs_)
            {
                if (config.name_ == name)
                    return config;
            }

            return Config.getDefault(name);
        }


        private void InitializeAirLocks()
        {
            // read config
            Config config = new Config();
            configs_.Clear();
            if (!config.parse(Me, out configs_))
            {
                Echo("Error: Invalid configuration!");
                return;
            }

            // find new doors
            airlocks.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyDoor>(null, (door) =>
            {
                // is an airlock door
                if (door.CustomName.Contains(Program.DefaultAirlockTag))
                {
                    string airlockName = getAirlockName(door.CustomName);

                    if (!findAirlockPair((pair) =>
                    {
                        pair.AddDoor(door);
                        return true;
                    }, airlockName))
                    {
                        // add new one
                        Airlock airLock = new Airlock(getConfigData(airlockName));
                        airLock.AddDoor(door);

                        Echo("Find new airlock pair: [" + airLock.Name + "]");
                        airlocks.Add(airLock);
                    }
                }

                return false;
            });

            // setup display
            surface_ = Me.GetSurface(0);
            surface_.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            surface_.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
            surface_.BackgroundColor = new Color(5, 36, 0);

            surface_.TextPadding = 0.0f;
            surface_.FontSize = 0.7f;
            surface_.FontColor = new Color(0, 216, 23);
            surface_.Font = "debug";
        }

        void InitilizeApp()
        {
            // initialize
            InitializeAirLocks();

            Runtime.UpdateFrequency = DefualtUpdateFrequency;
            statistics_.setSensitivity(Runtime.UpdateFrequency);
        }
        #endregion // Tools

        #region SE Methods
        public Program()
        {
            InitilizeApp();
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                timer += Runtime.TimeSinceLastRun;

                // process iteration step
                foreach (var airlock in airlocks)
                    airlock.Tick(timer);

                string stats = statistics_.update(this);
                if (stats != "")
                {
                    Echo(stats);
                    surface_.WriteText(stats);
                }
            }
            catch (Exception exp)
            {
                statistics_.registerException(exp);
                InitilizeApp();
            }
        }
        #endregion // SE Methods
    }
}
