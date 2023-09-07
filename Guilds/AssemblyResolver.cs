using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
	public sealed class ModuleInitializerAttribute : Attribute
	{
	}
}

namespace Guilds
{
	public static class Initializer
	{
		[ModuleInitializer]
		public static void Init() => AppDomain.CurrentDomain.AssemblyResolve += (_, e) => e.Name.StartsWith("Guilds,") ? Assembly.Load(StreamToByteArray(Assembly.GetExecutingAssembly().GetManifestResourceStream("Guilds.Guilds.dll")!)) : null;

		private static byte[] StreamToByteArray(Stream input)
		{
			using MemoryStream stream = new();
			input.CopyTo(stream);
			return stream.ToArray();
		}
	}
}
