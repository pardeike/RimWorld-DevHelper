using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace DevHelper
{
	// recall log window size/pos
	[HarmonyPatch(typeof(Window), "SetInitialSizeAndPosition")]
	public class Window_SetInitialSizeAndPosition_Patch
	{
		public static void Postfix(Window __instance, ref Rect ___windowRect)
		{
			var info = __instance.GetPositionInfo();
			if (info == null) return;

			if (info.Rect != default)
				___windowRect = info.Rect;
		}
	}

	// update initial log window
	[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.Add))]
	public class WindowStack_Add_Patch
	{
		public static void Postfix(Window window)
		{
			var info = window.GetPositionInfo();
			if (info == null) return;

			info.visible = true;
			Helper.Settings.Write();
		}
	}

	// update log window during use
	[HarmonyPatch(typeof(Window), nameof(Window.WindowOnGUI))]
	public class Window_WindowOnGUI_Patch
	{
		public static void Prefix(Window __instance, out Rect __state)
		{
			__state = __instance.windowRect;
		}

		public static void Postfix(Window __instance, Rect __state)
		{
			var rect = __instance.windowRect;
			if (rect == __state || rect == default) return;

			var info = __instance.GetPositionInfo();
			if (info == null) return;

			info.Rect = rect;
			Helper.Settings.Write();
		}
	}

	// update log window when closed
	[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.TryRemove))]
	[HarmonyPatch(new[] { typeof(Window), typeof(bool) })]
	public class WindowStack_TryRemove_Patch
	{
		public static void Prefix(Window window)
		{
			var info = window.GetPositionInfo();
			if (info == null) return;
			info.visible = false;
			Helper.Settings.Write();
		}
	}

	// show last open windows
	[HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.Init))]
	public class MainMenuDrawer_Init_Patch
	{
		public static void Postfix()
		{
			Helper.Settings.windowState
				.DoIf(pair => pair.Value.visible, pair =>
				{
					var windowType = pair.Key;
					if (Find.WindowStack.TryRemove(windowType, true) == false)
						Find.WindowStack.Add(Activator.CreateInstance(windowType) as Window);
				});
		}
	}
}