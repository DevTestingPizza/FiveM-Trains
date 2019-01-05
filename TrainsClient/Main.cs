using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace TrainsClient
{
    public class Main : BaseScript
    {
        private bool justSetup = false;

        private bool CreateBlips => (GetConvar("create_train_blips", "true") ?? "true").ToLower() == "true";

        private List<int> TrainHandles
        {
            get
            {
                List<int> trains = new List<int>();
                int entity = 0;
                int findHandle = FindFirstVehicle(ref entity);
                if (DoesEntityExist(entity) && IsThisModelATrain((uint)GetEntityModel(entity)))
                {
                    if ((uint)GetEntityModel(entity) == (uint)GetHashKey("METROTRAIN"))
                    {
                        if (GetTrainCarriage(entity, 1) != 0)
                        {
                            trains.Add(entity);
                        }
                    }
                    else if ((uint)GetEntityModel(entity) == (uint)GetHashKey("FREIGHT"))
                    {
                        trains.Add(entity);
                    }
                }
                while (FindNextVehicle(findHandle, ref entity))
                {
                    if (DoesEntityExist(entity) && IsThisModelATrain((uint)GetEntityModel(entity)))
                    {
                        if ((uint)GetEntityModel(entity) == (uint)GetHashKey("METROTRAIN"))
                        {
                            if (GetTrainCarriage(entity, 1) != 0)
                            {
                                trains.Add(entity);
                            }
                        }
                        else if ((uint)GetEntityModel(entity) == (uint)GetHashKey("FREIGHT"))
                        {
                            trains.Add(entity);
                        }
                    }
                }
                EndFindVehicle(findHandle);
                return trains;
            }
        }

        private readonly List<string> TrainModels = new List<string>()
        {
            "CABLECAR",
            "FREIGHT",
            "FREIGHTCAR",
            "FREIGHTCONT1",
            "FREIGHTCONT2",
            "FREIGHTGRAIN",
            "TANKERCAR",
            "METROTRAIN",
            "S_M_M_LSMETRO_01" // not a train, but this model should also be loaded. (train driver ped model)
        };

        // Metro train station stops
        private readonly List<Vector3> TrainStops = new List<Vector3>()
        {
            // los santos airport (airport front door entrance)
            new Vector3(-1088.627f, -2709.362f, -7.137819f),
            new Vector3(-1081.309f, -2725.259f, -7.137033f),

            // los santos airport (car park/highway entrance)
            new Vector3(-889.2755f, -2311.825f, -11.45941f),
            new Vector3(-876.7512f, -2323.808f, -11.45609f),

            // Little Seoul (near los santos harbor)
            new Vector3(-545.3138f, -1280.548f, 27.09238f),
            new Vector3(-536.8082f, -1286.096f, 27.08768f),

            // Strawberry (near strip club)
            new Vector3(270.2029f, -1210.818f, 39.25398f),
            new Vector3(265.3616f, -1198.051f, 39.23406f), // vector3(262.6042, -1210.32, 39.23338)

            // Rockford Hills (San Vitus Blvd)
            new Vector3(-286.3837f, -318.8773f, 10.33625f),
            new Vector3(-302.6719f, -322.9958f, 10.33629f),

            // Rockford Hills (Near golf club)
            new Vector3(-826.3845f, -134.7151f, 20.22362f),
            new Vector3(-816.7159f, -147.4567f, 20.2231f),

            // Del Perro (Near beach)
            new Vector3(-1351.282f, -481.2916f, 15.318f),
            new Vector3(-1341.085f, -467.674f, 15.31838f),

            // Little Seoul
            new Vector3(-496.0209f, -681.0325f, 12.08264f),
            new Vector3(-495.8456f, -665.4668f, 12.08244f),

            // Pillbox Hill (Downtown)
            new Vector3(-218.2868f, -1031.54f, 30.51112f),
            new Vector3(-209.6845f, -1037.544f, 30.50939f),

            // Davis (Gang / hood area)
            new Vector3(112.3714f, -1729.233f, 30.24097f),
            new Vector3(120.0308f, -1723.956f, 30.31433f)
        };

        /// <summary>
        /// Constructor
        /// </summary>
        public Main()
        {
            SetTrain();
            EventHandlers.Add(GetCurrentResourceName() + ":GetTrainNetworkHandle", new Action(SetTrain));
            Tick += ManageTrainStops;
        }

        List<int> stoppingTrains = new List<int>();

        private async Task ManageTrainStops()
        {
            foreach (var train in TrainHandles)
            {
                if (DoesEntityExist(train))
                {
                    if (IsVehicleModel(train, (uint)GetHashKey("METROTRAIN")))
                    {
                        Vector3 pos = GetEntityCoords(train, false);
                        if (pos != null)
                        {
                            if ((stoppingTrains.Count > 0 && !stoppingTrains.Contains(train)) || stoppingTrains.Count < 1)
                            {
                                if (TrainStops.Any((stop) => stop.DistanceToSquared(pos) < 10f))
                                {
                                    StopTrain(train);
                                }
                            }
                        }
                    }
                }
            }
            await Task.FromResult(0);
        }

        private async void StopTrain(int train)
        {
            if (NetworkHasControlOfEntity(train) && NetworkHasControlOfEntity(GetTrainCarriage(train, 1)))
            {
                stoppingTrains.Add(train);
                SetTrainCruiseSpeed(train, 0f);

                var trainCarriage = GetTrainCarriage(train, 1);

                Vehicle mainTrain = (Vehicle)Vehicle.FromHandle(train);
                Vehicle cariageTrain = (Vehicle)Vehicle.FromHandle(trainCarriage);

                if (mainTrain != null && cariageTrain != null)
                {
                    // Get all doors from main train and it's carriage.
                    var doors = mainTrain.Doors.GetAll().ToList();
                    doors.AddRange(cariageTrain.Doors.GetAll().ToList());

                    // doors to keep closed. (the inverse of the other thing below)
                    var doorsToClose = doors.Where((d) => (GetTrainCarriage(d.Vehicle.Handle, 1) != 0 ? ((float)d.Index % 2 != 0) : ((float)d.Index % 2 == 0))).ToList();

                    // Filter the list to only make train doors open on the side of the platform, and the others remain closed.
                    doors = doors.Where((d) => (GetTrainCarriage(d.Vehicle.Handle, 1) == 0 ? ((float)d.Index % 2 != 0) : ((float)d.Index % 2 == 0))).ToList();

                    // Open doors.
                    doors.ForEach((a) => SetVehicleDoorOpen(a.Vehicle.Handle, (int)a.Index, false, false));

                    // Remove doors.
                    doors.ForEach((a) => a.Break(false));

                    int stoppedTimer = GetGameTimer();
                    while (GetGameTimer() - stoppedTimer < (20 * 1000))
                    {
                        // keep doors that are on the wrong side of the platform closed while the train is stopped.
                        doorsToClose.ForEach(d => d.Close(true));
                        await Delay(0);
                    }

                    // Restore (fix and close) the doors by fixing the train.
                    SetVehicleFixed(train);
                    SetVehicleFixed(trainCarriage);

                }

                SetTrainCruiseSpeed(train, 8f);

                int timer = GetGameTimer();

                while (GetGameTimer() - timer < 10000)
                {
                    await Delay(0);

                    if (mainTrain != null && cariageTrain != null)
                    {
                        var doors = mainTrain.Doors.GetAll().ToList();
                        doors.AddRange(cariageTrain.Doors.GetAll().ToList());
                        doors.ForEach(d => d.Close(true));
                    }
                }

                stoppingTrains.Remove(train);

                while (!stoppingTrains.Contains(train))
                {
                    await Delay(0);

                    if (mainTrain != null && cariageTrain != null)
                    {
                        var doors = mainTrain.Doors.GetAll().ToList();
                        doors.AddRange(cariageTrain.Doors.GetAll().ToList());
                        doors.ForEach(d => d.Close(true));
                    }
                }
            }
        }

        /// <summary>
        /// sets or creates trains
        /// </summary>
        /// <param name="netHandles"></param>
        private async void SetTrain()
        {
            if (!justSetup)
            {
                foreach (string t in TrainModels)
                {
                    RequestModel((uint)GetHashKey(t));
                    while (!HasModelLoaded((uint)GetHashKey(t)))
                    {
                        await Delay(0);
                    }
                }

                if (NetworkIsHost())
                {
                    bool direction = new Random().Next(0, 1) == 1;

                    List<Vector3> coords = new List<Vector3>()
                    {
                        new Vector3(3007f, 4105f, 54f),
                        new Vector3(1010f, 3218f, 40f),
                        new Vector3(665f, -729f, 24f),
                        //new Vector3(-594f, -1391f, 21f),
                        //new Vector3(40.2f, -1201.3f, 31.0f),

                        new Vector3(-1081.309f, -2725.259f, -7.137033f),

                        new Vector3(-536.8082f, -1286.096f, 27.08768f),

                        new Vector3(-302.6719f, -322.9958f, 10.33629f),

                        new Vector3(-1341.085f, -467.674f, 15.31838f),

                        new Vector3(-209.6845f, -1037.544f, 30.50939f),
                    };

                    for (var i = 0; i < coords.Count; i++)
                    {
                        if ((TrainHandles.Count > i && !DoesEntityExist(TrainHandles[i])) || (!(TrainHandles.Count > i)))
                        {
                            if (i > 2)
                            {
                                CreateMissionTrain(24, coords[i].X, coords[i].Y, coords[i].Z, true);
                            }
                            else
                            {
                                CreateMissionTrain(new Random().Next(2, 15), coords[i].X, coords[i].Y, coords[i].Z, direction);
                            }
                            Debug.WriteLine($"Train {i} id: {TrainHandles[i]}");
                        }

                    }

                    foreach (int t in TrainHandles)
                    {
                        SetVehicleFixed(t);

                        var ped = GetPedInVehicleSeat(t, -1);

                        if (DoesEntityExist(ped))
                        {
                            // only delete the ped if it's not any player's ped to prevent game crashing.
                            if (!(Players.Any(p => p.Character.Handle == ped)))
                            {
                                DeletePed(ref ped);
                            }
                        }

                        var newped = CreatePedInsideVehicle(t, 26, (uint)GetHashKey("s_m_m_lsmetro_01"), -1, true, true);

                        SetPedDefaultComponentVariation(newped);
                        ClearPedDecorations(newped);
                        ClearAllPedProps(newped);
                        SetPedCanBeDraggedOut(newped, false);
                        SetPedCanBeShotInVehicle(newped, false);
                    }

                    TriggerServerEvent(GetCurrentResourceName() + ":SetTrainNetHandle");
                }

                if (CreateBlips)
                {
                    foreach (int TrainHandle in TrainHandles)
                    {
                        int blip = 0;

                        if (DoesEntityExist(TrainHandle) && !DoesBlipExist(GetBlipFromEntity(TrainHandle)))
                        {
                            blip = AddBlipForEntity(TrainHandle);
                        }
                        else
                        {
                            blip = GetBlipFromEntity(TrainHandle);
                        }

                        SetBlipAsShortRange(blip, true);
                        ShowHeadingIndicatorOnBlip(blip, true);
                        SetBlipColour(blip, 0);

                        BeginTextCommandSetBlipName("string");
                        AddTextComponentSubstringPlayerName("Train");
                        EndTextCommandSetBlipName(blip);
                    }
                }

                foreach (string t in TrainModels)
                {
                    SetModelAsNoLongerNeeded((uint)GetHashKey(t));
                }

                justSetup = true;
            }
            else
            {
                justSetup = false;
            }
        }
    }
}
