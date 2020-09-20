using HarmonyLib;
using RimWorld;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Verse;

namespace DevHelper
{
	public static class Tools
	{
		public static bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		public static void SetUsefulDefaults()
		{
			Prefs.TestMapSizes = true;
			Prefs.ResetModsConfigOnCrash = false;
			Prefs.DevMode = true;
			try { File.Delete(GenFilePaths.DevModePermanentlyDisabledFilePath); } finally { }
			_ = Traverse.Create(typeof(DevModePermanentlyDisabledUtility)).Field("disabled").SetValue(false);
		}

		public static void ResizeApplication(int x, int y, int resX = 0, int resY = 0)
		{
			var hwnd = NativeMethods.FindWindow(null, "RimWorld by Ludeon Studios");
			if (hwnd != IntPtr.Zero)
				_ = NativeMethods.SetWindowPos(hwnd, 0, x, y, resX, resY, resX * resY == 0 ? 1 : 0);
		}
	}
}