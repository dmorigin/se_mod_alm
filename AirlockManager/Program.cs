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
        const string VERSION = "0.3 Alpha";
        const long MStoTick = 10000;


        // Default values
        const string DefultAirlockTag = "Airlock";
        const string DefultAirlockInnerTag = "Inner";
        const string DefultAirlockOuterTag = "Outer";
        const long DefaultAirlockDelayTime = 1000; // set to 1.0s
        const Config.Automatic DefaultAutomaticMode = Config.Automatic.Full;
        const UpdateFrequency DefualtUpdateFrequency = UpdateFrequency.Update1;


        private delegate bool AirlockPairCallback(Airlock pair);


        // variables
        private List<Airlock> airlocks = new List<Airlock>();
        private List<Config.Data> configs_ = new List<Config.Data>();
        private IMyTextSurface surface_ = null;
        private string log_ = "";


        #region Tools
        private string getAirlockName(string customName)
        {
            return customName
                .Replace(Program.DefultAirlockTag, "")
                .Replace(Program.DefultAirlockInnerTag, "")
                .Replace(Program.DefultAirlockOuterTag, "")
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
            airlocks = new List<Airlock>();
            GridTerminalSystem.GetBlocksOfType<IMyDoor>(null, (door) =>
            {
                // is an airlock door
                if (door.CustomName.Contains(Program.DefultAirlockTag))
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
            surface_.FontSize = 1f;
            surface_.FontColor = new Color(0, 216, 23);
            surface_.Font = "debug";
        }
        #endregion // Tools

        #region SE Methods
        public Program()
        {
            Runtime.UpdateFrequency = DefualtUpdateFrequency;

            // initialize
            InitializeAirLocks();
        }


        public void Save()
        {
        }


        // execute iteration
        private TimeSpan timer = new TimeSpan(0);
        private double lastRunTimeMS = 0;
        private int ticks = 0;

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                timer += Runtime.TimeSinceLastRun;
                lastRunTimeMS += Runtime.LastRunTimeMs;

                if (ticks == 0)
                {
                    log_ = $"Airlock Manager (v{Program.VERSION})\n";
                    log_ += "============================\n";
                    log_ += $"Airlocks: {airlocks.Count.ToString()}\n";
                    log_ += $"Avg Exec Time: {(lastRunTimeMS / 100.0).ToString("#0.#######")}ms\n";
                    log_ += "Airlock States\n------------------------------------\n";
                    //log += $"timer: {timer.TotalSeconds.ToString("#0.###")}\n";
                }

                // process iteration step
                foreach (var airlock in airlocks)
                {
                    airlock.Tick(timer);

                    if (ticks == 0)
                    {
                        if (airlock.IsProcessing)
                            log_ += $"{airlock.Name}: {airlock.CurrentSate.ToString()}\n";
                    }
                }

                if (ticks == 0)
                {
                    surface_.WriteText(log_);
                    lastRunTimeMS = 0;
                }

                if (++ticks > 100)
                    ticks = 0;
            }
            catch (Exception exp)
            {
                Echo(exp.ToString());
            }
        }
        #endregion // SE Methods
    }
}
