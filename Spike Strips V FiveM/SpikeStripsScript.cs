using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core.Native;

namespace Spike_Strips_V
{
    internal class SpikeStripsScript : BaseScript
    {
        public readonly List<SpikeStrip> SpikesStrips = new List<SpikeStrip>();
        public readonly List<Vehicle> PlayersVehicles = new List<Vehicle>();
        private DateTime lastPlayersVehiclesUpdateTime = DateTime.UtcNow;
        PoliceManagement pm = new PoliceManagement();

        private bool CanDeploySpikeStrips
        {
            get
            {
                Ped playerPed = LocalPlayer.Character;
                if (!pm.isCop)
                {
                    Screen.ShowNotification("~r~You are not a police officer!", true);
                    return false;
                }
                return !playerPed.IsDead && !playerPed.IsInVehicle() && !playerPed.IsInCover() && !playerPed.IsJumping && !playerPed.IsAiming && !playerPed.IsClimbing;
            }
        }

        private bool CanRemoveSpikeStrips
        {
            get
            {
                Ped playerPed = LocalPlayer.Character;
                if (!pm.isCop)
                {
                    Screen.ShowNotification("~r~You are not a police officer!", true);
                    return false;
                }
                return !playerPed.IsDead && !playerPed.IsInVehicle() && !playerPed.IsInCover() && !playerPed.IsJumping && !playerPed.IsAiming && !playerPed.IsClimbing;
            }
        }

        public SpikeStripsScript()
        {
            while (!NetworkIsPlayerConnected(PlayerId()))
            {
                Delay(1);
            }
            Tick += OnTick;
            PolM("spike");
        }

        private void PolM(string pluginName)
        {
            pm.Checks(pluginName);
            EventHandlers.Add(pluginName + ":isCop", new Action(pm.IsCop));
            EventHandlers.Add(pluginName + ":isNotCop", new Action(pm.IsNotCop));
            EventHandlers.Add(pluginName + ":isAdmin", new Action(pm.IsAdmin));
            EventHandlers.Add(pluginName + ":isNotAdmin", new Action(pm.IsNotAdmin));
        }        

        private async Task OnTick()
        {
            if (Game.IsControlPressed(0, Control.Context))
            {
                if (Game.IsControlJustPressed(0, Control.SelectWeaponMelee))
                {
                    if (CanDeploySpikeStrips)
                        CreateSpikeStrips();
                }
                else if (Game.IsControlJustPressed(0, Control.SelectWeaponUnarmed))
                {
                    if (CanDeploySpikeStrips)
                        CreateSSpikeStrips();
                }
                else if (Game.IsControlJustPressed(0, Control.SelectWeaponShotgun))
                {
                    if(CanRemoveSpikeStrips)
                        DeleteAllSpikeStrips();
                }
            }
            if (SpikesStrips.Count > 0 && (DateTime.UtcNow - lastPlayersVehiclesUpdateTime).TotalSeconds > 1.0)
            {
                PlayersVehicles.Clear();
                foreach (Player player in Players)
                {
                    Ped ped = player.Character;
                    if (ped.IsInVehicle())
                    {
                        PlayersVehicles.Add(ped.CurrentVehicle);
                    }
                }
                lastPlayersVehiclesUpdateTime = DateTime.UtcNow;
            }
            for (int i = 0; i < SpikesStrips.Count; i++)
            {
                SpikesStrips[i].OnTick(PlayersVehicles);
            }
            await Task.FromResult(0);
        }

        private async void CreateSSpikeStrips()
        {
            Ped playerPed = LocalPlayer.Character;
            Vector3 playerPos = playerPed.Position;
            Vector3 playerForwardVector = playerPed.ForwardVector;
            float playerHeading = playerPed.Heading;
            const int SpikeStripsToSpawn = 2;
            for (int i = 0; i < SpikeStripsToSpawn; i++)
            {
                const float SeparationFromPlayer = 2f;
                const float SeparationBetweenStingers = 3.7f;
                SpikeStrip s = await SpikeStrip.Create(playerPos + playerForwardVector * (SeparationFromPlayer + (SeparationBetweenStingers * i)), playerHeading);
                SpikesStrips.Add(s);
            }
            Screen.ShowNotification("~g~Deployed 1 spike strip!", true);
        }

        private async void CreateSpikeStrips()
        {
            Ped playerPed = LocalPlayer.Character;
            Vector3 playerPos = playerPed.Position;
            Vector3 playerForwardVector = playerPed.ForwardVector;
            float playerHeading = playerPed.Heading;
            const int SpikeStripsToSpawn = 4;
            for (int i = 0; i < SpikeStripsToSpawn; i++)
            {
                const float SeparationFromPlayer = 2f;
                const float SeparationBetweenStingers = 3.7f;

                SpikeStrip s = await SpikeStrip.Create(playerPos + playerForwardVector * (SeparationFromPlayer + (SeparationBetweenStingers * i)), playerHeading);
                SpikesStrips.Add(s);
            }
            Screen.ShowNotification("~g~Deployed 2 spike strips!", true);
        }

