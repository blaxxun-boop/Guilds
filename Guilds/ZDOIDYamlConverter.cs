using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Guilds;

public class ZDOIDYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type)
	{
		return type == typeof(ZDOID);
	}

	public object ReadYaml(IParser parser, Type type)
	{
		Scalar scalar = (Scalar)parser.Current!;
		string[] parts = scalar.Value.Split(':');
		ZDOID bytes = new(Int64.Parse(parts[0]), uint.Parse(parts[1]));
		parser.MoveNext();
		return bytes;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type)
	{
		ZDOID id = (ZDOID)value!;
		emitter.Emit(new Scalar($"{id.UserID}:{id.ID}"));
	}
}