using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ActiveShooters
{
    [CalloutProperties("Active Shooters", "HuskyNinja", "1.0")]
    internal class Active_Shooters : Callout{
        private WeaponHash[] weaponList = new WeaponHash[]{
            WeaponHash.AssaultRifle,
            WeaponHash.AssaultSMG,
            WeaponHash.CarbineRifle,
            WeaponHash.MicroSMG,
            WeaponHash.PumpShotgun,
            WeaponHash.SMG,
            WeaponHash.MarksmanRifle
        };

        private List<Vector3> locations = new List<Vector3>(){
            new Vector3(-1639.831f, -1108.659f, 13.02439f), //Del Perro Amusment Park
            new Vector3(-878.8595f, -858.1266f, 19.12326f), //Chinese Pagota
            new Vector3(1066.351f, -2447.885f, 29.06232f), //Bus Graveyard
            new Vector3(191.7502f, -946.9447f, 30.09192f), //Art Installation
            new Vector3(96.85612f, -356.3206f, 42.14159f) //Construction Site
        };

        private Vector3 calloutLocation;

        public Active_Shooters(){
            //Get a random location for the callout
            calloutLocation = locations.SelectRandom();
            
            //Try to generate the callout details. If some part of this fails end the callout.
            try{
                //Setup callout
                InitInfo(calloutLocation);
                ResponseCode = 3;
                StartDistance = 180f;
                ShortName = "Active Shooters";
                CalloutDescription = String.Format("Be Advised: Active Shooters reported in the {0} area.\n\t-- Estimated there to be between 2 and 5 armed subjects.\n\t-- Unknown number of casualties at this time.\n\t-- Reports suggest that shots by high powered weapons have been fired.\n\t-- Automatic weapons should be assumed.\n\t-- EMS/Fire standing by. No further information at this time.\n\t-- Proceed with extreme caution.", World.GetZoneLocalizedName(calloutLocation));
            }
            catch{
                EndCallout();
            }
        }

        public override async Task OnAccept(){
            try{
                InitBlip(50f);
                UpdateData();
            }
            catch{
                EndCallout();
            }
        }

        public override async void OnStart(Ped player){
            base.OnStart(player);

            Random rand = new Random();
            List<Ped> shooters = new List<Ped>();

            int shooterCount = rand.Next(2, 6);

            try{
                //Spawn in the shooters
                for(int i = 0; i < shooterCount; i++){
                    Ped shooter = await CreateShooter();
                    shooters.Add(shooter);

                    //20% chance the shooter will attack a random pedestrian
                    if(rand.Next(0,101) <= 20){
                        Ped nearestPed = Utilities.GetClosestPed(shooter);
                        shooter.Task.ShootAt(nearestPed);
                    }
                    //Or they will just wander around the area
                    else{
                        shooter.Task.WanderAround(calloutLocation, 8f);
                    }
                }

                //Make the shooters hate the player
                API.SetRelationshipBetweenGroups(5, 0x6A3B9F86/*GANG GROUP*/, 0x6F0783F5/*PLAYERS*/);
                API.SetRelationshipBetweenGroups(5, 0x6F0783F5/*PLAYERS*/, 0x6A3B9F86/*GANG GROUP*/);

                //Wait till the closest player nears the shooters
                while(World.GetDistance(player.Position,shooters.FirstOrDefault().Position) >= 55f) { await BaseScript.Delay(150); }

                //Play the callout dialogue when a player gets near the shooters
                PlayCalloutDialogue();

                //Make the shooters change targets to players
                foreach(Ped shooter in shooters){
                    shooter.Task.ClearAllImmediately();
                    shooter.Task.ShootAt(AssignedPlayers.SelectRandom());
                }
            } 
            catch{
                EndCallout();
            }
        }

        public override void OnCancelBefore(){
            base.OnCancelBefore();
        }
        private async Task<Ped> CreateShooter(){
            Ped shooter = await SpawnPed(RandomUtils.GetRandomPed(), calloutLocation.Around(8f));

            shooter.Weapons.Give(weaponList.SelectRandom(), 500, true, true);
            shooter.Armor = 2500;
            shooter.Accuracy = 18;
            shooter.AlwaysKeepTask = true;
            shooter.DropsWeaponsOnDeath = false;
            //This prevents the shooters from killing each other
            shooter.RelationshipGroup = (RelationshipGroup)"AMBIENT_GANG_WEICHENG";

            return shooter;
        }
        private void PlayCalloutDialogue(){

            string dialogue = "~r~Suspect~s~: The ~b~Cops~s~ are here! ~r~Shoot~s~ anyone you see!";
            ShowDialog(dialogue, 5000, 25f);
        }
    }
}
