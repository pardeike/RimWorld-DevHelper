using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DevHelper
{
	public class DevHelperSettings : ModSettings
	{
		public Dictionary<Type, WindowInfo> windowState = new Dictionary<Type, WindowInfo>();

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref windowState, "windowState", LookMode.Value, LookMode.Deep);
			windowState = windowState ?? new Dictionary<Type, WindowInfo>();
		}

		public void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);
			list.End();
		}
	}
}