using HarmonyLib;
using RimWorld;
using System.IO;
using Verse;

namespace DevHelper
{
	public static class Tools
	{
		public static void SetUsefulDefaults()
		{
			Prefs.TestMapSizes = true;
			Prefs.ResetModsConfigOnCrash = false;
			Prefs.DevMode = true;
			try { File.Delete(GenFilePaths.DevModePermanentlyDisabledFilePath); } finally { }
			_ = Traverse.Create(typeof(DevModePermanentlyDisabledUtility)).Field("disabled").SetValue(false);
		}
	}
}