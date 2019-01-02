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
        private bool hasSpawned = false;
        private bool justSetup = false;

        private List<int> TrainHandles { get; set; } = new List<int>() { 0, 0, 0, 0 };
        //private List<int> TrainNetHandles { get; set; } = new List<int>();
        //private int[] TrainNetHandles { get; set; } = new int[3] { 0, 0, 0 };
        //private int[] TrainHandles { get; set; } = new int[3] { 0, 0, 0 };

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
            EventHandlers.Add("playerSpawned", new Action(Spawn));
            Spawn();
            EventHandlers.Add(GetCurrentResourceName() + ":GetTrainNetworkHandle", new Action<List<dynamic>>(SetTrain));
            Tick += ManageTrainStops;
        }

        List<int> stoppingTrains = new List<int>();

        private async Task ManageTrainStops()
        {
            //SetRandomTrains(true);
            foreach (var train in TrainHandles)
            {
                if (DoesEntityExist(train))
                {
                    if (IsVehicleModel(train, (uint)GetHashKey("METROTRAIN")))
                    {
                        //if (GetEntitySpeed(train) < 1f)
                        //{
                        //    // Get all doors from main train and it's carriage.
                        //    var doors = ((Vehicle)Vehicle.FromHandle(train)).Doors.GetAll().ToList();
                        //    doors.AddRange(((Vehicle)Vehicle.FromHandle(GetTrainCarriage(train, 1))).Doors.GetAll().ToList());

                        //    if (doors.Any(a => !a.IsFullyOpen))
                        //    {
                        //        // Open doors.
                        //        doors.ForEach((a) => a.Open(false, false));
                        //        while (!doors.All((a) => a.IsFullyOpen))
                        //        {
                        //            await Delay(0);
                        //        }

                        //        // Remove doors.
                        //        doors.ForEach((a) => a.Break(false));
                        //    }

                        //}
                        //else
                        //{
                        //    // Get all doors from main train and it's carriage.
                        //    var doors = ((Vehicle)Vehicle.FromHandle(train)).Doors.GetAll().ToList();
                        //    doors.AddRange(((Vehicle)Vehicle.FromHandle(GetTrainCarriage(train, 1))).Doors.GetAll().ToList());

                        //    if (doors.Any(a => a.IsOpen))
                        //    {
                        //        // Restore the doors by fixing the train.
                        //        SetVehicleFixed(train);
                        //        SetVehicleFixed(GetTrainCarriage(train, 1));

                        //        // Set the doors back open (instantly)
                        //        doors.ForEach(a => a.Open(false, true));

                        //        // Close the doors (normal animation speed).
                        //        doors.ForEach(a => a.Close(false));

                        //        // Wait for the doors to be closed.
                        //        while (doors.Any(a => a.IsOpen))
                        //        {
                        //            await Delay(0);
                        //        }
                        //    }
                        //}


                        Vector3 pos = GetEntityCoords(train, false);
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
            await Task.FromResult(0);
        }

        private async void StopTrain(int train)
        {
            stoppingTrains.Add(train);
            SetTrainCruiseSpeed(train, 0f);

            // Get all doors from main train and it's carriage.
            var doors = ((Vehicle)Vehicle.FromHandle(train)).Doors.GetAll().ToList();
            doors.AddRange(((Vehicle)Vehicle.FromHandle(GetTrainCarriage(train, 1))).Doors.GetAll().ToList());

            if (doors.Any(a => !a.IsFullyOpen))
            {
                // Open doors.
                doors.ForEach((a) => a.Open(false, false));
                while (!doors.All((a) => a.IsFullyOpen))
                {
                    await Delay(0);
                }

                // Remove doors.
                doors.ForEach((a) => a.Break(false));
            }

            await Delay(20000);

            // Restore the doors by fixing the train.
            SetVehicleFixed(train);
            SetVehicleFixed(GetTrainCarriage(train, 1));

            // Set the doors back open (instantly)
            doors.ForEach(a => a.Open(false, true));

            // Close the doors (normal animation speed).
            doors.ForEach(a => a.Close(false));

            // Wait for the doors to be closed.
            while (doors.Any(a => a.IsOpen))
            {
                await Delay(0);
            }

            SetTrainCruiseSpeed(train, 8f);

            await Delay(5000);

            stoppingTrains.Remove(train);
        }

        /// <summary>
        /// sets or creates trains
        /// </summary>
        /// <param name="netHandles"></param>
        private async void SetTrain(List<dynamic> netHandles)
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

                foreach (var nh in netHandles)
                {
                    if (NetworkDoesNetworkIdExist((int)nh))
                    {
                        TrainHandles.Add(NetToVeh((int)nh));
                    }
                }


                //{
                List<int> vehicles = new List<int>();
                int entity = 0;
                int handle = FindFirstVehicle(ref entity);
                if (DoesEntityExist(entity))
                {
                    vehicles.Add(entity);
                    while (true)
                    {
                        if (FindNextVehicle(handle, ref entity))
                        {
                            if (DoesEntityExist(entity))
                            {
                                vehicles.Add(entity);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                EndFindVehicle(handle);

                List<int> existingTrains = new List<int>();
                foreach (int veh in vehicles)
                {
                    if (IsThisModelATrain((uint)GetEntityModel(veh)) && (IsVehicleModel(veh, (uint)GetHashKey("FREIGHT")) || IsVehicleModel(veh, (uint)GetHashKey("METROTRAIN"))))
                    {
                        //if (existingTrains.Count < 3)
                        //{
                        existingTrains.Add(veh);
                        //}
                        //else
                        //{
                        //    int ripTrain = veh;
                        //    DeleteMissionTrain(ref ripTrain);
                        //}
                    }
                }
                int count = TrainHandles.Where(t => DoesEntityExist(t)).Count();

                if (count < existingTrains.Count)
                {
                    TrainHandles = existingTrains;
                    //int i = 0;
                    //foreach (int train in existingTrains)
                    //{
                    //    Debug.WriteLine(i.ToString());
                    //    TrainHandles[i] = train;
                    //    i++;
                    //}
                }

                //speedv
                //SetRandomTrains(false);
                //DeleteAllTrains();
                if (NetworkIsHost())
                {
                    bool direction = new Random().Next(0, 1) == 1;

                    List<Vector3> coords = new List<Vector3>()
                    {
                        new Vector3(3007f, 4105f, 54f),
                        new Vector3(1010f, 3218f, 40f),
                        new Vector3(665f, -729f, 24f),
                        //new Vector3(-594f, -1391f, 21f),
                        new Vector3(40.2f, -1201.3f, 31.0f),
                    };

                    for (var i = 0; i < 4; i++)
                    {
                        if ((TrainHandles.Count > i && !DoesEntityExist(TrainHandles[i])) || (TrainHandles.Count <= i))
                        {
                            if (i == 3)
                            {
                                TrainHandles[i] = CreateMissionTrain(24, coords[i].X, coords[i].Y, coords[i].Z, true);
                            }
                            else
                            {
                                TrainHandles[i] = CreateMissionTrain(new Random().Next(2, 15), coords[i].X, coords[i].Y, coords[i].Z, direction);
                            }
                            Debug.WriteLine($"Train {i} id: {TrainHandles[i]}");
                        }

                        //N_0x2b6747faa9db9d6b(TrainHandles[i], true);
                        var ped = GetPedInVehicleSeat(TrainHandles[i], -1);
                        if (DoesEntityExist(ped))
                        {
                            DeletePed(ref ped);
                        }
                        var newped = CreatePedInsideVehicle(TrainHandles[i], 26, (uint)GetHashKey("s_m_m_lsmetro_01"), -1, true, true);
                        //if (IsVehicleModel(TrainHandles[i], (uint)GetHashKey("METROTRAIN")))
                        //{
                        //    TaskVehicleDriveWander(newped, TrainHandles[i], 8f, 786603);
                        //}
                        //SetVehicleSt(TrainHandles[i], true);
                        SetVehicleFixed(TrainHandles[i]);
                        //CitizenFX.Core.Native.Function.Call((CitizenFX.Core.Native.Hash)0xE861D0B05C7662B8, newped, 1, 1);
                        SetPedDefaultComponentVariation(newped);
                        ClearPedDecorations(newped);




                        //N_0xe861d0b05c7662b8(newped, 0, 0);
                        //}
                    }

                    List<int> trainNetHandles = new List<int>();
                    foreach (int tHandle in TrainHandles)
                    {
                        trainNetHandles.Add(VehToNet(tHandle));
                    }
                    TriggerServerEvent(GetCurrentResourceName() + ":SetTrainNetHandle", trainNetHandles);
                }
                //}

                foreach (int TrainHandle in TrainHandles)
                {
                    if (DoesEntityExist(TrainHandle) && GetEntitySpeedVector(TrainHandle, true).Y > -1f)
                    {
                        int blip = AddBlipForEntity(TrainHandle);

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

        private void Spawn()
        {
            if (!hasSpawned)
            {
                hasSpawned = true;

                TriggerServerEvent(GetCurrentResourceName() + ":RequestTrainNetworkHandle");
            }
        }
    }
}
