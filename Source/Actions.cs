using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Profile;

namespace DevHelper
{
	public static class Actions
	{
		[DebugAction("dev.helper", null, allowedGameStates = AllowedGameStates.Entry)]
		public static void GenerateEmptyMap()
		{
			LongEventHandler.QueueLongEvent(delegate ()
			{
				var game = new Game
				{
					InitData = new GameInitData() { mapSize = 75, permadeath = false },
					Scenario = ScenarioDefOf.Crashlanded.scenario,
					storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough)
				};
				Find.Scenario.PreConfigure();
				Current.Game = game;

				Find.GameInitData.PrepForMapGen();
				Find.GameInitData.startedFromEntry = true;
				Find.Scenario.PreMapGenerate();

				Find.GameInitData.ResetWorldRelatedMapInitData();
				Current.Game.World = WorldGenerator.GenerateWorld(0.05f, GenText.RandomSeedString(), OverallRainfall.Normal, OverallTemperature.Normal, OverallPopulation.Normal);
				MemoryUtility.UnloadUnusedUnityAssets();
				Find.World.renderer.RegenerateAllLayersNow();

				MemoryUtility.UnloadUnusedUnityAssets();

				Current.ProgramState = ProgramState.MapInitializing;

				var mapSize = new IntVec3(game.InitData.mapSize, 1, game.InitData.mapSize);
				game.World.info.initialMapSize = mapSize;
				if (game.InitData.permadeath)
				{
					game.Info.permadeathMode = true;
					game.Info.permadeathModeUniqueName = PermadeathModeUtility.GeneratePermadeathSaveName();
				}

				game.tickManager.gameStartAbsTick = GenTicks.ConfiguredTicksAbsAtGameStart;

				_ = Ref.parts(Find.Scenario).RemoveAll(part => part is ScenPart_GameStartDialog);
				var arrivalMethod = Find.Scenario.AllParts.OfType<ScenPart_PlayerPawnsArriveMethod>().First();
				Ref.method(arrivalMethod) = PlayerPawnsArriveMethod.Standing;

				var tile = TileFinder.RandomStartingTile();

				Find.GameInitData.startingAndOptionalPawns.Clear();

				for (var i = 1; i <= 3; i++)
				{
					var pawn = StartingPawnUtility.NewGeneratedStartingPawn();
					pawn.playerSettings.hostilityResponse = HostilityResponseMode.Ignore;
					DefDatabase<SkillDef>.AllDefsListForReading.Do(skillDef => pawn.skills.GetSkill(skillDef).EnsureMinLevelWithMargin(1));
					Find.GameInitData.startingAndOptionalPawns.Add(pawn);
				}

				var settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
				settlement.SetFaction(Find.GameInitData.playerFaction);
				settlement.Tile = tile;
				settlement.Name = NameGenerator.GenerateName(Faction.OfPlayer.def.factionNameMaker);
				Find.WorldObjects.Add(settlement);

				Current.Game.CurrentMap = MapGenerator.GenerateMap(mapSize, settlement, settlement.MapGeneratorDef, settlement.ExtraGenStepDefs, null);
				PawnUtility.GiveAllStartingPlayerPawnsThought(ThoughtDefOf.NewColonyOptimism);

				game.FinalizeInit();
				game.playSettings.useWorkPriorities = true;

				Find.CameraDriver.JumpToCurrentMapLoc(MapGenerator.PlayerStartSpot);
				Find.CameraDriver.ResetSize();
				Find.Scenario.PostGameStart();

				foreach (var researchProjectDef in DefDatabase<ResearchProjectDef>.AllDefs)
					game.researchManager.FinishProject(researchProjectDef, false, null);

				GameComponentUtility.StartedNewGame();
				game.InitData = null;

			}, "GeneratingMap", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap), true);
			LongEventHandler.QueueLongEvent(delegate ()
			{
				ScreenFader.SetColor(Color.black);
				ScreenFader.StartFade(Color.clear, 0.5f);
			}, null, false, null, true);
		}
	}
}