        private void DeleteAllSpikeStrips()
        {
            if (SpikesStrips.Count != 0)
            {
                for (int i = 0; i < SpikesStrips.Count; i++)
                {
                    SpikesStrips[i].Prop.Delete();
                }
                SpikesStrips.Clear();
                Screen.ShowNotification("~r~Removed spike strips!", true);
            }
            else
                Screen.ShowNotification("~r~No spike strips deployed!", true);
        }

        public class SpikeStrip
        {
            private static readonly Model Model = "p_ld_stinger_s";
            public Prop Prop { get; private set; }
            private SpikeStrip()
            {
            }
            [System.Security.SecuritySafeCritical]
            public void OnTick(List<Vehicle> playerVehicles = null)
            {
                if (Entity.Exists(Prop))
                {
                    Vector3 pointA = Prop.GetOffsetPosition(new Vector3(0.0f, 1.7825f, 0.034225f));
                    Vector3 pointB = Prop.GetOffsetPosition(new Vector3(0.0f, -1.7825f, 0.034225f));
                    Vector3 propPos = Prop.Position;
                    if (playerVehicles != null)
                    {
                        for (int i = 0; i < playerVehicles.Count; i++)
                        {
                            Vehicle veh = playerVehicles[i];
                            if (Vector3.DistanceSquared(veh.Position, propPos) < 7.25f * 7.25f)
                            {
                                BurstVehicleTyres(veh, pointA, pointB);
                            }
                        }
                    }
                    Vehicle closestVeh = new Vehicle(GetClosestVehicle(propPos.X, propPos.Y, propPos.Z, 10.0f, 0, 70));
                    BurstVehicleTyres(closestVeh, pointA, pointB);
                }
            }

            private void BurstVehicleTyres(Vehicle v, Vector3 pointA, Vector3 pointB)
            {
                if (Entity.Exists(v))
                {
                    OutputArgument outModelMin = new OutputArgument(), outModelMax = new OutputArgument();
                    Function.Call(Hash.GET_MODEL_DIMENSIONS, v.Model.Hash, outModelMin, outModelMax);
                    Vector3 modelMin = outModelMin.GetResult<Vector3>();
                    BurstTyre(v, "wheel_lf", 0, modelMin, pointA, pointB);
                    BurstTyre(v, "wheel_rf", 1, modelMin, pointA, pointB);
                    BurstTyre(v, "wheel_lm1", 2, modelMin, pointA, pointB);
                    BurstTyre(v, "wheel_rm1", 3, modelMin, pointA, pointB);
                    BurstTyre(v, "wheel_lr", 4, modelMin, pointA, pointB);
                    BurstTyre(v, "wheel_rr", 5, modelMin, pointA, pointB);
                }
            }

            private bool CanBurstTyre(Vehicle veh, string wheelBone, int wheelIndex)
            {
                return Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, veh.Handle, wheelBone) != -1 && !Function.Call<bool>(Hash.IS_VEHICLE_TYRE_BURST, veh.Handle, wheelIndex, false);
            }

            private void BurstTyre(Vehicle veh, string wheelBone, int wheelIndex, Vector3 vehModelMinDim, Vector3 pointA, Vector3 pointB)
            {
                if (CanBurstTyre(veh, wheelBone, wheelIndex))
                {
                    Vector3 wheelPos = Vector3.Zero;
                    wheelPos = veh.Bones[wheelBone].Position;
                    wheelPos.Z += (vehModelMinDim.Z / 2);
                    Vector3 wheelClosestPoint = GetClosestPointOnLineSegment(pointA, pointB, wheelPos);
                    float wheelClosestPointSqrDistance = Vector3.DistanceSquared(wheelPos, wheelClosestPoint);
                    if (wheelClosestPointSqrDistance < 0.275f * 0.275f)
                    {
                        SetVehicleTyreBurst(veh.Handle, wheelIndex, false, 940f);
                    }
                }
            }

            private static Vector3 GetClosestPointOnLineSegment(Vector3 linePointStart, Vector3 linePointEnd, Vector3 testPoint)
            {
                Vector3 lineDiffVect = linePointEnd - linePointStart;
                float lineSegSqrLength = lineDiffVect.LengthSquared();
                Vector3 lineToPointVect = testPoint - linePointStart;
                float dotProduct = Vector3.Dot(lineDiffVect, lineToPointVect);
                float percAlongLine = dotProduct / lineSegSqrLength;
                if (percAlongLine < 0.0f)
                    return linePointStart;
                else if (percAlongLine > 1.0f)
                    return linePointEnd;
                return (linePointStart + (percAlongLine * (linePointEnd - linePointStart)));
            }

            public static async Task<SpikeStrip> Create(Vector3 position, float heading)
            {
                SpikeStrip s = new SpikeStrip
                {
                    Prop = await World.CreateProp(Model, position, true, true)
                };
                s.Prop.Heading = heading;
                return s;
            }
        }
    }
}
