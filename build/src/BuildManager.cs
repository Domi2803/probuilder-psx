using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJson;

/**
 *	Core build system.
 */
namespace ProBuilder.BuildSystem
{
	/**
	 *	Build system for ProBuilder, capable of creating Unity projects & UnityPackage files.
	 */
	public static class BuildManager
	{
		static int Main(string[] args)
		{
			List<BuildTarget> m_Targets = new List<BuildTarget>();

			bool m_IsDebug = false;

			foreach(string arg in args)
			{
				if(arg.StartsWith("-debug"))
				{
					m_IsDebug = true;
				}
				// No valid argument prefix, treat this input as a build target
				else
				{
					try
					{
						BuildTarget t = SimpleJson.SimpleJson.DeserializeObject<BuildTarget>(File.ReadAllText(arg));
						m_Targets.Add(t);
					}
					catch
					{
						Console.WriteLine("Failed adding built target: " + arg);
					}
				}
			}

			foreach(BuildTarget target in m_Targets)
			{
			    string m_UnityPath = target.GetUnityPath();

			    if(string.IsNullOrEmpty(m_UnityPath))
			    {
			    	Console.WriteLine(string.Format("Build target {0} has invalid Unity path. Skipping.\nMac: {1}\nWindows: {2}",
			    		target.Name,
			    		target.UnityContentsPath,
			    		target.UnityDataPath ));

			    	continue;
			    }

			    if(target.Macros == null)
			    	target.Macros = new Dictionary<string, string>();

				target.Macros.Add("$UNITY", m_UnityPath);

			    target.ExpandMacros();

			    if(target.OnPreBuild != null)
			    {
				    foreach(BuildCommand command in target.OnPreBuild)
				    	BuildCommandEvaluator.Execute(command);
			    }

				foreach(AssemblyTarget at in target.Assemblies)
				{
					if(!Compiler.CompileDLL(at, m_IsDebug))
					{
						// If `Release` build do not continue when compiler throws any wornings or errors.
						if(!m_IsDebug)
						{
							Console.WriteLine(string.Format("Assembly {0} failed compilation. Stopping build.", at.OutputAssembly));
							return 1;
						}
						else
						{
							Console.WriteLine(string.Format("Assembly {0} failed compilation.", at.OutputAssembly));
						}
					}
				}

				if(target.OnPostBuild != null)
				{
				    foreach(BuildCommand command in target.OnPostBuild)
				    	BuildCommandEvaluator.Execute(command);
				   }
			}

			return 0;
		}
	}
}
