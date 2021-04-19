using RimWorld;
using System.Collections.Generic;
using static HarmonyLib.AccessTools;

namespace DevHelper
{
	class Ref
	{
		public static readonly FieldRef<Scenario, List<ScenPart>> parts = FieldRefAccess<Scenario, List<ScenPart>>("parts");
		public static readonly FieldRef<ScenPart_PlayerPawnsArriveMethod, PlayerPawnsArriveMethod> method = FieldRefAccess<ScenPart_PlayerPawnsArriveMethod, PlayerPawnsArriveMethod>("method");
	}
}