﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using System.Windows.Forms;
using StoryMode;

namespace CustomSpawns
{
    public class Main : MBSubModuleBase
    {
        public static readonly string version = "v1.1.8";
        public static readonly bool isAPIMode = true;
        public static CustomSpawnsCustomSpeedModel customSpeedModel;

        private static bool removalMode = false;

        protected override void OnSubModuleLoad()
        {
            Config config = ConfigLoader.Instance.Config;
            ModIntegration.SubModManager.LoadAllValidDependentMods();
            if (config.IsRemovalMode)
            {
                removalMode = true;
                return;
            }
            removalMode = false;
            customSpeedModel = new CustomSpawnsCustomSpeedModel();
        }

        public override void OnCampaignStart(Game game, object starterObject)
        {
            base.OnCampaignStart(game, starterObject);
            if (!(game.GameType is Campaign))
                return;
            try
            {
                InitializeGame(game, (IGameStarter)starterObject);
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(e);
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (!(game.GameType is Campaign) || ((StoryMode.CampaignStoryMode)game.GameType).CampaignGameLoadingType == Campaign.GameLoadingType.NewCampaign)
                return;
            InitializeGame(game, gameStarterObject);
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot() //assure player :) also myself lol
        {
            UX.ShowMessage("CustomSpawns " + version + " is now enabled. Enjoy! :)", Color.ConvertStringToColor("#001FFFFF"));
            AI.AIManager.FlushRegisteredBehaviours(); //forget old behaviours to allocate space. 
            foreach (var subMod in ModIntegration.SubModManager.dependentModsArray)
            {
                UX.ShowMessage( subMod.SubModuleName + " is now integrated into the CustomSpawns API.", Color.ConvertStringToColor("#001FFFFF"));
            }
        }

        private void InitializeGame(Game game, IGameStarter gameStarterObject)
        {
            try
            {
                ClearLastInstances();
                AddBehaviours(gameStarterObject as CampaignGameStarter);
                //do overrides
                if (ConfigLoader.Instance.Config.ModifyPartySpeeds && !removalMode)
                    gameStarterObject.AddModel(customSpeedModel);
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(e);
            }
        }

        public static UtilityBehaviours.OnSaveStartRunBehaviour currentOnSaveStartRunBehaviour;

        private void ClearLastInstances()
        {
            Data.DiplomacyDataManager.ClearInstance(this);
            Data.SpawnDataManager.ClearInstance(this);
            Data.NameSignifierData.ClearInstance(this);
        }

        private void AddBehaviours(CampaignGameStarter starter)
        {
            if (!removalMode)
            {
                starter.AddBehavior(new Spawn.DailySpawnBehaviour(Data.SpawnDataManager.Instance));
                starter.AddBehavior(new AI.HourlyPatrolAroundSpawnBehaviour());
                starter.AddBehavior(new AI.AttackClosestIfIdleForADayBehaviour());
                starter.AddBehavior(new AI.PatrolAroundClosestLestInterruptedAndSwitchBehaviour());
                starter.AddBehavior(new Diplomacy.ForcedWarPeaceBehaviour());
                starter.AddBehavior(new Diplomacy.ForceNoKingdomBehaviour());
                starter.AddBehavior(new Economics.SimpleAllSpawnNotStarveBehaviour()); //TODO for now we shall have to use this.
                starter.AddBehavior(new PrisonerRecruitment.PrisonerRecruitmentBehaviour());
                currentOnSaveStartRunBehaviour = new UtilityBehaviours.OnSaveStartRunBehaviour();
                starter.AddBehavior(currentOnSaveStartRunBehaviour);
                currentOnSaveStartRunBehaviour.RegisterFunctionToRun(OnSaveStart);
            }
            else
            {
                starter.AddBehavior(new Utils.RemoverBehaviour());
            }
        }

        private void OnSaveStart()
        {
            //restore lost AI behaviours!
            try
            {
                var partyIDToData = Data.SpawnDataManager.Instance.PartyIDToData;
                foreach (MobileParty mb in MobileParty.All)
                {
                    string id = CampaignUtils.IsolateMobilePartyStringID(mb);
                    if(id != "" && partyIDToData.ContainsKey(id))
                    {
                        var spawnData = partyIDToData[id];
                        Spawn.Spawner.HandleAIChecks(mb, spawnData, mb.HomeSettlement);
                    }

                }
            }catch(Exception e)
            {
                ErrorHandler.HandleException(e, " reconstruction of save custom spawns mobile party data");
            }
        }

    }
}
