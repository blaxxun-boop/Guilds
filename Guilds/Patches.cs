using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Guilds;

public static class Patches
{
	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public class FriendlyFirePatch
	{
		private static bool Prefix(Character __instance, HitData hit)
		{
			if (__instance is Player target && hit.GetAttacker() is Player attacker && hit.m_statusEffectHash != "Staff_shield".GetStableHashCode())
			{
				if (API.GetPlayerGuild(target) is { } guild && guild.Name == API.GetPlayerGuild(attacker)?.Name && Guilds.friendlyFire.Value == Toggle.Off)
				{
					return false;
				}
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.ShowHud))]
	public class DisplayGuildNameAbovePlayer
	{
		private static void Postfix(EnemyHud __instance, Character c, Dictionary<Character, EnemyHud.HudData> ___m_huds)
		{
			if (c is not Player player)
			{
				return;
			}

			GameObject hudBase = ___m_huds[c].m_gui;

			TextMeshProUGUI playerName = hudBase.transform.Find("Name").GetComponent<TextMeshProUGUI>();

			if (hudBase.transform.Find("guildname")?.gameObject is not { } guildnameObject)
			{
				guildnameObject = new GameObject("guildname", typeof(RectTransform));
				guildnameObject.transform.SetParent(hudBase.transform, false);
				((RectTransform)guildnameObject.transform).sizeDelta = new Vector2(300, 12);
				TextMeshProUGUI guildnametext = guildnameObject.AddComponent<TextMeshProUGUI>();
				guildnametext.font = playerName.font;
				guildnametext.fontSize = 14;
				guildnametext.alignment = playerName.alignment;
				Outline outline = guildnameObject.AddComponent<Outline>();
				outline.effectColor = Color.black;
				outline.effectDistance = new Vector2(1, -1);
			}

			TextMeshProUGUI guildNameText = guildnameObject.GetComponent<TextMeshProUGUI>();
			Color defaultColor = __instance.m_baseHudPlayer.transform.Find("Name").GetComponent<TextMeshProUGUI>().color;

			RectTransform nameTransform = playerName.GetComponent<RectTransform>();
			Vector2 namePrivot = nameTransform.pivot;
			if (API.GetPlayerGuild(player) is { } guild)
			{
				ColorUtility.TryParseHtmlString(guild.General.color, out Color color);

				namePrivot.y = 0.2f;
				guildNameText.text = $"<{guild.Name}{(Guilds.displayGuildLevel.Value == Toggle.On ? $" ({guild.General.level})" : "")}>";
				guildNameText.color = Guilds.guildColors.Value == Toggle.On ? color : defaultColor;
			}
			else
			{
				namePrivot.y = 0.5f;
				guildnameObject.GetComponent<TextMeshProUGUI>().text = "";
			}

			nameTransform.pivot = namePrivot;
		}
	}

	[HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
	public static class AffixGuildMenu
	{
		private static void Prefix(Hud __instance)
		{
			Transform transform = __instance.gameObject.GetComponentInParent<Localize>().gameObject.transform;
			Interface.NoGuildUIPrefab.SetActive(false);
			Interface.SearchGuildUIPrefab.SetActive(false);
			Interface.CreateGuildUIPrefab.SetActive(false);
			Interface.GuildManagementUIPrefab.SetActive(false);
			Interface.ApplicationsUIPrefab.SetActive(false);
			Interface.EditGuildUIPrefab.SetActive(false);
			Interface.NoGuildUI = Object.Instantiate(Interface.NoGuildUIPrefab, transform, false);
			Interface.SearchGuildUI = Object.Instantiate(Interface.SearchGuildUIPrefab, transform, false);
			Interface.CreateGuildUI = Object.Instantiate(Interface.CreateGuildUIPrefab, transform, false);
			Interface.GuildManagementUI = Object.Instantiate(Interface.GuildManagementUIPrefab, transform, false);
			Interface.ApplicationsUI = Object.Instantiate(Interface.ApplicationsUIPrefab, transform, false);
			Interface.EditGuildUI = Object.Instantiate(Interface.EditGuildUIPrefab, transform, false);

			UnifiedPopup.instance.transform.SetAsLastSibling();
		}
	}

	[HarmonyPatch]
	private class DisablePlayerInputInGuildMenu
	{
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(StoreGui), nameof(StoreGui.IsVisible)),
			AccessTools.DeclaredMethod(typeof(TextInput), nameof(TextInput.IsVisible)),
		};

		private static void Postfix(ref bool __result)
		{
			if (Interface.UIIsActive())
			{
				__result = true;
			}
		}
	}

	[HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
	internal class PreventMainMenu
	{
		public static bool AllowMainMenu = true;

		private static bool Prefix() => !Interface.UIIsActive() && AllowMainMenu;
	}

	[HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
	private static class UpdateLastOnline
	{
		private static void Prefix(ZNet __instance, ZNetPeer peer)
		{
			if (__instance.IsServer())
			{
				ZNet.PlayerInfo info = __instance.m_players.FirstOrDefault(p => p.m_characterID == peer.m_characterID);
				if (info.m_characterID != ZDOID.None)
				{
					PlayerReference player = PlayerReference.fromPlayerInfo(info);
					if (API.GetPlayerGuild(player) is { } guild)
					{
						guild.Members[player].lastOnline = DateTime.Now;
						API.SaveGuild(guild);
					}
				}
			}
		}
	}
}
