using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx;
using JetBrains.Annotations;
#if ! API
using YamlDotNet.Serialization;
#endif

namespace Guilds;

[PublicAPI]
public class Guild
{
	public string Name = "";
	public GuildGeneral General = new();
	public Dictionary<PlayerReference, GuildMember> Members = new();
	public Dictionary<PlayerReference, Application> Applications = new();
	public Dictionary<string, AchievementData> Achievements = new();
	public CustomData customData = new();
}

[PublicAPI]
public class GuildMember
{
	public Ranks rank = Ranks.Member;
#if ! API
	[YamlMember(Alias = "last online", ApplyNamingConventions = false)]
#endif
	public DateTime lastOnline = DateTime.Now;
	public Dictionary<int, Dictionary<string, int>> contribution = new();
}

[PublicAPI]
public class GuildGeneral
{
	public int id = 0;
	public string description = "";
#if ! API
	[YamlMember(Alias = "icon id", ApplyNamingConventions = false)]
#endif
	public int icon = 1;
	public int level = 0;
#if ! API
	[YamlMember(Alias = "guild color", ApplyNamingConventions = false)]
#endif
	public string color = "";
}

[PublicAPI]
public class Application
{
	public DateTime applied = DateTime.Now;
	public string description = "";
}

[PublicAPI]
public class AchievementData
{
	public float? progress = 0;
	public List<DateTime> completed = new();
}

[PublicAPI]
public class CustomData
{
	internal Dictionary<Type, object> data = new();
	internal Dictionary<string, object> unknown = new();
}

[PublicAPI]
public class AchievementConfig
{
	public string name = "";
	public string description = "";
	public List<float> progress = new() { 1 };
	public List<int>? guild = null;
	public List<int> level = new();
	public string icon = "";
	public bool first;
	public Dictionary<string, string> config = new();

	public T getConfigValue<T>(string name, T defaultValue = default!)
	{
#if ! API
		if (config.TryGetValue(name, out string value))
		{
			if (typeof(T) == typeof(int))
			{
				if (int.TryParse(value, out int integer))
				{
					return (T)(object)integer;
				}
			}
			else if (typeof(T) == typeof(float))
			{
				if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
				{
					return (T)(object)number;
				}
			}
			else
			{
				return (T)(object)value;
			}
		}
#endif
		return defaultValue;
	}

#if ! API
	internal int GetLevel(int completed) => completed <= level.Count ? level[completed - 1] : level.Max();

	internal UnityEngine.Sprite? GetIcon() => Interface.AchievementIcons.TryGetValue(icon, out UnityEngine.Sprite sprite) ? sprite : null;
#endif
}

[PublicAPI]
[StructLayout(LayoutKind.Sequential)]
[TypeConverter(typeof(PlayerReferenceTypeConverter))]
public struct PlayerReference
{
	public static PlayerReference fromPlayerInfo(ZNet.PlayerInfo playerInfo) => new() { id = playerInfo.m_userInfo.m_id.ToString(), name = playerInfo.m_name ?? "" };
	public static PlayerReference fromPlayer(Player player) => player == Player.m_localPlayer ? forOwnPlayer() : fromPlayerInfo(ZNet.instance.m_players.FirstOrDefault(info => info.m_characterID == player.GetZDOID()));
	public static PlayerReference forOwnPlayer() => new() { id = UserInfo.GetLocalUser().UserId.ToString(), name = Game.instance.GetPlayerProfile().GetName() };
#if !API
	public static PlayerReference fromRPC(ZRpc? rpc) => rpc is null ? forOwnPlayer() : fromPlayerInfo(ZNet.instance.m_players.First(p => p.m_userInfo.m_id.ToString().EndsWith(rpc.m_socket.GetHostName())));
#endif

	public string id;
	public string name;

	public static bool operator !=(PlayerReference a, PlayerReference b) => !(a == b);
	public static bool operator ==(PlayerReference a, PlayerReference b) => a.id == b.id && a.name == b.name;
	public bool Equals(PlayerReference other) => this == other;
	public override bool Equals(object? obj) => obj is PlayerReference other && Equals(other);

	// ReSharper disable NonReadonlyMemberInGetHashCode
	public override int GetHashCode() => (id.GetHashCode() * 397) ^ name.GetHashCode();
	// ReSharper restore NonReadonlyMemberInGetHashCode

	public override string ToString() => $"{id}:{name}";

	public static PlayerReference fromString(string str)
	{
		string[] parts = str.Split(':');
		return new PlayerReference { id = parts[0], name = parts[1] };
	}
}

public class PlayerReferenceTypeConverter : TypeConverter
{
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value) => PlayerReference.fromString(value?.ToString() ?? ":");
}
