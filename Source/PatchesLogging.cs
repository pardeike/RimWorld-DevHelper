using HarmonyLib;
using UnityEngine;
using Verse;

namespace DevHelper
{
	[HarmonyPatch(typeof(Log), nameof(Log.Warning))]
	public class Log_Warning_Patch
	{
		public static bool Prefix(string text)
		{
			Logging.Log(LogType.Warning, text);
			return false;
		}
	}

	[HarmonyPatch(typeof(Log), nameof(Log.Message))]
	public class Log_Message_Patch
	{
		public static bool Prefix(string text)
		{
			Logging.Log(LogType.Log, text);
			return false;
		}
	}

	[HarmonyPatch(typeof(Log), nameof(Log.Error))]
	public class Log_Error_Patch
	{
		public static bool Prefix(string text)
		{
			Logging.Log(LogType.Error, text);
			return false;
		}
	}

	[HarmonyPatch(typeof(Log), nameof(Log.ErrorOnce))]
	public class Log_ErrorOnce_Patch
	{
		public static bool Prefix(string text)
		{
			Logging.Log(LogType.Error, text);
			return false;
		}
	}
}