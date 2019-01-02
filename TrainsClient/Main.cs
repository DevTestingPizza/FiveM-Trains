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
            "METROTRAIN",
            "TANKERCAR"
        };

        /// <summary>
        /// Constructor
        /// </summary>
        public Main()
        {
            //EventHandlers.Add("playerSpawned", new Action(Spawn));
            Spawn();
            EventHandlers.Add(GetCurrentResourceName() + ":GetTrainNetworkHandle", new Action<List<dynamic>>(SetTrain));
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
                //if (NetworkDoesNetworkIdExist(netHandle1))
                //{
                //    TrainNetHandles[0] = netHandle1;
                //    TrainHandles[0] = NetToVeh(netHandle1);
                //}
                //if (NetworkDoesNetworkIdExist(netHandle2))
                //{
                //    TrainNetHandles[1] = netHandle2;
                //    TrainHandles[1] = NetToVeh(netHandle2);
                //}
                //if (NetworkDoesNetworkIdExist(netHandle3))
                //{
                //    TrainNetHandles[2] = netHandle3;
                //    TrainHandles[2] = NetToVeh(netHandle3);
                //}


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
                if (NetworkIsHost())
                {
                    bool direction = new Random().Next(0, 1) == 1;

                    List<Vector3> coords = new List<Vector3>()
                    {
                        new Vector3(3007f, 4105f, 54f),
                        new Vector3(1010f, 3218f, 40f),
                        new Vector3(665f, -729f, 24f),
                        new Vector3(-594f, -1391f, 21f),
                    };

                    for (var i = 0; i < 4; i++)
                    {
                        if (!DoesEntityExist(TrainHandles[i]))
                        {
                            if (i == 3)
                            {
                                TrainHandles[i] = CreateMissionTrain(24, coords[i].X, coords[i].Y, coords[i].Z, direction);
                            }
                            else
                            {
                                TrainHandles[i] = CreateMissionTrain(new Random().Next(2, 15), coords[i].X, coords[i].Y, coords[i].Z, direction);
                            }

                        }
                    }

                    //if (!DoesEntityExist(TrainHandles[0]))
                    //{
                    //    TrainHandles[0] = CreateMissionTrain(new Random().Next(2, 15), 3007, 4105, 54, direction);
                    //}
                    //if (!DoesEntityExist(TrainHandles[1]))
                    //{
                    //    TrainHandles[1] = CreateMissionTrain(new Random().Next(2, 15), 1010f, 3218f, 40f, direction);
                    //}
                    //if (!DoesEntityExist(TrainHandles[2]))
                    //{
                    //    TrainHandles[2] = CreateMissionTrain(new Random().Next(2, 15), 665f, -729f, 24f, direction);
                    //}
                    //for (var ii = 0; ii < TrainHandles.Length; ii++)
                    //{
                    //    TrainNetHandles[ii] = VehToNet(TrainHandles[ii]);
                    //}
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
