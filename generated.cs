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

const string VERSION = "0.51 Alpha";

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
}

public class Statistics
{
    char[] runSymbol_ = { '-', '\\', '|', '/' };
    int runSymbolIndex_ = 0;
    char getRunSymbol()
    {
        char sym = runSymbol_[runSymbolIndex_++];
        runSymbolIndex_ %= 4;
        return sym;
    }

    double sensitivity_ = 0.01;
    public void setSensitivity(UpdateFrequency uf)
    {
        switch (uf)
        {
            case UpdateFrequency.Update1:
                sensitivity_ = 1;
                return;
            case UpdateFrequency.Update10:
                sensitivity_ = 0.1;
                return;
            case UpdateFrequency.Update100:
                sensitivity_ = 0.01;
                return;
        }
    }

    string exception_ = "";
    public void registerException(Exception exp)
    {
        exception_ = exp.ToString();
    }

    TimeSpan nextUpdate_ = new TimeSpan(0);
    TimeSpan updateInterval_ = TimeSpan.FromSeconds(1.0);
    TimeSpan ticks_ = new TimeSpan(0);

    StringBuilder sb_ = new StringBuilder();

    long ticksSinceLastUpdate_ = 0;
    double timeSinceLastUpdate_ = 0.0;

    public string update(Program app)
    {
        ticks_ += app.Runtime.TimeSinceLastRun;

        // update
        timeSinceLastUpdate_ += app.Runtime.LastRunTimeMs * sensitivity_;
        ticksSinceLastUpdate_++;

        if (nextUpdate_ <= ticks_)
        {
            // print statistic
            sb_.Clear();
            sb_.AppendLine($"Airlock Manager ({Program.VERSION})\n=============================");
            sb_.AppendLine($"Running: {getRunSymbol()}");
            sb_.AppendLine($"Time: {ticks_}");
            sb_.AppendLine($"Ticks: {ticksSinceLastUpdate_}");
            sb_.AppendLine($"Avg Time/tick: {(timeSinceLastUpdate_ / ticksSinceLastUpdate_).ToString("#0.0#####")}ms");
            sb_.AppendLine($"Airlocks: {app.airlocks.Count}");
            sb_.AppendLine("Airlock States\n------------------------------------");

            foreach(Airlock airlock in app.airlocks)
            {
                if (airlock.IsProcessing)
                    sb_.AppendLine($"{airlock.Name}: {airlock.CurrentSate}");
            }

            if (exception_ != "")
                sb_.Append($"\nException:\n{exception_}\n");

            nextUpdate_ = ticks_ + updateInterval_;
            ticksSinceLastUpdate_ = 0;
            timeSinceLastUpdate_ = 0.0;

            return sb_.ToString();
        }

        return "";
    }
}