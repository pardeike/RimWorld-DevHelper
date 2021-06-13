using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace DevHelper
{
	public class PatchesMap
	{
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GenerateContentsIntoMap))]
		public class MapGenerator_GenerateContentsIntoMap_Patch
		{
			public static readonly HashSet<Type> skipGenTypes = new HashSet<Type>()
			{
				typeof(GenStep_Caves),
				typeof(GenStep_RocksFromGrid),
				typeof(GenStep_CavesTerrain),
				typeof(GenStep_Roads),
				typeof(GenStep_RockChunks),
				typeof(GenStep_ScatterRuinsSimple),
				typeof(GenStep_ScatterShrines),
				typeof(GenStep_ScatterThings),
				typeof(GenStep_ScenParts),
				typeof(GenStep_Plants),
				typeof(GenStep_Snow),
				typeof(GenStep_Animals),
				typeof(GenStep_CaveHives)
			};

			public static void UseStep(GenStep genStep, Map map, GenStepParams parameters)
			{
				if (genStep is GenStep_Terrain)
				{
					var underGrid = map.terrainGrid.underGrid;
					var topGrid = map.terrainGrid.topGrid;
					for (var i = 0; i < topGrid.Length; i++)
					{
						underGrid[i] = TerrainDefOf.Soil;
						topGrid[i] = TerrainDefOf.WoodPlankFloor;
					}
					return;
				}

				if (skipGenTypes.Contains(genStep.GetType())) return;
				genStep.Generate(map, parameters);
			}

			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				return instructions.MethodReplacer(
					AccessTools.Method(typeof(GenStep), nameof(GenStep.Generate)),
					SymbolExtensions.GetMethodInfo(() => UseStep(default, default, default))
				);
			}
		}
	}
}
