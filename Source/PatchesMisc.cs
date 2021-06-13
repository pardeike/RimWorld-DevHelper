using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Steam;

namespace DevHelper
{
	public class PatchesMisc
	{
		// skip autosave
		[HarmonyPatch(typeof(Autosaver), nameof(Autosaver.DoAutosave))]
		public class Autosaver_DoAutosave_Patch
		{
			public static bool Prefix()
			{
				return false;
			}
		}

		// 2% world coverage as first choice
		[HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.Reset))]
		public class Page_CreateWorldParams_Reset_Patch
		{
			public static void Postfix(ref float ___planetCoverage, ref string ___seedString)
			{
				___planetCoverage = 0.02f;
				___seedString = Helper.Settings.fixedSeed;
			}
		}

		// 2% world coverage as first choice
		[HarmonyPatch(typeof(WorldCameraDriver), nameof(WorldCameraDriver.ResetAltitude))]
		public class WorldCameraDriver_ResetAltitude_Patch
		{
			public static void Postfix(ref float ___altitude, ref float ___desiredAltitude)
			{
				___altitude = 125f;
				___desiredAltitude = 125f;
			}
		}

		// suppress "Near 4 tiles. Settle here anyway" message
		[HarmonyPatch(typeof(SettlementProximityGoodwillUtility), nameof(SettlementProximityGoodwillUtility.CheckConfirmSettle))]
		public class SettlementProximityGoodwillUtility_CheckConfirmSettle_Patch
		{
			public static bool Prefix(Action settleAction)
			{
				settleAction();
				return false;
			}
		}

		// preselect starting tile
		[HarmonyPatch(typeof(Page_SelectStartingSite), nameof(Page_SelectStartingSite.PostOpen))]
		public class Page_SelectStartingSite_PostOpen_Patch
		{
			public static void Postfix()
			{
				Find.WorldInterface.SelectedTile = 530;
			}
		}

		// suppress game start dialog
		[HarmonyPatch(typeof(ScenPart_GameStartDialog), nameof(ScenPart_GameStartDialog.PostGameStart))]
		public class ScenPart_GameStartDialog_PostGameStart_Patch
		{
			public static bool Prefix()
			{
				Find.MusicManagerPlay.disabled = true;
				Find.WindowStack.Notify_GameStartDialogOpened();
				LongEventHandler.ExecuteWhenFinished(() =>
				{
					Find.MusicManagerPlay.disabled = false;
					Find.WindowStack.Notify_GameStartDialogClosed();
					Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
					TutorSystem.Notify_Event("GameStartDialogClosed");
				});
				return false;
			}
		}

		// suppress no-steam dialog
		[HarmonyPatch(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init))]
		public class UIRoot_Entry_Init_Patch
		{
			public static bool TrueReturner()
			{
				return true;
			}

			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var from = AccessTools.PropertyGetter(typeof(SteamManager), nameof(SteamManager.Initialized));
				var to = SymbolExtensions.GetMethodInfo(() => TrueReturner());
				return Transpilers.MethodReplacer(instructions, from, to);
			}
		}
	}
}
