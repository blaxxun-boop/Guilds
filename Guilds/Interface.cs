using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Guilds;

public static class Interface
{
	internal static GameObject NoGuildUIPrefab = null!;
	internal static GameObject SearchGuildUIPrefab = null!;
	internal static GameObject CreateGuildUIPrefab = null!;
	internal static GameObject GuildManagementUIPrefab = null!;
	internal static GameObject ApplicationsUIPrefab = null!;
	internal static GameObject EditGuildUIPrefab = null!;
	internal static GameObject AchievementUIPrefab = null!;
	internal static GameObject AchievementPopupPrefab = null!;
	internal static GameObject NoGuildUI = null!;
	internal static GameObject SearchGuildUI = null!;
	internal static GameObject CreateGuildUI = null!;
	internal static GameObject GuildManagementUI = null!;
	internal static GameObject ApplicationsUI = null!;
	internal static GameObject EditGuildUI = null!;
	internal static GameObject AchievementUI = null!;
	internal static GameObject AchievementPopup = null!;
	internal static readonly Dictionary<int, Sprite> GuildIcons = new();
	internal static readonly Dictionary<string, Sprite> AchievementIcons = new();

	internal static void LoadAssets()
	{
		AssetBundle assets = Tools.LoadAssetBundle("guildsbundle");
		NoGuildUIPrefab = assets.LoadAsset<GameObject>("NoGuild");
		CreateGuildUIPrefab = assets.LoadAsset<GameObject>("CreateGuild");
		SearchGuildUIPrefab = assets.LoadAsset<GameObject>("SearchGuild");
		GuildManagementUIPrefab = assets.LoadAsset<GameObject>("GuildManagementUI");
		ApplicationsUIPrefab = assets.LoadAsset<GameObject>("ApplicationsUI");
		EditGuildUIPrefab = assets.LoadAsset<GameObject>("EditGuild");
		AchievementUIPrefab = assets.LoadAsset<GameObject>("AchievementUI");
		AchievementPopupPrefab = assets.LoadAsset<GameObject>("AchievementPopup");
		assets.Unload(false);
		
		foreach (string s in Assembly.GetExecutingAssembly().GetManifestResourceNames())
		{
			if (s.StartsWith("Guilds.Icons.Badges", StringComparison.Ordinal))
			{
				string[] parts = s.Split('.');
				GuildIcons.Add(int.Parse(parts[parts.Length - 2]), Tools.loadSprite(s.Replace("Guilds.Icons.", ""), 128, 128));
			}
			else if (s.StartsWith("Guilds.Icons.Achievements", StringComparison.Ordinal))
			{
				AchievementIcons[s.Replace("Guilds.Icons.Achievements.", "")] = Tools.loadSprite(s.Replace("Guilds.Icons.", ""), 128, 128);
			}
		}
	}

	private static AssetBundle LoadAssetBundle(string bundleName)
	{
		string resource = typeof(Guilds).Assembly.GetManifestResourceNames().Single(s => s.EndsWith(bundleName));
		return AssetBundle.LoadFromStream(typeof(Guilds).Assembly.GetManifestResourceStream(resource));
	}

	internal static void Update()
	{
		Patches.PreventMainMenu.AllowMainMenu = true;

		if (Player.m_localPlayer is not { } player)
		{
			return;
		}

		if (player.TakeInput() && Guilds.guildInterfaceKey.Value.IsDown())
		{
			if (API.GetOwnGuild() is null)
			{
				NoGuildUI.SetActive(true);
			}
			else
			{
				GuildManagementUI.SetActive(true);
			}
		}

		if (UIIsActive() && Input.GetKey(KeyCode.Escape))
		{
			HideUI();
			Patches.PreventMainMenu.AllowMainMenu = false;
		}
	}

	internal static bool UIIsActive() => (NoGuildUI && NoGuildUI.activeSelf) || (CreateGuildUI && CreateGuildUI.activeSelf) || (SearchGuildUI && SearchGuildUI.activeSelf) || (GuildManagementUI && GuildManagementUI.activeSelf) || (ApplicationsUI && ApplicationsUI.activeSelf) || (EditGuildUI && EditGuildUI.activeSelf) || (AchievementUI && AchievementUI.activeSelf);

	internal static void HideUI()
	{
		NoGuildUI.SetActive(false);
		SearchGuildUI.SetActive(false);
		CreateGuildUI.SetActive(false);
		GuildManagementUI.SetActive(false);
		ApplicationsUI.SetActive(false);
		EditGuildUI.SetActive(false);
		AchievementUI.SetActive(false);
	}

	internal static void SwitchUI(GameObject newUI, bool hideOld = true)
	{
		if (hideOld)
		{
			HideUI();
		}
		newUI.SetActive(true);
	}
}
