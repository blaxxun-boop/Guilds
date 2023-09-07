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
public class CustomData
{
	internal Dictionary<Type, object> data = new();
	internal Dictionary<string, object> unknown = new();
}

[PublicAPI]
[StructLayout(LayoutKind.Sequential)]
[TypeConverter(typeof(PlayerReferenceTypeConverter))]
public struct PlayerReference
{
	public static PlayerReference fromPlayerInfo(ZNet.PlayerInfo playerInfo) => new() { id = playerInfo.m_host.IsNullOrWhiteSpace() ? PrivilegeManager.GetNetworkUserId() : playerInfo.m_host.Contains("_") ? playerInfo.m_host : $"Steam_{playerInfo.m_host}", name = playerInfo.m_name ?? "" };
	public static PlayerReference fromPlayer(Player player) => player == Player.m_localPlayer ? forOwnPlayer() : fromPlayerInfo(ZNet.instance.m_players.FirstOrDefault(info => info.m_characterID == player.GetZDOID()));
	public static PlayerReference forOwnPlayer() => new() { id = PrivilegeManager.GetNetworkUserId(), name = Game.instance.GetPlayerProfile().GetName() };

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
