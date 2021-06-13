using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace DevHelper
{
	public class PatchesPawn
	{
		[HarmonyPatch(typeof(Page_SelectScenario), nameof(Page_SelectScenario.BeginScenarioConfiguration))]
		public class Page_SelectScenario_BeginScenarioConfiguration_Patch
		{
			public static void Postfix()
			{
				Find.GameInitData.mapSize = 50;
			}
		}

		[HarmonyPatch(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.PreOpen))]
		public class Page_ConfigureStartingPawns_PreOpen_Patch
		{
			public static bool Prefix()
			{
				Find.GameInitData.startingAndOptionalPawns.Clear();
				PageUtility.InitGameStart();
				return false;
			}
		}

		[HarmonyPatch(typeof(Scenario), nameof(Scenario.AllParts), MethodType.Getter)]
		public class Scenario_AllParts_Getter_Patch
		{
			public static readonly HashSet<ScenPartDef> excludedDefs = new HashSet<ScenPartDef>()
			{
				ScenPartDefOf.StartingAnimal,
				ScenPartDefOf.PlayerPawnsArriveMethod,
			};

			public static IEnumerable<ScenPart> Postfix(IEnumerable<ScenPart> input)
			{
				return input.Where(part => excludedDefs.Contains(part.def) == false);
			}
		}

		[HarmonyPatch]
		public class DebugToolsSpawning_SpawnPawn_Patch
		{
			public static MethodInfo TargetMethod()
			{
				var type = AccessTools.FirstInner(typeof(DebugToolsSpawning), t => t.Name.Contains("DisplayClass1_0"));
				return AccessTools.FirstMethod(type, method => method.Name.Contains("SpawnPawn"));
			}

			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var from = SymbolExtensions.GetMethodInfo(() => PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, default));
				var to = SymbolExtensions.GetMethodInfo(() => GeneratePawn(default, default));
				return instructions.MethodReplacer(from, to);
			}

			public static Pawn GeneratePawn(PawnKindDef kindDef, Faction faction)
			{
				var pawn = PawnGenerator.GeneratePawn(kindDef, faction);
				if (kindDef != PawnKindDefOf.Colonist || faction != Faction.OfPlayer)
					return pawn;

				pawn.health.hediffSet?.hediffs?.Clear();
				pawn.inventory.DestroyAll();
				pawn.carryTracker.DestroyCarriedThing();
				pawn.needs.AllNeeds?.Clear();
				pawn.mindState.Reset(true, true);
				pawn.equipment.DestroyAllEquipment();
				pawn.apparel.DestroyAll();
				pawn.abilities.abilities?.Clear();
				pawn.story.childhood?.disallowedTraits?.Clear();
				pawn.story.adulthood?.disallowedTraits?.Clear();
				pawn.story.traits?.allTraits?.Clear();

				foreach (var allDef in DefDatabase<SkillDef>.AllDefsListForReading)
				{
					var skill = pawn.skills.GetSkill(allDef);
					skill.Level = 6;
					skill.passion = Passion.None;
					skill.xpSinceLastLevel = 0;
				}

				foreach (var allDef in DefDatabase<AbilityDef>.AllDefsListForReading)
					pawn.abilities.GainAbility(allDef);

				for (var hour = 0; hour < 24; hour++)
					pawn.timetable.SetAssignment(hour, TimeAssignmentDefOf.Anything);

				return pawn;
			}
		}
	}
}
