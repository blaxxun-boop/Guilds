using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using UnityEngine;

namespace Guilds;

internal static class GuildList
{
	private static readonly CustomSyncedValue<Dictionary<string, string>> guildEntries = new(Guilds.configSync, "guildEntries");
	private static Dictionary<string, string> oldEntries = new();
	private static readonly SortedDictionary<int, KeyValuePair<string, Dictionary<string[], object?>>> unappliedChanges = new();
	private static int changeCount = 0;
	private static bool GuildIOActive = false;
	public static readonly Dictionary<string, Guild> guildList = new();
	public static Dictionary<int, Guild> guildsById = new();
	private static readonly Dictionary<string, Guild> guildBackup = new();
	private static HashSet<int> guildsWithPendingChanges = new();
	private static bool truthSource = false;

	public static void Init()
	{
		guildEntries.ValueChanged += () =>
		{
			bool signalAPIUpdates = truthSource == Guilds.configSync.IsSourceOfTruth;
			truthSource = Guilds.configSync.IsSourceOfTruth;

			HashSet<string> guildsToWrite = new();
			bool wasGuildIOActive = GuildIOActive;
			GuildIOActive = true;
			if (Guilds.configSync.IsSourceOfTruth && !wasGuildIOActive)
			{
				foreach (KeyValuePair<string, string> entry in guildEntries.Value.Where(kv => !oldEntries.ContainsKey(kv.Key) || oldEntries[kv.Key] != kv.Value))
				{
					guildsToWrite.Add(entry.Key);
				}

				foreach (string removedEntry in oldEntries.Keys.Except(guildEntries.Value.Keys))
				{
					File.Delete(Guilds.GuildsPath + Path.DirectorySeparatorChar + removedEntry + ".yml");
				}

				oldEntries = guildEntries.Value.ToDictionary(kv => kv.Key, kv => kv.Value);
			}

			//Debug.Log($"VALCHANGED: update from BACKUP\n{new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).WithTypeConverter(new ZDOIDYamlConverter()).Build().Serialize(guildBackup)} and LIST {new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).WithTypeConverter(new ZDOIDYamlConverter()).Build().Serialize(guildList)}");

			Dictionary<int, Guild> oldGuilds = guildsById;
			guildsById = new Dictionary<int, Guild>();
			guildList.Clear();
			guildBackup.Clear();
			foreach (KeyValuePair<string, string> syncedEntry in guildEntries.Value)
			{
				try
				{
					foreach (char c in syncedEntry.Key)
					{
						if (!Tools.ValidateChar(c))
						{
							throw new InvalidDataException($"Invalid character {(int)c} in guild name");
						}
					}

					if (GuildSerialization.Deserialize<Guild?>(syncedEntry.Value) is { } guild)
					{
						Guild guildCopy = GuildSerialization.Deserialize<Guild>(syncedEntry.Value);

						//Debug.Log($"VALCHANGED: new PREDIFF {new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).WithTypeConverter(new ZDOIDYamlConverter()).Build().Serialize(guild)}");

						foreach (Dictionary<string[], object?> diffs in unappliedChanges.Values.Where(kv => kv.Key == syncedEntry.Key).Select(kv => kv.Value))
						{
							ObjectDiff.ApplyDiff(ref guild, diffs);
							ObjectDiff.ApplyDiff(ref guildCopy, diffs);
						}

						//Debug.Log($"VALCHANGED: new POSTDIFF {new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).WithTypeConverter(new ZDOIDYamlConverter()).Build().Serialize(guild)}");

						guildsById.Add(guild.General.id, guild);
						guildList.Add(syncedEntry.Key, guild);
						guildBackup.Add(syncedEntry.Key, guildCopy);

						if (signalAPIUpdates)
						{
							if (!oldGuilds.TryGetValue(guild.General.id, out Guild oldGuild))
							{
								API.InvokeGuildCreated(guild);
							}

							HashSet<PlayerReference> oldMembers = new(oldGuild?.Members.Keys ?? Enumerable.Empty<PlayerReference>());
							foreach (PlayerReference joined in guild.Members.Keys.Except(oldMembers))
							{
								API.InvokeGuildJoined(guild, joined);
							}
							foreach (PlayerReference left in oldMembers.Except(guild.Members.Keys))
							{
								API.InvokeGuildLeft(guild, left);
							}
						}

						if (!wasGuildIOActive && guildsToWrite.Contains(syncedEntry.Key))
						{
							File.WriteAllText(Guilds.GuildsPath + Path.DirectorySeparatorChar + syncedEntry.Key + ".yml", GuildSerialization.Serialize(GuildConfigSerialized.fromGuildConfig(guild)));
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogError($"Failed to deserialize internally transferred guild file {syncedEntry.Key}: {e}");
				}
			}

			if (signalAPIUpdates)
			{
				foreach (KeyValuePair<int, Guild> kv in oldGuilds)
				{
					if (!guildsById.ContainsKey(kv.Key))
					{
						foreach (PlayerReference player in kv.Value.Members.Keys)
						{
							API.InvokeGuildLeft(kv.Value, player);
						}
						API.InvokeGuildDeleted(kv.Value);
					}
				}
			}

			GuildIOActive = wasGuildIOActive;

			if (Interface.GuildManagementUI)
			{
				Interface.GuildManagementUI.GetComponent<GuildManagementUI>().UpdateRows();
				Interface.ApplicationsUI.GetComponent<ApplicationsUI>().UpdateRows();
				Interface.SearchGuildUI.GetComponent<SearchGuildUI>().UpdateRows();

				if (API.GetOwnGuild() is not null)
				{
					if (Interface.NoGuildUI.activeSelf || Interface.CreateGuildUI.activeSelf)
					{
						Interface.SwitchUI(Interface.GuildManagementUI);
					}
					Map.UpdateMapPinColor();
				}
				else
				{
					GuildChat.ToggleGroupsChat(false);
				}
			}
		};
	}

	private static void DelayedGuildUpdate(int guild)
	{
		if (guildsWithPendingChanges.Count == 0)
		{
			IEnumerator NotifyPendingChanges()
			{
				yield return new WaitForSeconds(1);
				HashSet<int> changedGuilds = guildsWithPendingChanges;
				guildsWithPendingChanges = new HashSet<int>();
				foreach (int guild in changedGuilds)
				{
					WriteGuild(guild);
				}
			}

			Guilds.self.StartCoroutine(NotifyPendingChanges());
		}

		guildsWithPendingChanges.Add(guild);
	}

	public static void readGuildFiles()
	{
		if (!GuildIOActive)
		{
			GuildIOActive = true;
			Directory.CreateDirectory(Guilds.GuildsPath);
			oldEntries.Clear();

			foreach (FileInfo file in Directory.GetFiles(Guilds.GuildsPath).Select(s => new FileInfo(s)).Where(file => file.Name.EndsWith(".yml", StringComparison.Ordinal)))
			{
				try
				{
					string guildName = file.Name.Replace(".yml", "");
					if (GuildSerialization.Deserialize<GuildConfigSerialized?>(File.ReadAllText(file.FullName)) is { } deserializedGuild)
					{
						// yamldotnet helpfully nulls fields if empty
						// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
						deserializedGuild.members ??= new List<GuildMemberClass>();
						deserializedGuild.general ??= new GuildGeneral();
						deserializedGuild.applications ??= new List<ApplicationClass>();
						deserializedGuild.customData ??= new CustomData();
						// ReSharper restore NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

						Guild guild = deserializedGuild.toGuild(guildName);

						oldEntries.Add(guildName, GuildSerialization.Serialize(guild));
					}
				}
				catch (Exception e)
				{
					Debug.LogError($"Failed to deserialize guild file {file.Name}: {e}");
				}
			}

			//Debug.Log($"Reread: UPDATE from\n{serializer.Serialize(guildEntries.Value)} to {serializer.Serialize(oldEntries)}");

			guildEntries.AssignLocalValue(oldEntries.ToDictionary(kv => kv.Key, kv => kv.Value));
			GuildIOActive = false;
		}
	}

	[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
	private static class GuildListManipulationListener
	{
		[UsedImplicitly]
		private static void Postfix(ZNet __instance, ZNetPeer peer)
		{
			if (__instance.IsServer())
			{
				peer.m_rpc.Register<ZPackage>("Guild Create Guild", AddGuild);
				peer.m_rpc.Register<ZPackage>("Guild Update Guild", UpdateGuild);
				peer.m_rpc.Register<ZPackage>("Guild Remove Guild", RemoveGuild);
				peer.m_rpc.Register<ZPackage>("Guild Rename Guild", RenameGuild);
				peer.m_rpc.Register<ZPackage>("Guild Increase Achievement", IncreaseAchievement);
			}
			else
			{
				peer.m_rpc.Register<int>("Guild Update Ack", AckUpdate);
				peer.m_rpc.Register<string, string, string>("Guild Achievement Completed", CompletedAchievement);
			}
		}

		private static void AckUpdate(ZRpc rpc, int id)
		{
			unappliedChanges.Remove(id);
			//Debug.Log("Ack for {id}");
		}

		public static void UpdateGuild(ZRpc? rpc, ZPackage zpkg)
		{
			int userChangeId = zpkg.ReadInt();

			string guildName = zpkg.ReadString();
			Dictionary<string[], object?> differences = new();
			int differenceCount = zpkg.ReadInt();
			for (int i = 0; i < differenceCount; ++i)
			{
				string[] path = new string[zpkg.ReadInt()];
				for (int j = 0; j < path.Length; ++j)
				{
					path[j] = zpkg.ReadString();
				}

				object? value = null;
				string typeInfo = zpkg.ReadString();
				if (typeInfo != "" && Type.GetType(typeInfo) is { } type)
				{
					string s = zpkg.ReadString();
					//Debug.Log($"Deser {s} for {type}");
					value = GuildSerialization.Deserialize(s, type);
				}

				differences.Add(path, value);
			}

			Guild guild = guildList[guildName];
			HashSet<PlayerReference> oldMembers = new(guild.Members.Keys);
			//Debug.Log($"guild server pre apply state\n{GuildSerialization.Serialize(guild)}");
			ObjectDiff.ApplyDiff(ref guild, differences);
			//Debug.Log($"guild server post apply state\n{GuildSerialization.Serialize(guild)}");
			guildList[guildName] = guild;
			foreach (PlayerReference joined in guild.Members.Keys.Except(oldMembers))
			{
				API.InvokeGuildJoined(guild, joined);
			}
			foreach (PlayerReference left in oldMembers.Except(guild.Members.Keys))
			{
				API.InvokeGuildLeft(guild, left);
			}

			WriteGuild(guildName);

			rpc?.Invoke("Guild Update Ack", userChangeId);
		}

		public static void AddGuild(ZRpc? rpc, ZPackage zpkg)
		{
			Guild guild = GuildSerialization.Deserialize<Guild>(zpkg.ReadString());
			if (!guildList.ContainsKey(guild.Name))
			{
				guildList[guild.Name] = guild;
			}

			WriteGuild(guild.Name);
		}

		public static void RemoveGuild(ZRpc? rpc, ZPackage zpkg)
		{
			string guildName = zpkg.ReadString();

			guildEntries.Value.Remove(guildName);
			guildEntries.Value = guildEntries.Value;
		}

		public static void RenameGuild(ZRpc? rpc, ZPackage zpkg)
		{
			string oldName = zpkg.ReadString();
			string newName = zpkg.ReadString();

			if (guildEntries.Value.TryGetValue(oldName, out string guildData))
			{
				guildEntries.Value[newName] = guildData;
				guildEntries.Value.Remove(oldName);
				guildEntries.Value = guildEntries.Value;
			}
		}

		public static void IncreaseAchievement(ZRpc? rpc, ZPackage package)
		{
			int guildId = package.ReadInt();
			string achievement = package.ReadString();
			float increment = package.ReadSingle();

			if (guildsById.TryGetValue(guildId, out Guild guild) && Achievements.GetAchievementConfig(achievement) is { } config)
			{
				if (!guild.Achievements.TryGetValue(achievement, out AchievementData data))
				{
					guild.Achievements[achievement] = data = new AchievementData();
					if (config.first && API.GetGuilds().Any(g => g.Achievements.TryGetValue(achievement, out AchievementData guildData) && guildData.progress is null))
					{
						data.progress = null;
					}
				}

				if (data.progress is not null && data.completed.Count < config.progress.Count)
				{
					data.progress += increment;
					if (data.progress >= config.progress[data.completed.Count])
					{
						if (config.first)
						{
							foreach (Guild g in API.GetGuilds())
							{
								if (g.Achievements.TryGetValue(achievement, out AchievementData achiev))
								{
									achiev.progress = null;
								}
							}
						}

						data.completed.Add(DateTime.Now);
						guild.General.level += config.GetLevel(data.completed.Count);
						WriteGuild(guildId);

						PlayerReference player = PlayerReference.fromRPC(rpc);
						foreach (ZNetPeer peer in ZNet.instance.GetConnectedPeers())
						{
							peer.m_rpc.Invoke("Guild Achievement Completed", player.name, player.id, achievement);
						}
						CompletedAchievement(null, player.name, player.id, achievement);

						return;
					}

					DelayedGuildUpdate(guildId);
				}
			}
		}

		private static void CompletedAchievement(ZRpc? rpc, string name, string id, string achievement)
		{
			PlayerReference player = new() { id = id, name = name };
			foreach (API.AchievementCompleted callback in Achievements.achievementCompletedCallbacks)
			{
				callback(player, achievement);
			}
		}
	}

	private static void WriteGuild(string guildName)
	{
		guildsWithPendingChanges.Remove(guildList[guildName].General.id);
		guildEntries.Value[guildName] = GuildSerialization.Serialize(guildList[guildName]);
		guildEntries.Value = guildEntries.Value;
	}

	private static void WriteGuild(int guildId)
	{
		if (guildsById.TryGetValue(guildId, out Guild guild))
		{
			WriteGuild(guild.Name);
		}
	}

	public static void removeGuild(string guildName)
	{
		ZPackage zpkg = new();

		zpkg.Write(guildName);
		guildBackup.Remove(guildName);

		if (ZNet.m_instance.GetServerRPC() is { } rpc)
		{
			rpc.Invoke("Guild Remove Guild", zpkg);
		}
		else
		{
			zpkg.SetPos(0);
			GuildListManipulationListener.RemoveGuild(null, zpkg);
		}
	}

	public static void renameGuild(string oldName, string newName)
	{
		ZPackage zpkg = new();

		zpkg.Write(oldName);
		zpkg.Write(newName);

		guildBackup[newName] = guildBackup[oldName];
		guildBackup.Remove(oldName);

		if (ZNet.m_instance.GetServerRPC() is { } rpc)
		{
			rpc.Invoke("Guild Rename Guild", zpkg);
		}
		else
		{
			zpkg.SetPos(0);
			GuildListManipulationListener.RenameGuild(null, zpkg);
		}
	}

	public static void updateGuild(string guildName)
	{
		ZPackage zpkg = new();

		//Debug.Log($"update guild ...\n{serializer.Serialize(guildList[guildName])}");

		if (guildBackup.ContainsKey(guildName))
		{
			zpkg.Write(++changeCount);
			zpkg.Write(guildName);
			Dictionary<string[], object?> diffs = ObjectDiff.diff(guildBackup[guildName], guildList[guildName]);
			zpkg.Write(diffs.Count);
			foreach (KeyValuePair<string[], object?> diff in diffs)
			{
				zpkg.Write(diff.Key.Length);
				foreach (string diffPart in diff.Key)
				{
					zpkg.Write(diffPart);
				}

				if (diff.Value is null)
				{
					zpkg.Write("");
				}
				else
				{
					zpkg.Write(diff.Value.GetType().AssemblyQualifiedName);
					string serialized = GuildSerialization.Serialize(diff.Value);
					zpkg.Write(serialized);
					//Debug.Log($"Diff send for {string.Join(",", diff.Key)}\n{serialized}");

					Guild guild = guildBackup[guildName];
					//Debug.Log($"guild Backup state\n{serializer.Serialize(guild)}");
					ObjectDiff.ApplyDiff(ref guild, new Dictionary<string[], object?> { { diff.Key, GuildSerialization.Deserialize(serialized, diff.Value.GetType()) } });
					//Debug.Log($"guild Backup post diff apply state\n{serializer.Serialize(guild)}");
					guildBackup[guildName] = guild;
				}
			}

			if (ZNet.m_instance.GetServerRPC() is { } rpc)
			{
				//Debug.Log($"Send for {changeCount}");
				unappliedChanges.Add(changeCount, new KeyValuePair<string, Dictionary<string[], object?>>(guildName, diffs));
				rpc.Invoke("Guild Update Guild", zpkg);
			}
			else
			{
				zpkg.SetPos(0);
				GuildListManipulationListener.UpdateGuild(null, zpkg);
			}
		}
		else
		{
			string serialized = GuildSerialization.Serialize(guildList[guildName]);
			zpkg.Write(serialized);
			guildBackup[guildName] = GuildSerialization.Deserialize<Guild>(serialized);

			//Debug.Log($"Full send\n{serialized}");

			if (ZNet.m_instance.GetServerRPC() is { } rpc)
			{
				rpc.Invoke("Guild Create Guild", zpkg);
			}
			else
			{
				zpkg.SetPos(0);
				GuildListManipulationListener.AddGuild(null, zpkg);
			}
		}
	}

	public static void increaseAchievement(int guildId, string achievement, float increment)
	{
		if (Achievements.GetAchievementConfig(achievement) is not { } config || (config.guild is { } guilds && !guilds.Contains(guildId)))
		{
			return;
		}

		ZPackage zpkg = new();
		zpkg.Write(guildId);
		zpkg.Write(achievement);
		zpkg.Write(increment);

		if (ZNet.instance.GetServerRPC() is { } rpc)
		{
			rpc.Invoke("Guild Increase Achievement", zpkg);
		}
		else
		{
			zpkg.SetPos(0);
			GuildListManipulationListener.IncreaseAchievement(null, zpkg);
		}
	}
}
