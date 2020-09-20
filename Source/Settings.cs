using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DevHelper
{
	public class DevHelperSettings : ModSettings
	{
		public Dictionary<Type, WindowInfo> windowState = new Dictionary<Type, WindowInfo>();
		public bool remoteLoggingEnabled = true;
		public string remoteLoggingHostname = "localhost";
		public int remoteLoggingPort = 8888;
		public string lastError;

		string hostname;
		string port;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref windowState, "windowState", LookMode.Value, LookMode.Deep);
			windowState = windowState ?? new Dictionary<Type, WindowInfo>();
			Scribe_Values.Look(ref remoteLoggingEnabled, "remoteLoggingEnabled", true);
			Scribe_Values.Look(ref remoteLoggingHostname, "remoteLoggingHostname", "localhost");
			Scribe_Values.Look(ref remoteLoggingPort, "remoteLoggingPort", 8888);
			Scribe_Values.Look(ref lastError, "lastError", null);
		}

		public static void Label(Listing_Standard list, string text, GameFont font = GameFont.Medium, Color? color = null)
		{
			var oldFont = Text.Font;
			Text.Font = font;
			var oldColor = GUI.color;
			if (color.HasValue)
				GUI.color = color.Value;
			_ = list.Label(text);
			Text.Font = oldFont;
			GUI.color = oldColor;
		}

		public void DoWindowContents(Rect inRect)
		{
			if (hostname == null)
			{
				hostname = Helper.Settings.remoteLoggingHostname;
				port = $"{Helper.Settings.remoteLoggingPort}";
			}

			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);
			list.Gap();

			var wasEnabled = Helper.Settings.remoteLoggingEnabled;
			list.CheckboxLabeled("Remote logging enabled", ref Helper.Settings.remoteLoggingEnabled);
			if (wasEnabled != Helper.Settings.remoteLoggingEnabled)
				Logging.RefreshConnection();

			if (Helper.Settings.remoteLoggingEnabled)
			{
				list.Gap(4);
				Label(list, "Host", GameFont.Tiny);
				hostname = list.TextEntry(hostname);
				list.Gap(4);
				Label(list, "Port", GameFont.Tiny);
				var portStr = list.TextEntry(port);
				if (int.TryParse(portStr, out var nr)) port = portStr;
				list.Gap(8);
				if (Helper.Settings.lastError != null || Helper.Settings.remoteLoggingHostname != hostname || Helper.Settings.remoteLoggingPort != int.Parse(port))
				{
					if (list.ButtonText("Reconnect"))
					{
						Helper.Settings.remoteLoggingHostname = hostname;
						Helper.Settings.remoteLoggingPort = int.Parse(port);
						Logging.RefreshConnection();
					}
				}
				if (Helper.Settings.lastError != null)
					Label(list, Helper.Settings.lastError, GameFont.Small, Color.red);
			}
			list.Gap();

			list.End();
		}
	}
}