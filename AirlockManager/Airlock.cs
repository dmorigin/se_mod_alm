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
        private class Airlock
        {
            class Door
            {
                private IMyDoor block_ = null;


                public Door(IMyDoor block)
                {
                    block_ = block;
                }


                public IMyDoor Obj => block_;
                public bool IsOpen => block_.Status == DoorStatus.Open;
                public bool IsClosed => block_.Status == DoorStatus.Closed;
                public DoorStatus Status => block_.Status;

                public void Open() => block_.OpenDoor();
                public void Close() => block_.CloseDoor();
                public void On() => block_.ApplyAction("OnOff_On");
                public void Off() => block_.ApplyAction("OnOff_Off");
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

            public string updateStatistic()
            {
                return $"{Name}: {CurrentSate}";
            }


            #region Properties
            public string Name => config_.name_;
            public Config.Automatic Automatic => config_.automatic_;
            public bool IsProcessing => state_ != State.Idle;
            public State CurrentSate => state_;
            #endregion // Properties

            #region Tools
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
            #endregion // Tools

            #region Jobs
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
            #endregion // Jobs
        }
    }
}
