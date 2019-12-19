/*
 * ﻿  Airlock Manager
 *   --------------------------------
 * 
 * Author: [DM]Origin
 * Page:   https://www.gamers-shell.de/
 * Source: https://github.com/dmorigin/se_mod_vis
 * 
 * For detailed informations read the README.md on github.
 */

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

private class Airlock
{
    class Door
    {
        private IMyDoor block_ = null;


        public Door(IMyDoor block)
        {
            block_ = block;
        }


        public IMyDoor Obj
        {
            get
            {
                return block_;
            }
        }


        public bool IsOpen
        {
            get
            {
                return block_.Status == DoorStatus.Open;
            }
        }


        public bool IsClosed
        {
            get
            {
                return block_.Status == DoorStatus.Closed;
            }
        }

        public DoorStatus Status
        {
            get
            {
                return block_.Status;
            }
        }


        public void Open()
        {
            block_.OpenDoor();
        }


        public void Close()
        {
            block_.CloseDoor();
        }


        public void On()
        {
            block_.ApplyAction("OnOff_On");
        }


        public void Off()
        {
            block_.ApplyAction("OnOff_Off");
        }
    }


    public enum State
    {
        Init,
        Idle,
        OneSideOpen,
        OtherSideOpen,
        WaitToClose,
        SomeoneInside,
        Closing,
        Closed
    };


    private delegate void JobHandler(TimeSpan timer);

    private Config.Data config_ = new Config.Data();
    private List<Door> doors_ = new List<Door>();
    private State state_ = State.Init;
    private Door activeDoor_ = null;
    private JobHandler job_ = null;
    private TimeSpan waitFor_ = new TimeSpan(0);


    public Airlock(Config.Data config)
    {
        config_ = config;
        job_ = HandleInit;
        state_ = State.Init;
    }


    public void Tick(TimeSpan timer)
    {
        job_(timer);
    }


    public void AddDoor(IMyDoor block)
    {
        doors_.Add(new Door(block));
    }


    public string Name
    {
        get
        {
            return config_.name_;
        }
    }


    public Config.Automatic Automatic
    {
        get
        {
            return config_.automatic_;
        }
    }


    public bool IsProcessing
    {
        get
        {
            return state_ != State.Idle;
        }
    }


    public State CurrentSate
    {
        get
        {
            return state_;
        }
    }

    private Door oneDoorOpen()
    {
        foreach (var door in doors_)
            if (door.IsOpen)
                return door;
        return null;
    }


    private bool allDoorsClosed(Door except = null)
    {
        if (except == null)
        {
            foreach (var door in doors_)
                if (!door.IsClosed)
                    return false;
        }
        else
        {
            foreach (var door in doors_)
            {
                if (door != except && !door.IsClosed)
                    return false;
            }
        }

        return true;
    }


    private bool allDoorsOpen(Door except = null)
    {
        if (except == null)
        {
            foreach (var door in doors_)
                if (!door.IsOpen)
                    return false;
        }
        else
        {
            foreach (var door in doors_)
            {
                if (door != except && !door.IsOpen)
                    return false;
            }
        }

        return true;
    }


    private void closeAllDoors(Door except = null)
    {
        if (except == null)
        {
            foreach (var door in doors_)
            {
                door.On();
                door.Close();
            }
        }
        else
        {
            foreach (var door in doors_)
            {
                if (door != except)
                {
                    door.On();
                    door.Close();
                }
            }
        }
    }


    private void openAllDoors(Door except = null)
    {
        if (except == null)
        {
            foreach (var door in doors_)
            {
                door.On();
                door.Open();
            }
        }
        else
        {
            foreach (var door in doors_)
            {
                if (door != except)
                {
                    door.On();
                    door.Open();
                }
            }
        }
    }


    private void deactivateAllOtherDoors(Door except)
    {
        foreach (var door in doors_)
        {
            if (door != except)
                door.Off();
        }
    }


    private void activateAllDoors()
    {
        foreach (var door in doors_)
            door.On();
    }

    private void HandleInit(TimeSpan timer)
    {
        closeAllDoors();

        if (allDoorsClosed())
        {
            job_ = HandleInteraction;
            activeDoor_ = null;
            state_ = State.Idle;
        }
    }


    private void HandleInteraction(TimeSpan timer)
    {
        activeDoor_ = oneDoorOpen();
        if (activeDoor_ != null)
        {
            state_ = State.OneSideOpen;
            switch (config_.automatic_)
            {
                case Config.Automatic.Manually:
                    job_ = HandleManualllyAirlock;
                    break;
                case Config.Automatic.Half:
                    job_ = HandleHalfAutomaticAirlock;
                    break;
                case Config.Automatic.Full:
                    job_ = HandleFullAutomaticAirlock;
                    break;
            }
        }
    }


    private void HandleManualllyAirlock(TimeSpan timer)
    {
        // there is no need to do something. Al actions are done by
        // the user himself
        job_ = HandleInteraction;
        state_ = State.Idle;
        activeDoor_ = null;
    }


    private void HandleHalfAutomaticAirlock(TimeSpan timer)
    {
        if (state_ == State.OneSideOpen)
        {
            // on side of the airlock is open. Close all other sides
            closeAllDoors(activeDoor_);
            if (allDoorsClosed(activeDoor_))
            {
                // all doors are closed. Deactivate all of them
                deactivateAllOtherDoors(activeDoor_);
                state_ = State.WaitToClose;
                waitFor_ = timer + config_.delay_;
            }
        }
        else if (state_ == State.WaitToClose)
        {
            if (timer >= waitFor_)
            {
                if (!activeDoor_.IsClosed)
                    activeDoor_.Close();
                else if (activeDoor_.IsClosed)
                {
                    activateAllDoors();
                    job_ = HandleInteraction;
                    state_ = State.Idle;
                }
            }
        }
    }


    private void HandleFullAutomaticAirlock(TimeSpan timer)
    {
        if (state_ == State.OneSideOpen)
        {
            // close all other doors
            closeAllDoors(activeDoor_);
            if (allDoorsClosed(activeDoor_))
            {
                // deactivate all doors
                deactivateAllOtherDoors(activeDoor_);
                state_ = State.WaitToClose;
                waitFor_ = timer + config_.delay_;
            }
        }
        else if (state_ == State.WaitToClose)
        {
            // it is time to close the first open door
            if (timer >= waitFor_)
            {
                if (activeDoor_.IsOpen)
                {
                    // now we need to close the door
                    activeDoor_.Close();
                }
                else if (activeDoor_.IsClosed)
                {
                    // switch to next state
                    state_ = State.SomeoneInside;
                    activeDoor_.Off();
                }
            }
        }
        else if (state_ == State.SomeoneInside)
        {
            if (allDoorsClosed())
            {
                openAllDoors(activeDoor_);
            }
            else if (allDoorsOpen(activeDoor_))
            {
                state_ = State.Closing;
                waitFor_ = timer + config_.delay_;
            }
        }
        else if (state_ == State.Closing)
        {
            // end sequence
            if (timer >= waitFor_)
            {
                // close all doors
                closeAllDoors(activeDoor_);
                state_ = State.Closed;
            }
        }
        else if (state_ == State.Closed)
        {
            if (allDoorsClosed())
            {
                activeDoor_.On();
                activeDoor_ = null;
                job_ = HandleInteraction;
                state_ = State.Idle;
            }
        }
    }
}

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
}