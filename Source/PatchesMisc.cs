using HarmonyLib;
using RimWorld;
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