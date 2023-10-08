using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Guilds;

public static class AchievementTracker
{
	private static readonly List<string> bosses = new() { "Eikthyr", "gd_king", "Bonemass", "Dragon", "GoblinKing", "SeekerQueen" };

	[HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
	public static class TrackBossKills
	{
		private static void Prefix(Character __instance)
		{
			if (API.GetOwnGuild() is not { } guild)
			{
				return;
			}

			if (bosses.Contains(Utils.GetPrefabName(__instance.gameObject)))
			{
				AchievementStorage storage = AchievementStorage.get(guild);
				if (!storage.guildBossKills.Contains(Utils.GetPrefabName(__instance.gameObject)))
				{
					if (API.GetAchievementConfig("Guild Boss Kills") is { } killsConfig && Tools.GetNearbyGuildMembers(Player.m_localPlayer, 40, true).Count >= killsConfig.getConfigValue("required members", 3))
					{
						storage.guildBossKills.Add(__instance.name);
						storage.Save();

						if (storage.guildBossKills.Count == bosses.Count)
						{
							API.IncreaseAchievementProgress(guild, "Guild Boss Kills", 1);
						}
					}
				}

				if (API.GetAchievementConfig("Guild Boss Kills Timed") is { } timedKillsConfig)
				{
					if (storage.lastBossKillTimes.All(k => k.Value < DateTime.Now.Subtract(new TimeSpan(0, 0, timedKillsConfig.getConfigValue("maximum time between kills", 10)))))
					{
						storage.lastBossKillTimes.Clear();
					}
					storage.lastBossKillTimes[__instance.name] = DateTime.Now;
					if (storage.lastBossKillTimes.Count == bosses.Count)
					{
						API.IncreaseAchievementProgress(guild, "Guild Boss Kills Timed", 1);
					}
					storage.Save();
				}
			}
		}
	}

	[HarmonyPatch(typeof(Trader), nameof(Trader.Interact))]
	private static class TrackTraderDiscovery
	{
		private static void Postfix(Trader __instance)
		{
			if (API.GetOwnGuild() is not { } guild)
			{
				return;
			}

			if (Utils.GetPrefabName(__instance.gameObject) == "Haldor")
			{
				API.IncreaseAchievementProgress(guild, "Found Haldor");
			}
			
			if (Utils.GetPrefabName(__instance.gameObject) == "Hildir")
			{
				API.IncreaseAchievementProgress(guild, "Found Hildir");
			}
		}
	}

	[HarmonyPatch(typeof(Trader), nameof(Trader.OnBought))]
	private static class TrackCoinsSpend
	{
		private static void Postfix(Trader.TradeItem item)
		{
			if (API.GetOwnGuild() is not { } guild)
			{
				return;
			}
			
			API.IncreaseAchievementProgress(guild, "Spend Coins", item.m_price);
		}
	}
}
