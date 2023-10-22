using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using ServerSync;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Guilds;

public static class Achievements
{
	private static readonly CustomSyncedValue<string> achievementConfigData = new(Guilds.configSync, "achievementConfig", "");
	private static Dictionary<string, AchievementConfig> achievementConfigs = new();
	internal static readonly Dictionary<string, AchievementConfig> dynamicAchievementConfigs = new();
	private static string achievementConfigPath = null!;

	internal static readonly List<API.AchievementCompleted> achievementCompletedCallbacks = new();

	internal static void Init()
	{
		achievementConfigPath = Path.GetDirectoryName(Guilds.self.Config.ConfigFilePath)! + Path.DirectorySeparatorChar + "AchievementConfig.yml";
		achievementConfigData.ValueChanged += ConfigChanged;
		Guilds.guildAchievementConfig.SettingChanged += (_, _) => ConfigChanged();
			
		void ConfigChanged()
		{
			if (Guilds.guildAchievementConfig.Value == Guilds.GuildAchievements.Disabled)
			{
				achievementConfigs = new Dictionary<string, AchievementConfig>();
				return;
			}
			
			try
			{
				Dictionary<string, AchievementConfig> configs = deserializeConfig(achievementConfigData.Value);
				if (Guilds.guildAchievementConfig.Value != Guilds.GuildAchievements.External)
				{
					achievementConfigs = deserializeConfig(System.Text.Encoding.UTF8.GetString(Tools.ReadEmbeddedFileBytes("AchievementConfig.yml")));
					foreach (KeyValuePair<string, AchievementConfig> kv in configs)
					{
						achievementConfigs[kv.Key] = kv.Value;
					}
				}
				else
				{
					achievementConfigs = configs;
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to deserialize achievementConfig: {e}");
			}
		}

		readAchievementConfigFile();
		Guilds.addFileWatchEvent(new FileSystemWatcher(Path.GetDirectoryName(achievementConfigPath)!, Path.GetFileName(achievementConfigPath)), (_, _) => readAchievementConfigFile());

		API.RegisterOnAchievementCompleted((player, achievement) =>
		{
			if (GetAchievementConfig(achievement) is { } config && API.GetOwnGuild() is { } guild && guild.Members.ContainsKey(player) && guild.Achievements.TryGetValue(achievement, out AchievementData data))
			{
				AchievementPopup.Queue(player, config, data.completed.Count);
			}
		});
	}

	private static Dictionary<string, AchievementConfig> deserializeConfig(string data)
	{
		IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

		Dictionary<string, AchievementConfig> configs = new(deserializer.Deserialize<Dictionary<string, AchievementConfig>?>(data) ?? new Dictionary<string, AchievementConfig>(), StringComparer.InvariantCultureIgnoreCase);
		foreach (AchievementConfig achievementConfig in configs.Values)
		{
			// yamldotnet helpfully nulls fields if empty
			// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
			achievementConfig.progress ??= new List<float> { 1 };
			achievementConfig.level ??= new List<int>();
			achievementConfig.config ??= new Dictionary<string, string>();
			// ReSharper restore NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		}

		return configs;
	}

	internal static AchievementConfig? GetAchievementConfig(string achievement)
	{
		if (!achievementConfigs.TryGetValue(achievement, out AchievementConfig config))
		{
			dynamicAchievementConfigs.TryGetValue(achievement, out config);
		}
		return config;
	}

	internal static IEnumerable<KeyValuePair<string, AchievementConfig>> AllAchievementConfigs() => achievementConfigs.Concat(dynamicAchievementConfigs);

	private static void readAchievementConfigFile()
	{
		achievementConfigData.AssignLocalValue(File.Exists(achievementConfigPath) ? File.ReadAllText(achievementConfigPath) : "");
	}

	[HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.IncrementStat))]
	private static class IncreaseGuildAchievementProgress
	{
		private static void Postfix(PlayerStatType stat, float amount)
		{
			if (API.GetOwnGuild() is { } guild)
			{
				API.IncreaseAchievementProgress(guild, stat.ToString().ToLowerInvariant(), amount);
			}
		}
	}
}
