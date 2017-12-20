using System;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Spike_Strips_V
{
    public class PoliceManagement : BaseScript
    {
        public bool isCop = false;
        public bool isAdmin = false;
        public string pluginName;

        public async void Checks(string pluginAcronym)
        {
            pluginName = pluginAcronym;
            EventHandlers.Add(pluginName + ":isCop", new Action(IsCop));
            EventHandlers.Add(pluginName + ":isNotCop", new Action(IsNotCop));
            EventHandlers.Add(pluginName + ":isAdmin", new Action(IsAdmin));
            EventHandlers.Add(pluginName + ":isNotAdmin", new Action(IsNotAdmin));
            while (true)
            {
                await Delay(1000);
                TriggerServerEvent("pm:isCop", GetPlayerServerId(PlayerId()), pluginName+":isCop", pluginName+":isNotCop");
                TriggerServerEvent("pm:isAdmin", GetPlayerServerId(PlayerId()), pluginName + ":isAdmin", pluginName + ":isNotAdmin");
            }
        }

        public void IsCop()
        {
            isCop = true;
        }

        public void IsNotCop()
        {
            isCop = false;
        }

        public void IsAdmin()
        {
            isAdmin = true;
        }

        public void IsNotAdmin()
        {
            isAdmin = false;
        }
    }
}
