using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.Utilities;

namespace Guilds;

[PublicAPI]
public class GuildConfigSerialized
{
	public GuildGeneral general = new();
	public List<GuildMemberClass> members = new();
	public List<ApplicationClass> applications = new();
	public Dictionary<string, AchievementData> achievements = new();
#if ! API
	[YamlMember(Alias = "custom data", ApplyNamingConventions = false)]
#endif
	public CustomData customData = new();

	public Guild toGuild(string name) => new()
	{
		Name = name,
		Members = new Dictionary<PlayerReference, GuildMember>(members.ToDictionary(m => m.player, m => new GuildMember { rank = m.rank, lastOnline = m.lastOnline } )),
		General = general,
		Applications = new Dictionary<PlayerReference, Application>(applications.ToDictionary(a => a.player, a => new Application { applied = a.applied, description = a.description } )),
		Achievements = achievements,
		customData = customData,
	};

	public static GuildConfigSerialized fromGuildConfig(Guild from) => new()
	{
		members = from.Members.Select(kv => new GuildMemberClass { player = kv.Key, rank = kv.Value.rank, lastOnline = kv.Value.lastOnline }).ToList(),
		applications = from.Applications.Select(kv => new ApplicationClass { player = kv.Key, applied = kv.Value.applied, description = kv.Value.description }).ToList(),
		general = from.General,
		customData = from.customData,
		achievements = from.Achievements,
	};
}
	
[PublicAPI]
public class GuildMemberClass
{
	public PlayerReference player;
	public Ranks rank;
	[YamlMember(Alias = "last online", ApplyNamingConventions = false)] public DateTime lastOnline;
}

[PublicAPI]
public class ApplicationClass
{
	public PlayerReference player;
	public DateTime applied = DateTime.Now;
	public string description = "";
}

public class CustomDataConverter(IValueDeserializer valueDeserializer, IValueSerializer valueSerializer) : IYamlTypeConverter
{
	public static readonly Dictionary<string, Type> RegisteredCustomTypes = new();

	public bool Accepts(Type type) => type == typeof(CustomData);

	public object ReadYaml(IParser parser, Type _)
	{
		parser.Consume<MappingStart>();
		CustomData customData = new();
#pragma warning disable 8601
		while (!parser.TryConsume(out MappingEnd _))
#pragma warning restore 8601
		{
			string typeName = (string)valueDeserializer.DeserializeValue(parser, typeof(string), new SerializerState(), valueDeserializer)!;
			if (RegisteredCustomTypes.TryGetValue(typeName, out Type type))
			{
				if (valueDeserializer.DeserializeValue(parser, type, new SerializerState(), valueDeserializer) is { } value)
				{
					customData.data.Add(type, value);
				}
			}
			else
			{
				if (valueDeserializer.DeserializeValue(parser, typeof(object), new SerializerState(), valueDeserializer) is {} value)
				{
					customData.unknown.Add(typeName, value);
				}
			}
		}
		return customData;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type)
	{
		if (value is null)
		{
			return;
		}
		
		CustomData customData = (CustomData)value;
		emitter.Emit(new MappingStart());
		foreach (KeyValuePair<Type, object> kv in customData.data)
		{
			valueSerializer.SerializeValue(emitter, kv.Key.FullName, typeof(string));
			valueSerializer.SerializeValue(emitter, kv.Value, kv.Key);
		}
		foreach (KeyValuePair<string, object> kv in customData.unknown)
		{
			valueSerializer.SerializeValue(emitter, kv.Key, typeof(string));
			valueSerializer.SerializeValue(emitter, kv.Value, typeof(object));
		}
		emitter.Emit(new MappingEnd());
	}
}

public static class GuildSerialization
{
	private static readonly CustomDataConverter customDataConverter = new(new DeserializerBuilder().WithTypeConverter(new ZDOIDYamlConverter()).BuildValueDeserializer(), new SerializerBuilder().WithTypeConverter(new ZDOIDYamlConverter()).BuildValueSerializer());
	private static readonly IDeserializer Deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).WithTypeConverter(new ZDOIDYamlConverter()).WithTypeConverter(customDataConverter).Build();
	private static readonly ISerializer Serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).WithTypeConverter(new ZDOIDYamlConverter()).WithTypeConverter(customDataConverter).Build();

	public static T Deserialize<T>(string yaml) => Deserializer.Deserialize<T>(yaml);
	public static object? Deserialize(string yaml, Type type) => Deserializer.Deserialize(yaml, type);
	public static string Serialize<T>(T guild) where T: notnull => Serializer.Serialize(guild);
}
