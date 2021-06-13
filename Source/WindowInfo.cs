using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DevHelper
{
	public static class WindowExtensions
	{
		public static HashSet<Type> ResizeableWindowTypes;

		public static WindowInfo GetPositionInfo(this Window window)
		{
			var windowType = window?.GetType();
			if (windowType == null) return null;

			ResizeableWindowTypes ??= typeof(EditWindow).Assembly.GetTypes()
					.Where(type => type.IsSubclassOf(typeof(EditWindow))).ToHashSet();
			if (ResizeableWindowTypes.Contains(windowType) == false) return null;

			var state = Helper.Settings.windowState;
			if (state.TryGetValue(windowType, out var info) == false)
			{
				info = new WindowInfo();
				state.Add(windowType, info);
				Helper.Settings.Write();
			}
			return info;
		}
	}

	public class WindowInfo : IExposable
	{
		public List<float> dimensions = new List<float>() { 0, 0, 0, 0 };
		public bool visible;

		public void ExposeData()
		{
			Scribe_Values.Look(ref visible, "visible", default, true);
			Scribe_Collections.Look(ref dimensions, "dimensions", LookMode.Value);
			if (dimensions == null || dimensions.Count != 4)
				dimensions = new List<float>() { 0, 0, 0, 0 };
		}

		public Rect Rect
		{
			get => new Rect(dimensions[0], dimensions[1], dimensions[2], dimensions[3]);
			set => dimensions = new List<float>() { value.x, value.y, value.width, value.height };
		}
	}
}
