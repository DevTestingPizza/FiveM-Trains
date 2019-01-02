using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace TrainsServer
{
    public class Main : BaseScript
    {
        private List<int> TrainNetworkHandles { get; set; } = new List<int>() { 0, 0, 0, 0 };

        /// <summary>
        /// Constructor, adds 2 event handlers.
        /// </summary>
        public Main()
        {
            EventHandlers.Add(GetCurrentResourceName() + ":SetTrainNetHandle", new Action<Player, List<dynamic>>(SetTrain));
            EventHandlers.Add(GetCurrentResourceName() + ":RequestTrainNetworkHandle", new Action<Player>(GetTrain));
        }

        /// <summary>
        /// Set the new train network handle if the client is actually the host of the session.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="netHandle"></param>
        private void SetTrain([FromSource]Player source, List<dynamic> handles)
        {
            if (GetHostId() == source.Handle)
            {
                TrainNetworkHandles.Clear();
                foreach (var h in handles)
                {
                    TrainNetworkHandles.Add((int)h);
                }
                //TrainNetworkHandle1 = netHandle1;
                //TrainNetworkHandle2 = netHandle2;
                //TrainNetworkHandle3 = netHandle3;
                TriggerClientEvent(GetCurrentResourceName() + ":GetTrainNetworkHandle", TrainNetworkHandles);
                //Debug.WriteLine($"Setting the train handle received from {source.Name}^7 to: {TrainNetworkHandle1}, {TrainNetworkHandle2}, {TrainNetworkHandle3}");
            }
        }

        /// <summary>
        /// Gets the train network handle.
        /// </summary>
        /// <param name="source"></param>
        private void GetTrain([FromSource] Player source)
        {
            //Debug.WriteLine($"Sending {source.Name}^7 the train handle: {TrainNetworkHandle1}, {TrainNetworkHandle2}, {TrainNetworkHandle3}");
            source.TriggerEvent(GetCurrentResourceName() + ":GetTrainNetworkHandle", TrainNetworkHandles);
        }
    }
}
