using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Guilds;

public class AchievementStorage
{
	public readonly Dictionary<string, DateTime> lastBossKillTimes = new();
	public readonly List<string> guildBossKills = new();

	[YamlIgnore]
	private Guild? guild;
	
	public static AchievementStorage get(Guild? guild = null)
	{
		guild ??= API.GetOwnGuild();
		if (guild is not null)
		{
			if (API.GetCustomData<AchievementStorage>(guild) is { } storage)
			{
				storage.guild = guild;
				return storage;
			}
			storage = new AchievementStorage { guild = guild };
			API.SetCustomData(guild, storage);
			return storage;
		}
		return new AchievementStorage();
	}

	public void Save()
	{
		if (guild is not null)
		{
			API.SetCustomData(guild, this);
		}
	}
}
