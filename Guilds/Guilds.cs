using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using LocalizationManager;
using ServerSync;
using UnityEngine;

namespace Guilds;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInIncompatibility("org.bepinex.plugins.valheim_plus")]
public class Guilds : BaseUnityPlugin
{
	private const string ModName = "Guilds";
	private const string ModVersion = "1.1.11";
	private const string ModGUID = "org.bepinex.plugins.guilds";

	public static readonly ConfigSync configSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

	internal static Guilds self = null!;

	public static string GuildsPath = null!;

	private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
	{
		ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

		SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
		syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

		return configEntry;
	}

	private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

	private static ConfigEntry<Toggle> serverConfigLocked = null!;
	internal static ConfigEntry<Toggle> friendlyFire = null!;
	internal static ConfigEntry<KeyboardShortcut> guildInterfaceKey = null!;
	internal static ConfigEntry<Toggle> displayGuildLevel = null!;
	internal static ConfigEntry<Toggle> guildColors = null!;
	internal static ConfigEntry<Color> guildChatColor = null!;
	internal static ConfigEntry<int> minimumGuildNameLength = null!;
	internal static ConfigEntry<int> maximumGuildNameLength = null!;
	internal static ConfigEntry<uint> maximumGuildMembers = null!;
	internal static ConfigEntry<Toggle> allowGuildCreation = null!;
	internal static ConfigEntry<Toggle> allowGuildEdit = null!;
	internal static ConfigEntry<KeyboardShortcut> guildPingHotkey = null!;
	internal static ConfigEntry<GuildAchievements> guildAchievementConfig = null!;

	internal enum GuildAchievements
	{
		Disabled = 0,
		Default = 1,
		External = 2,
	}

	public void Awake()
	{
		self = this;

		APIManager.Patcher.Patch();
		Localizer.Load();

		serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.Off, new ConfigDescription("Locks the config and enforces the servers configuration."));
		configSync.AddLockingConfigEntry(serverConfigLocked);
		guildInterfaceKey = config("1 - General", "Guild Interface Key", new KeyboardShortcut(KeyCode.O), new ConfigDescription("Keyboard shortcut to press in order to display the guild interface."), false);
		friendlyFire = config("1 - General", "Friendly fire in guilds", Toggle.Off, new ConfigDescription("If members from the same guild can damage each other in PvP."));
		displayGuildLevel = config("1 - General", "Display guild level on nameplate", Toggle.Off, new ConfigDescription("Displays the level of the guild after its name on nameplates."), false);
		guildColors = config("1 - General", "Guild Colors", Toggle.On, new ConfigDescription("If off, the guild colors will be replaced with Valheims default colors instead."), false);
		guildChatColor = config("1 - General", "Guild Chat Color", new Color(1, 0.7176471f, 0.3602941f), new ConfigDescription("The color for messages in the guild chat."), false);
		minimumGuildNameLength = config("1 - General", "Minimum Name Length", 2, new ConfigDescription("The minimum length of guild names as the number of characters.", new AcceptableValueRange<int>(1, 16)));
		minimumGuildNameLength.SettingChanged += (_, _) =>
		{
			if (minimumGuildNameLength.Value > maximumGuildNameLength.Value)
			{
				minimumGuildNameLength.Value = maximumGuildNameLength.Value;
			}
		};
		maximumGuildNameLength = config("1 - General", "Maximum Name Length", 32, new ConfigDescription("The maximum length of guild names as the number of characters.", new AcceptableValueRange<int>(2, 64)));
		maximumGuildNameLength.SettingChanged += (_, _) =>
		{
			if (maximumGuildNameLength.Value < minimumGuildNameLength.Value)
			{
				maximumGuildNameLength.Value = minimumGuildNameLength.Value;
			}
		};
		allowGuildCreation = config("1 - General", "Allow Guild Creation", Toggle.On, new ConfigDescription("If off, only admins can create new guilds."));
		allowGuildEdit = config("1 - General", "Allow Guild Edit", Toggle.On, new ConfigDescription("If off, only admins can edit guilds."));
		guildPingHotkey = config("1 - General", "Guild Ping Modifier Key", new KeyboardShortcut(KeyCode.LeftShift), new ConfigDescription("Modifier key that has to be pressed while pinging the map, to make the map ping visible to guild members only."), false);
		maximumGuildMembers = config("1 - General", "Maximum Guild Members", 0U, new ConfigDescription("Maximum number of guild members per guild. Set to 0 for no maximum."));
		guildAchievementConfig = config("2 - Achievements", "Ignore Internal Achievement Config", GuildAchievements.Default, new ConfigDescription("Disabled: Guild achievements are disabled and not available on your server.\nDefault: The internal guild achievement config is enabled and can be adjusted via an optional external AchievementConfig.yml file.\nExternal: The internal guild achievement configs are ignored and guild achievements are parsed from an external AchievementConfig.yml file only. This means that you have to keep track of newly added achievements yourself and add them to your config file, if you want to have them on your server."));

		Assembly assembly = Assembly.GetExecutingAssembly();
		Harmony harmony = new(ModGUID);
		harmony.PatchAll(assembly);

		Interface.LoadAssets();
		Map.Init();
		Achievements.Init();

		InvokeRepeating(nameof(updatePositon), 0, 2);
	}

	[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
	private static class ReadGuildList
	{
		private static bool first = true;

		private static void Postfix()
		{
			if (first)
			{
				first = false;

				GuildsPath = Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + Path.DirectorySeparatorChar + "Guilds";

				GuildList.Init();
				GuildList.readGuildFiles();
				addFileWatchEvent(new FileSystemWatcher(GuildsPath, "*.yml"), (_, _) => GuildList.readGuildFiles());
			}
		}
	}

	public void Update() => Interface.Update();

	internal static void addFileWatchEvent(FileSystemWatcher watcher, Action<object, EventArgs> handler)
	{
		watcher.Created += new FileSystemEventHandler(handler);
		watcher.Changed += new FileSystemEventHandler(handler);
		watcher.Renamed += new RenamedEventHandler(handler);
		watcher.Deleted += new FileSystemEventHandler(handler);
		watcher.IncludeSubdirectories = true;
		watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
		watcher.EnableRaisingEvents = true;
	}

	[HarmonyPatch(typeof(Game), nameof(Game.Start))]
	private class ShowPlayerMessage
	{
		private static void Postfix()
		{
			ZRoutedRpc.instance.Register<string>("Guilds PlayerMessage", (_, message) => MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message));
		}
	}

	public static void SendMessageToPlayer(PlayerReference player, string message)
	{
		if (ZNet.instance.m_players.FirstOrDefault(p => PlayerReference.fromPlayerInfo(p) == player) is { m_characterID.UserID: not 0 } playerInfo)
		{
			ZRoutedRpc.instance.InvokeRoutedRPC(playerInfo.m_characterID.UserID, "Guilds PlayerMessage", message);
		}
	}

	public static void SendMessageToAllPlayers(string message)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "Guilds PlayerMessage", message);
	}

	private void updatePositon()
	{
		if (Player.m_localPlayer is { } ownPlayer && API.GetOwnGuild() is { } guild && !ZNet.instance.m_publicReferencePosition)
		{
			foreach (ZNet.PlayerInfo player in ZNet.instance.m_players)
			{
				if (guild.Members.ContainsKey(PlayerReference.fromPlayerInfo(player)) && player.m_characterID != ownPlayer.GetZDOID())
				{
					ZRoutedRpc.instance.InvokeRoutedRPC(player.m_characterID.UserID, "Guilds UpdatePosition", ownPlayer.transform.position);
				}
			}
		}
	}
}
