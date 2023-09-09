using System;
#if ! API
using BepInEx;
#endif
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Guilds;

[PublicAPI]
public static class API
{
	public static bool IsLoaded()
	{
#if API
		return false;
#else
		return true;
#endif
	}

	public static Guild? GetPlayerGuild(PlayerReference player)
	{
#if API
		return null;
#else
		KeyValuePair<string, Guild> guild = GuildList.guildList.FirstOrDefault(s => s.Value.Members.ContainsKey(player));

		return guild.Key.IsNullOrWhiteSpace() ? null : guild.Value;
#endif
	}

	public static Guild? GetPlayerGuild(Player player) => GetPlayerGuild(PlayerReference.fromPlayer(player));
	public static Guild? GetOwnGuild() => GetPlayerGuild(PlayerReference.forOwnPlayer());

	public static Guild? GetGuild(string name)
	{
#if ! API
		if (GuildList.guildList.TryGetValue(name, out Guild? guild))
		{
			return guild;
		}
#endif
		return null;
	}

	public static Guild? GetGuild(int id)
	{
#if ! API
		if (GuildList.guildsById.TryGetValue(id, out Guild? guild))
		{
			return guild;
		}
#endif
		return null;
	}

	public static List<Guild> GetGuilds()
	{
#if API
		return new List<Guild>();
#else
		return GuildList.guildList.Values.ToList();
#endif
	}

	public static PlayerReference GetGuildLeader(Guild guild) => guild.Members.FirstOrDefault(r => r.Value.rank == Ranks.Leader).Key;

	public static Guild? CreateGuild(string name, PlayerReference leader)
	{
#if API
		return null;
#else
		if (GuildList.guildList.TryGetValue(name, out Guild? _))
		{
			return null;
		}

		int nextID = GuildList.guildsById.Keys.DefaultIfEmpty().Max() + 1;
		Guild guild = new() { General = new GuildGeneral { id = nextID }, Name = name };
		guild.Members.Add(leader, new GuildMember { rank = Ranks.Leader });
		GuildList.guildList[name] = guild;
		GuildList.updateGuild(name);
		return guild;
#endif
	}

	public static bool DeleteGuild(Guild guild)
	{
#if ! API
		if (GuildList.guildList.ContainsKey(guild.Name))
		{
			GuildList.removeGuild(guild.Name);

			return true;
		}
#endif
		return false;
	}

	public static bool RenameGuild(Guild guild, string newName)
	{
#if ! API
		if (GuildList.guildList.ContainsKey(newName))
		{
			return false;
		}

		GuildList.guildList[newName] = GuildList.guildList[guild.Name];
		GuildList.guildList.Remove(guild.Name);
		GuildList.renameGuild(guild.Name, newName);
#endif
		return true;
	}

	public static bool SaveGuild(Guild guild)
	{
#if ! API
		if (GuildList.guildList.ContainsKey(guild.Name))
		{
			GuildList.guildList[guild.Name] = guild;
			GuildList.updateGuild(guild.Name);

			return true;
		}
#endif
		return false;
	}

	public static bool AddPlayerToGuild(PlayerReference player, Guild guild)
	{
		if (GetPlayerGuild(player) is null)
		{
			guild.Members.Add(player, new GuildMember());
			SaveGuild(guild);

			return true;
		}

		return false;
	}

	public static bool RemovePlayerFromGuild(PlayerReference player)
	{
		if (GetPlayerGuild(player) is { } guild)
		{
			guild.Members.Remove(player);
			SaveGuild(guild);

			return true;
		}

		return false;
	}

	public static Ranks GetPlayerRank(PlayerReference player)
	{
		return GetPlayerGuild(player) is not { } guild ? 0 : guild.Members[player].rank;
	}

	public static bool UpdatePlayerRank(PlayerReference player, Ranks newRank)
	{
		if (GetPlayerGuild(player) is { } guild)
		{
			GuildMember member = guild.Members[player];
			member.rank = newRank;
			guild.Members[player] = member;

			return true;
		}

		return false;
	}

	public static IEnumerable<PlayerReference> GetOnlinePlayers(Guild? guild = null)
	{
		HashSet<PlayerReference> onlinePlayers = new(ZNet.instance.m_players.Select(PlayerReference.fromPlayerInfo));
		if (guild is not null)
		{
			return guild.Members.Keys.Where(onlinePlayers.Contains);
		}

		foreach (PlayerReference player in GetGuilds().SelectMany(g => g.Members.Keys))
		{
			onlinePlayers.Remove(player);
		}
		return onlinePlayers;
	}

	public static Guild? GetOwnAppliedGuild() => GetPlayerAppliedGuild(PlayerReference.forOwnPlayer());

	public static Guild? GetPlayerAppliedGuild(PlayerReference player)
	{
#if API
		return null;
#else
		return GuildList.guildList.FirstOrDefault(s => s.Value.Applications.ContainsKey(player)).Value;
#endif
	}

	public static bool ApplyToGuild(PlayerReference player, string description, Guild guild)
	{
		RemovePlayerApplication(player);

		guild.Applications[player] = new Application { description = description };
		return SaveGuild(guild);
	}

	public static bool RemovePlayerApplication(PlayerReference player, Guild? guild = null)
	{
		if (GetPlayerAppliedGuild(player) is { } oldApplied && (guild == null || oldApplied == guild))
		{
			oldApplied.Applications.Remove(player);
			return SaveGuild(oldApplied);
		}
		return true;
	}

	public static void RegisterCustomData(Type type)
	{
#if !API
		CustomDataConverter.RegisteredCustomTypes.Add(type.FullName, type);
#endif
	}

	public static T? GetCustomData<T>(Guild guild) where T : class
	{
#if API
		return null;
#else
		guild.customData.data.TryGetValue(typeof(T), out object value);
		return (T?)value;
#endif
	}

	public static void SetCustomData<T>(Guild guild, T customData) where T : class
	{
#if !API
		guild.customData.data[typeof(T)] = customData;
#endif
	}
}
