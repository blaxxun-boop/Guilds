using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Guilds;

public static class Patches
{
	private const short IsFriendlyAoe = -23749;

	[HarmonyPatch(typeof(Aoe), nameof(Aoe.OnHit))]
	private static class TagFriendlyFireAoe
	{
		private static HitData CheckAndTag(HitData hit, Aoe aoe)
		{
			if (aoe.m_hitFriendly && Projectile.FindHitObject(hit.m_hitCollider).GetComponent<Player>())
			{
				hit.m_weakSpot = IsFriendlyAoe;
			}
			return hit;
		}

		private static readonly MethodInfo ModifyHit = AccessTools.DeclaredMethod(typeof(HitData.DamageTypes), nameof(HitData.DamageTypes.Modify), new []{ typeof(float) });
		private static readonly MethodInfo Damage = AccessTools.DeclaredMethod(typeof(IDestructible), nameof(IDestructible.Damage));

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			bool preDamage = false;
			foreach (CodeInstruction instruction in instructions)
			{
				if (preDamage && instruction.Calls(Damage))
				{
					preDamage = false;
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(TagFriendlyFireAoe), nameof(CheckAndTag)));
				}
				else if (instruction.Calls(ModifyHit))
				{
					preDamage = true;
				}
				yield return instruction;
			}
		}
	}
	
	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public class FriendlyFirePatch
	{
		private static bool Prefix(Character __instance, HitData hit)
		{
			if (__instance == Player.m_localPlayer && hit.GetAttacker() is Player attacker && hit.m_weakSpot != IsFriendlyAoe)
			{
				if (API.GetOwnGuild() is { } guild && guild.Name == API.GetPlayerGuild(attacker)?.Name && Guilds.friendlyFire.Value == Toggle.Off)
				{
					return false;
				}
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	private static class PreventFriendlyFireMarkerOverwrite
	{
		private static void WriteWeakSpot(HitData hit, short index)
		{
			if (hit.m_weakSpot >= -1)
			{
				hit.m_weakSpot = index;
			}
		}

		private static readonly FieldInfo weakSpotField = AccessTools.DeclaredField(typeof(HitData), nameof(HitData.m_weakSpot));

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.StoresField(weakSpotField))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PreventFriendlyFireMarkerOverwrite), nameof(WriteWeakSpot)));
				}
				else
				{
					yield return instruction;
				}
			}
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
	
	[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
	public static class FejdStartupFixMaterial
	{
		public static Material? originalMaterial = null;

		static void Postfix(FejdStartup __instance)
		{
			AssetBundle[]? assetBundles = Resources.FindObjectsOfTypeAll<AssetBundle>();
			foreach (AssetBundle? bundle in assetBundles)
			{
				IEnumerable<Material>? bundleMaterials;
				try
				{
					bundleMaterials = bundle.isStreamedSceneAssetBundle && bundle
						? bundle.GetAllAssetNames().Select(bundle.LoadAsset<Material>).Where(shader => shader != null)
						: bundle.LoadAllAssets<Material>();
				}
				catch (Exception)
				{
					continue;
				}

				if (bundleMaterials == null) continue;
				foreach (Material? mat in bundleMaterials)
				{
					if (mat && mat.name == "litpanel")
					{
						originalMaterial = mat;
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
	public static class AffixGuildMenu
	{
		private static void Prefix(Hud __instance)
		{
			Transform hudroot = __instance.m_rootObject.transform;
			Interface.NoGuildUIPrefab.SetActive(false);
			Interface.SearchGuildUIPrefab.SetActive(false);
			Interface.CreateGuildUIPrefab.SetActive(false);
			Interface.GuildManagementUIPrefab.SetActive(false);
			Interface.ApplicationsUIPrefab.SetActive(false);
			Interface.EditGuildUIPrefab.SetActive(false);
			Interface.AchievementUIPrefab.SetActive(false);
			Interface.AchievementPopupPrefab.SetActive(false);
			Interface.NoGuildUI = Object.Instantiate(Interface.NoGuildUIPrefab, hudroot, false);
			Interface.SearchGuildUI = Object.Instantiate(Interface.SearchGuildUIPrefab, hudroot, false);
			Interface.CreateGuildUI = Object.Instantiate(Interface.CreateGuildUIPrefab, hudroot, false);
			Interface.GuildManagementUI = Object.Instantiate(Interface.GuildManagementUIPrefab, hudroot, false);
			Interface.ApplicationsUI = Object.Instantiate(Interface.ApplicationsUIPrefab, hudroot, false);
			Interface.EditGuildUI = Object.Instantiate(Interface.EditGuildUIPrefab, hudroot, false);
			Interface.AchievementUI = Object.Instantiate(Interface.AchievementUIPrefab, hudroot, false);
			Interface.AchievementPopup = Object.Instantiate(Interface.AchievementPopupPrefab, hudroot, false);
			FixBkgMaterial(Interface.NoGuildUI);
			FixBkgMaterial(Interface.SearchGuildUI);
			FixBkgMaterial(Interface.CreateGuildUI);
			FixBkgMaterial(Interface.GuildManagementUI);
			FixBkgMaterial(Interface.ApplicationsUI);
			FixBkgMaterial(Interface.EditGuildUI);
			FixBkgMaterial(Interface.AchievementUI);
			FixBkgMaterial(Interface.AchievementPopup);
			UnifiedPopup.instance.transform.SetAsLastSibling();
		}

		private static void FixBkgMaterial(GameObject ui)
		{
			if (FejdStartupFixMaterial.originalMaterial == null) return;
			Transform? background = Utils.FindChild(ui.transform, "Background");
			if (background is not null)
			{
				Image? uiBkg = background.GetComponent<Image>();
				uiBkg.material = FejdStartupFixMaterial.originalMaterial;
			}

			Transform? backgroundBack = Utils.FindChild(ui.transform, "BackgroundBack");
			if (backgroundBack is null) return;

			Image? uiBkgBack = backgroundBack.GetComponent<Image>();
			uiBkgBack.material = FejdStartupFixMaterial.originalMaterial;
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
