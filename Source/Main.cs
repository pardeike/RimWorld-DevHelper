using HarmonyLib;
using UnityEngine;
using Verse;

namespace DevHelper
{
	public class Helper : Mod
	{
		public static DevHelperSettings Settings;

		public Helper(ModContentPack content) : base(content)
		{
			Settings = GetSettings<DevHelperSettings>();
			Tools.SetUsefulDefaults();

			var harmony = new Harmony("net.pardeike.rimworld.mod.devhelper");
			harmony.PatchAll();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Settings.DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "DevHelper";
		}
	}
}