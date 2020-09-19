using HarmonyLib;
using System;
using UnityEngine;

namespace DevHelper
{
	public static class Logging
	{
		public static void Init()
		{
			Application.logMessageReceived += (string condition, string stackTrace, LogType type) =>
			{
				var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss.fff");
				FileLog.Log($"{timestamp} {type.ToString().Substring(0, 3).ToUpper()} {condition}");
			};
		}
	}
}