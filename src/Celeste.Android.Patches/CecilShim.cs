using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Mono.Cecil;
using MonoMod;

namespace CelesteAndroid
{
	/// <summary>
	/// Destrava o relink de code mods do Everest no Android.
	///
	/// No .NET do Android, Assembly.Location de um assembly já carregado vem como
	/// "&lt;gameDir&gt;/&lt;NomeSimples&gt;" SEM a extensão ".dll" (ex.: ".../files/Celeste/FNA").
	/// O Relinker do Everest (EverestModuleAssemblyContext.ResolveGlobal) resolve as libs do
	/// jogo fazendo ModuleDefinition.ReadModule(assembly.Location), que então estoura
	/// FileNotFoundException para FNA e para dezenas de System.*/MonoMod.*. Com o FNA sem
	/// resolver, MonoModRules.RelinkAgainstFNA lança "Failed to resolve
	/// Microsoft.Xna.Framework.Game" e o mod cai como "(mod assembly failed to load)".
	///
	/// Este shim religa SÓ o overload de 1 argumento de ReadModule: se o caminho não existir,
	/// tenta "&lt;path&gt;.dll" e, por fim, procura no gameDir pelo nome do arquivo. Todos os
	/// assemblies do jogo já são empacotados no gameDir (assets/everest/gamedir). O overload de
	/// 2 argumentos NÃO é religado, então chamá-lo aqui não recursa.
	///
	/// IMPORTANTE: só entra no build Everest (Spike=true). O patch vanilla on-device
	/// (CelestePatcher) não tem o Mono.Cecil nas dependências e quebraria ao mesclar este tipo
	/// — por isso o csproj remove este arquivo quando Spike != true.
	/// </summary>
	public static class CecilShim
	{
		[MonoModLinkFrom("Mono.Cecil.ModuleDefinition Mono.Cecil.ModuleDefinition::ReadModule(System.String)")]
		public static ModuleDefinition ReadModule(string fileName)
			=> ModuleDefinition.ReadModule(Resolve(fileName), new ReaderParameters());

		private static string Resolve(string fileName)
		{
			if (string.IsNullOrEmpty(fileName) || File.Exists(fileName))
				return fileName;

			// Android: Location sem extensão (".../Celeste/FNA" -> ".../Celeste/FNA.dll").
			if (File.Exists(fileName + ".dll"))
				return fileName + ".dll";

			// Fallback: nome do arquivo dentro do gameDir (BaseDirectory = gameDir no launcher).
			string gameDir = AppContext.BaseDirectory;
			if (!string.IsNullOrEmpty(gameDir))
			{
				string name = Path.GetFileName(fileName);
				string candidate = Path.Combine(gameDir, name);
				if (File.Exists(candidate))
					return candidate;
				if (File.Exists(candidate + ".dll"))
					return candidate + ".dll";
			}

			// Não achou: devolve o original para o Cecil lançar como antes.
			return fileName;
		}
	}

	/// <summary>
	/// Destrava a APLICAÇÃO de hooks de code mods no Android (etapa seguinte ao relink).
	///
	/// O Everest carrega o .dll relinkado do mod com File.OpenRead(path) + LoadFromStream.
	/// No MonoVM do Android, um assembly carregado por stream não tem caminho associado: quando
	/// o MonoMod.RuntimeDetour aplica um hook do mod, o JIT (RuntimeMethodHandle.GetFunctionPointer)
	/// tenta resolver o assembly pelo nome via open_from_bundles e estoura
	/// "Could not load file or assembly 'XYZ'" (não está no bundle do APK; está em Mods/Cache).
	///
	/// Este shim religa o overload LoadFromStream(Stream, Stream): quando o stream é um arquivo
	/// (FileStream.Name), carrega via LoadFromAssemblyPath para o MonoVM conhecer o caminho e
	/// conseguir compilar os métodos do mod (o .pdb ao lado é achado automaticamente). Fora desse
	/// caso, cai no comportamento original. Só entra no build Everest (Spike=true).
	/// </summary>
	public static class AsmLoadShim
	{
		private static readonly MethodInfo _loadFromStream = typeof(AssemblyLoadContext).GetMethod(
			nameof(AssemblyLoadContext.LoadFromStream),
			BindingFlags.Public | BindingFlags.Instance,
			null, new[] { typeof(Stream), typeof(Stream) }, null)!;

		[MonoModLinkFrom("System.Reflection.Assembly System.Runtime.Loader.AssemblyLoadContext::LoadFromStream(System.IO.Stream,System.IO.Stream)")]
		public static Assembly LoadFromStream(AssemblyLoadContext alc, Stream assembly, Stream assemblySymbols)
		{
			if (assembly is FileStream fs && !string.IsNullOrEmpty(fs.Name) && File.Exists(fs.Name))
				return alc.LoadFromAssemblyPath(fs.Name);

			// Chamada direta recursaria (a instrução seria religada para cá). Invoca por reflexão.
			return (Assembly)_loadFromStream.Invoke(alc, new object[] { assembly, assemblySymbols });
		}
	}
}
