using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace DevHelper
{
	[HarmonyPatch]
	public static class PatchesExceptions
	{
		public static List<CrashReport> crashReports = new List<CrashReport>();

		public static readonly MethodInfo OriginalLogError = SymbolExtensions.GetMethodInfo(() => Log.Error("", false));
		public static readonly MethodInfo Handle_Exception = SymbolExtensions.GetMethodInfo(() => HandleException(default, default, default));
		public static readonly HashSet<int> SeenErrors = new HashSet<int>();

		public static Stopwatch watch;
		public static int patchCounter;

		public static void HandleException(string message, bool ignoreStopLoggingLimit, Exception exception)
		{
			Log.Error(message, ignoreStopLoggingLimit);
			if (SeenErrors.Add(message.GetHashCode()))
				crashReports.Add(new CrashReport(exception, message));
		}

		public static void ReplaceInstruction(this List<CodeInstruction> instructions, int index, CodeInstruction[] replacement)
		{
			instructions[index].opcode = replacement[0].opcode;
			instructions[index].operand = replacement[0].operand;
			instructions.InsertRange(index + 1, replacement.Skip(1));
		}

		[HarmonyPriority(int.MinValue)]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var list = instructions.ToList();
			for (var i = 0; i < list.Count; i++)
			{
				var instruction = list[i];
				if (instruction.blocks.All(block => block.blockType != ExceptionBlockType.BeginCatchBlock))
					continue;
				var catchBlockStart = i;
				var catchBlockEnd = list.FindIndex(catchBlockStart, instr => instr.blocks.Any(block => block.blockType == ExceptionBlockType.EndExceptionBlock));
				if (catchBlockEnd == -1)
					continue;
				var catchInstructions = list.GetRange(catchBlockStart, catchBlockEnd - catchBlockStart);
				var logErrorOffset = catchInstructions.FindIndex(instr => instr.Calls(OriginalLogError));
				if (logErrorOffset >= 0)
				{
					var ex = generator.DeclareLocal(typeof(Exception));
					list.ReplaceInstruction(catchBlockStart, new[]
					{
						new CodeInstruction(OpCodes.Dup),
						new CodeInstruction(OpCodes.Stloc, ex),
						list[catchBlockStart].Clone()
					});
					list.ReplaceInstruction(catchBlockStart + 2 + logErrorOffset, new[]
					{
						new CodeInstruction(OpCodes.Ldloc, ex),
						new CodeInstruction(OpCodes.Call, Handle_Exception)
					});
					catchBlockEnd += 3;
				}
				i = catchBlockEnd + 1;
			}
			return list.AsEnumerable();
		}

		public static void Prepare(MethodBase original)
		{
			if (original == null)
			{
				watch = new Stopwatch();
				watch.Start();
			}
		}

		public static IEnumerable<MethodBase> TargetMethods()
		{
			var methodsToPatch = new List<MethodBase>();
			typeof(Pawn).Assembly.DefinedTypes
				.Where(type => type.IsGenericType == false)
				.SelectMany(type => type.DeclaredMethods)
				.DoIf(method => method.IsGenericMethod == false && method.ContainsGenericParameters == false, method =>
				{
					try
					{
						var body = method.GetMethodBody();
						if (body == null || body.ExceptionHandlingClauses.Count == 0)
							return;
						var info = PatchProcessor.ReadMethodBody(method).ToList();
						for (var i = 0; i < info.Count - 1; i++)
						{
							var opcode = info[i].Key;
							if (opcode == OpCodes.Leave || opcode == OpCodes.Leave_S)
							{
								if (info[i + 1].Key.StackBehaviourPop == StackBehaviour.Pop1)
								{
									var foundLogError = false;
									for (var j = i + 2; j < info.Count; j++)
									{
										opcode = info[j].Key;
										if (opcode == OpCodes.Call && info[j].Value as MethodInfo == OriginalLogError)
											foundLogError = true;
										if (opcode == OpCodes.Leave || opcode == OpCodes.Leave_S)
											break;
									}
									if (foundLogError)
									{
										methodsToPatch.Add(method);
										break;
									}
								}
							}
						}
					}
					finally { }
				});
			patchCounter = methodsToPatch.Count();
			return methodsToPatch.AsEnumerable();
		}

		public static void Cleanup(MethodBase original)
		{
			if (original == null)
			{
				Log.Message($"# Patching {patchCounter} exception message handling locations took {watch.ElapsedMilliseconds} ms");
				watch.Stop();
			}
		}
	}

	public class CrashReport
	{
		public static readonly string RimworldAssemblyName = typeof(Pawn).Assembly.GetName().Name;

		public Exception exception;
		public string message;
		public ModLine[] mods;

		public CrashReport(Exception exception, string message)
		{
			this.exception = exception;
			this.message = message;
			mods = new ModLine[0];

			MethodBase topMethod = null;
			var seenAssemblies = new HashSet<Assembly>();
			var lines = new List<string[]>();
			while (exception != null)
			{
				var st = new StackTrace(exception);
				st.GetFrames().Select(frame => frame.GetMethod())
					.DoIf(method => method != null, method =>
					{
						topMethod = topMethod ?? method;
						var assembly = method.DeclaringType.Assembly;
						if (IsModMethod(method) && seenAssemblies.Add(assembly))
						{
							var metaData = ModLine.GetModMetaData(assembly);
							if (metaData != null && metaData.IsCoreMod == false)
								_ = mods.AddToArray(new ModLine(assembly, method));
						}
					});
				exception = exception.InnerException;
			}
		}

		public static bool IsModMethod(MethodBase method)
		{
			var references = method.DeclaringType.Assembly.GetReferencedAssemblies();
			return references.Any(assemblyName => assemblyName.Name == RimworldAssemblyName);
		}
	}

	public class ModLine
	{
		public Assembly assembly;
		public MethodBase method;
		readonly ModMetaData metaData;

		public ModLine(Assembly assembly, MethodBase method)
		{
			metaData = GetModMetaData(assembly);
			this.method = method;
		}

		public string Name => metaData.Name;
		public string Author => metaData.Author;
		public int SteamID => metaData.SteamAppId;
		public string Url => metaData.Url;
		public string LastMethod => $"{method.DeclaringType.FullName}.{method.Name}";

		public static readonly Dictionary<Assembly, ModMetaData> MetaDataCache = new Dictionary<Assembly, ModMetaData>();
		public static ModMetaData GetModMetaData(Assembly assembly)
		{
			if (MetaDataCache.TryGetValue(assembly, out var metaData) == false)
			{
				var contentPack = LoadedModManager.RunningMods
					.FirstOrDefault(m => m.assemblies.loadedAssemblies.Contains(assembly));
				if (contentPack != null)
					metaData = new ModMetaData(contentPack.RootDir);
				MetaDataCache.Add(assembly, metaData);
			}
			return metaData;
		}
	}
}