using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Mono.Cecil;

namespace Assembly.Validator
{
	public class Program
	{
		public static int Main(string[] args)
		{
			if (args == null || args.Length < 1)
			{
				Console.WriteLine("Invalid arguments. Please specify a directory for scanning.");
				return -1;
			}

			var path = Path.GetFullPath(args[0]);
			Console.WriteLine("Will scan directory: " + path);

			if (Directory.Exists(path) == false)
			{
				Console.WriteLine("Directory does not exists: " + path);
				return -1;
			}

			var pathsToIgnore = new List<string>();
			for (var i = 1; i < args.Length; i++)
				pathsToIgnore.Add(Path.GetFullPath(args[i]));

			var debuggableFiles = new List<string>();

			var files = GetFiles(path);
			foreach (var file in files)
			{
				if (pathsToIgnore.Any(x => file.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
					continue;

				using (var stream = File.OpenRead(file))
				{
					try
					{
						var assembly = AssemblyDefinition.ReadAssembly(stream);
						var debuggableAttribute = assembly.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == typeof(DebuggableAttribute).Name);
						if (debuggableAttribute == null)
							continue;

						var debuggingModesAttribute = debuggableAttribute.ConstructorArguments.FirstOrDefault(x => x.Type.Name == typeof(DebuggableAttribute.DebuggingModes).Name);
						var modes = (DebuggableAttribute.DebuggingModes)debuggingModesAttribute.Value;

						if (modes.HasFlag(DebuggableAttribute.DebuggingModes.DisableOptimizations))
							debuggableFiles.Add(file);
					}
					catch (Exception e)
					{
						Console.WriteLine("Could not read assembly '{0}'. Error: {1}.", file, e.Message);
					}
				}
			}

			Console.WriteLine("Scan completed.");

			if (debuggableFiles.Count == 0)
				return 0;

			Console.WriteLine("Following files are debuggable:");
			foreach (var file in debuggableFiles)
			{
				Console.WriteLine(file);
			}

			return -1;
		}

		private static IEnumerable<string> GetFiles(string path)
		{
			var directories = Directory.GetDirectories(path);
			foreach (var file in directories.SelectMany(GetFiles))
				yield return file;

			foreach (var file in Directory.GetFiles(path, "*.dll"))
				yield return file;
		}
	}
}
