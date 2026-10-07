using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Android.Content;
using Android.Util;

namespace CelesteAndroid
{
	/// <summary>
	/// Carrega o Celeste.dll patcheado e chama o Main original, com o mesmo contrato do host desktop.
	/// </summary>
	public static class CelesteLauncher
	{
		/// <param name="everest">true: carrega o Celeste.dll do Everest (+ deps + Mods/); false: vanilla.</param>
		public static void Run(Context context, bool everest)
		{
			string gameDir = GameInstaller.GameDir(context);
			Log.Info(GameActivity.LogTag, $"Iniciando Celeste ({(everest ? "Everest" : "vanilla")}) de {gameDir}");

			// Ver CelesteAndroid.HostConfig (Celeste.Android.Patches).
			AppContext.SetData("CelesteAndroid.Platform", "Android");
			AppContext.SetData("CelesteAndroid.PrefPath", GameInstaller.UserDir(context));
			AppContext.SetData("CelesteAndroid.BackgroundPath", GameInstaller.BackgroundPng(context));

			// O FNA resolve o Content relativo ao diretório de trabalho no Android.
			AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", gameDir + Path.DirectorySeparatorChar);
			Environment.CurrentDirectory = gameDir;

			string dll;
			if (everest)
			{
				// Deps do Everest (MMHOOK, MonoMod.*, YamlDotNet, Newtonsoft, DiscordGameSDK...) em files/everest-libs/.
				string libsDir = GameInstaller.EverestLibsDir(context);
				AssemblyLoadContext.Default.Resolving += (ctx, name) =>
				{
					string candidate = Path.Combine(libsDir, name.Name + ".dll");
					if (File.Exists(candidate))
						return ctx.LoadFromAssemblyPath(candidate);

					// Mods relinkados vivem no ALC próprio do Everest (EverestModuleAssemblyContext),
					// não em everest-libs. Ao aplicar hooks (MonoMod.RuntimeDetour), o JIT do MonoVM
					// resolve o assembly do mod pelo nome e cai aqui (open_from_bundles falha). Devolve
					// a instância já carregada em qualquer ALC — sem isso o mod estoura
					// "Could not load file or assembly 'XYZ'" ao aplicar o primeiro hook.
					foreach (AssemblyLoadContext alc in AssemblyLoadContext.All)
						foreach (Assembly loaded in alc.Assemblies)
							if (loaded.GetName().Name == name.Name)
								return loaded;

					return null;
				};
				Directory.CreateDirectory(GameInstaller.ModsDir(context));   // o Everest espera Mods/
				dll = GameInstaller.EverestPatchedDll(context);
			}
			else
			{
				dll = GameInstaller.PatchedDll(context);
			}

			Assembly celeste = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);

			// Engine.AssemblyDirectory vem de Assembly.Location, que não aponta para o jogo.
			// No modo Everest, o PathGame do Everest = dir do Location (canônico /data/data/...); alinhamos
			// o Engine ao mesmo caminho para o Content e o PathContentOrig baterem (senão o parser de idioma estoura).
			string assemblyDir = everest
				? (Path.GetDirectoryName(celeste.Location) is { Length: > 0 } loc ? loc : gameDir)
				: gameDir;
			celeste.GetType("Monocle.Engine", throwOnError: true)!
				.GetField("AssemblyDirectory", BindingFlags.NonPublic | BindingFlags.Static)!
				.SetValue(null, assemblyDir);

			// Vanilla: Main é nonpublic. Everest torna Main público (embrulha o orig_Main) → incluir Public.
			MethodInfo main = celeste.GetType("Celeste.Celeste", throwOnError: true)!
				.GetMethod("Main", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
			// Everest aceita --loglevel verbose (boot detalhado no logcat); vanilla não parseia args → vazio.
			string[] gameArgs = everest ? new[] { "--loglevel", "verbose" } : Array.Empty<string>();
			main.Invoke(null, new object[] { gameArgs });
		}
	}
}
