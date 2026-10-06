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
		public static void Run(Context context)
		{
			string gameDir = GameInstaller.GameDir(context);
			Log.Info(GameActivity.LogTag, $"Iniciando Celeste de {gameDir}");

			// Ver CelesteAndroid.HostConfig (Celeste.Android.Patches).
			AppContext.SetData("CelesteAndroid.Platform", "Android");
			AppContext.SetData("CelesteAndroid.PrefPath", GameInstaller.UserDir(context));
			AppContext.SetData("CelesteAndroid.BackgroundPath", GameInstaller.BackgroundPng(context));

			// O FNA resolve o Content relativo ao diretório de trabalho no Android.
			AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", gameDir + Path.DirectorySeparatorChar);
			Environment.CurrentDirectory = gameDir;

			// Deps do Everest (MMHOOK, MonoMod.*, YamlDotNet, Newtonsoft, DiscordGameSDK...) ficam em files/everest-libs/.
			string libsDir = GameInstaller.EverestLibsDir(context);
			AssemblyLoadContext.Default.Resolving += (ctx, name) =>
			{
				string candidate = Path.Combine(libsDir, name.Name + ".dll");
				return File.Exists(candidate) ? ctx.LoadFromAssemblyPath(candidate) : null;
			};

			// O Everest espera uma pasta Mods/ (vazia nesta fase = vanilla).
			Directory.CreateDirectory(GameInstaller.ModsDir(context));

			Assembly celeste = AssemblyLoadContext.Default.LoadFromAssemblyPath(GameInstaller.PatchedDll(context));

			// Engine.AssemblyDirectory vem de Assembly.Location, que não aponta para o jogo.
			celeste.GetType("Monocle.Engine", throwOnError: true)!
				.GetField("AssemblyDirectory", BindingFlags.NonPublic | BindingFlags.Static)!
				.SetValue(null, gameDir);

			MethodInfo main = celeste.GetType("Celeste.Celeste", throwOnError: true)!
				.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static)!;
			// --loglevel verbose: boot detalhado do Everest no logcat (equivale ao everest-launch.txt do desktop).
			main.Invoke(null, new object[] { new[] { "--loglevel", "verbose" } });
		}
	}
}
