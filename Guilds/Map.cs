using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using Splatform;
using UnityEngine;

namespace Guilds;

public static class Map
{
	private static Sprite guildMapPlayerIcon = null!;
	private static Sprite guildMapPingIcon = null!;
	private static readonly ConditionalWeakTable<Chat.WorldTextInstance, object> guildPingTexts = new();
	private static readonly Color defaultColor = new(1f, 0.7176471f, 0.3602941f);

	public static void Init()
	{
		guildMapPlayerIcon = Tools.loadSprite("guildPlayerIcon.png", 64, 64);
		guildMapPingIcon = Tools.loadSprite("guildMapPingIcon.png", 64, 64);
	}

	[HarmonyPatch(typeof(Game), nameof(Game.RequestRespawn))]
	private static class UpdateGuildIcon
	{
		private static void Postfix()
		{
			UpdateMapPinColor();
		}
	}

	[HarmonyPatch(typeof(Chat), nameof(Chat.SendPing))]
	private static class RestrictPingsToGuildOnModifierHeld
	{
		[UsedImplicitly]
		private static bool RestrictBroadcast(ZRoutedRpc instance, long targetPeerId, string methodName, params object[] parameters)
		{
			if (API.GetOwnGuild() is { } guild && targetPeerId == ZRoutedRpc.Everybody && Guilds.guildPingHotkey.Value.IsPressed())
			{
				foreach (ZNet.PlayerInfo player in ZNet.instance.m_players)
				{
					if (guild.Members.ContainsKey(PlayerReference.fromPlayerInfo(player)))
					{
						instance.InvokeRoutedRPC(player.m_characterID.UserID, "Guilds MapPing", parameters);
					}
				}

				return true;
			}

			return false;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable, ILGenerator ilg)
		{
			MethodInfo routedRPC = AccessTools.DeclaredMethod(typeof(ZRoutedRpc), nameof(ZRoutedRpc.InvokeRoutedRPC), new[] { typeof(long), typeof(string), typeof(object[]) });
			MethodInfo routedRPCInstance = AccessTools.DeclaredPropertyGetter(typeof(ZRoutedRpc), nameof(ZRoutedRpc.instance));

			List<CodeInstruction> instructions = instructionsEnumerable.ToList();

			int methodEndIndex = instructions.FindIndex(i => i.Calls(routedRPC));
			int methodStartIndex = instructions.FindLastIndex(methodEndIndex, i => i.Calls(routedRPCInstance));

			Label skip = ilg.DefineLabel();
			instructions[methodEndIndex + 1].labels.Add(skip);

			// Repeat all instructions for method call, then skip original if restricted
			instructions.InsertRange(methodStartIndex, instructions.Skip(methodStartIndex).Take(methodEndIndex - methodStartIndex).Concat(new []
			{
				new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(RestrictPingsToGuildOnModifierHeld), nameof(RestrictBroadcast))),
				new CodeInstruction(OpCodes.Brtrue, skip),
			}).ToArray());

			return instructions;
		}
	}

	[HarmonyPatch(typeof(Chat), nameof(Chat.RPC_ChatMessage))]
	private class ClearGuildPing
	{
		public static void Prefix(Chat __instance, long sender)
		{
			if (__instance.FindExistingWorldText(sender) is { } text && guildPingTexts.Remove(text) && Minimap.instance)
			{
				for (int i = 0; i < Minimap.instance.m_tempShouts.Count; ++i)
				{
					Minimap.PinData pingPin = Minimap.instance.m_pingPins[i];
					Chat.WorldTextInstance tempShout = Minimap.instance.m_tempShouts[i];
					if (tempShout == text)
					{
						pingPin.m_icon = Minimap.instance.GetSprite(Minimap.PinType.Ping);
						if (pingPin.m_iconElement)
						{
							pingPin.m_iconElement.sprite = pingPin.m_icon;
						}
					}
				}
			}
		}
	}

	public static void onMapPing(long senderId, Vector3 position, int type, UserInfo name, string text)
	{
		if (API.GetOwnGuild() is not { } guild)
		{
			return;
		}

		ColorUtility.TryParseHtmlString(guild.General.color, out Color color);

		Chat.instance.RPC_ChatMessage(senderId, position, type, name, text);
		Chat.WorldTextInstance worldText = Chat.instance.FindExistingWorldText(senderId);
		worldText.m_textMeshField.color = color;
		guildPingTexts.Add(worldText, Array.Empty<object>());
	}

	internal static void UpdateMapPinColor()
	{
		if (API.GetOwnGuild() is not { } guild)
		{
			return;
		}

		ColorUtility.TryParseHtmlString(guild.General.color, out Color color);

		Color[]? pixels = Tools.loadTexture("guildPlayerIcon.png").GetPixels();
		for (int i = 0; i < pixels.Length; ++i)
		{
			if (pixels[i].r > 0.5 && pixels[i].b < 0.5 && pixels[i].g < 0.5)
			{
				pixels[i] = color;
			}
		}
		guildMapPlayerIcon.texture.SetPixels(pixels);
		guildMapPlayerIcon.texture.Apply();

		pixels = Tools.loadTexture("guildMapPingIcon.png").GetPixels();
		for (int i = 0; i < pixels.Length; ++i)
		{
			if (pixels[i].r > 0.5 && pixels[i].b < 0.5 && pixels[i].g < 0.5)
			{
				pixels[i].b = color.b;
				pixels[i].g = color.g;
				pixels[i].r = color.r;
			}
		}
		guildMapPingIcon.texture.SetPixels(pixels);
		guildMapPingIcon.texture.Apply();
	}

	[HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePlayerPins))]
	private class ChangeGuildMemberPin
	{
		[HarmonyPriority(Priority.HigherThanNormal)]
		private static void Postfix(Minimap __instance)
		{
			for (int index = 0; index < __instance.m_tempPlayerInfo.Count; ++index)
			{
				Minimap.PinData playerPin = __instance.m_playerPins[index];
				ZNet.PlayerInfo playerInfo = __instance.m_tempPlayerInfo[index];
				if (playerPin.m_name == playerInfo.m_name)
				{
					bool changed = false;
					if (API.GetOwnGuild()?.Members.ContainsKey(PlayerReference.fromPlayerInfo(playerInfo)) == true)
					{
						playerPin.m_icon = guildMapPlayerIcon;
						changed = true;
					}
					else if (playerPin.m_icon == guildMapPlayerIcon)
					{
						playerPin.m_icon = __instance.GetSprite(Minimap.PinType.Player);
						changed = true;
					}
					if (changed && playerPin.m_iconElement)
					{
						playerPin.m_iconElement.sprite = playerPin.m_icon;
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePingPins))]
	private class ChangeGuildMemberPing
	{
		private static void Postfix(Minimap __instance)
		{
			for (int i = 0; i < __instance.m_tempShouts.Count; ++i)
			{
				Minimap.PinData pingPin = __instance.m_pingPins[i];
				Chat.WorldTextInstance tempShout = __instance.m_tempShouts[i];
				if (guildPingTexts.TryGetValue(tempShout, out _))
				{
					pingPin.m_icon = guildMapPingIcon;
					if (pingPin.m_iconElement)
					{
						pingPin.m_iconElement.sprite = pingPin.m_icon;
					}
				}
			}
		}
	}

	internal static void onUpdatePosition(long senderId, Vector3 position)
	{
		List<ZNet.PlayerInfo> playerInfos = new();
		foreach (ZNet.PlayerInfo playerInfo in ZNet.instance.m_players)
		{
			ZNet.PlayerInfo info = playerInfo;
			if (info.m_characterID.UserID == senderId)
			{
				info.m_position = position;
			}
			playerInfos.Add(info);
		}
		ZNet.instance.m_players = playerInfos;
	}
	
	[HarmonyPatch]
	private class PreservePlayerPosition
	{
		private static IEnumerable<MethodInfo> TargetMethods() => new[]
		{
			AccessTools.DeclaredMethod(typeof(ZNet), nameof(ZNet.RPC_PlayerList)),
			AccessTools.DeclaredMethod(typeof(ZNet), nameof(ZNet.UpdatePlayerList)),
		};

		private static void Prefix(ZNet __instance, out Dictionary<long, Vector3> __state)
		{
			__state = new Dictionary<long, Vector3>();

			if (API.GetOwnGuild() is not {} guild)
			{
				return;
			}

			foreach (ZNet.PlayerInfo playerInfo in __instance.m_players.Where(p => guild.Members.ContainsKey(PlayerReference.fromPlayerInfo(p))))
			{
				__state[playerInfo.m_characterID.UserID] = playerInfo.m_position;
			}
		}

		private static void Postfix(ZNet __instance, Dictionary<long, Vector3> __state)
		{
			if (API.GetOwnGuild() is not {} guild)
			{
				return;
			}

			List<ZNet.PlayerInfo> playerInfos = new();
			foreach (ZNet.PlayerInfo playerInfo in __instance.m_players)
			{
				ZNet.PlayerInfo info = playerInfo;
				if (guild.Members.ContainsKey(PlayerReference.fromPlayerInfo(info)) && info.m_characterID != Player.m_localPlayer?.GetZDOID())
				{
					if (__state.TryGetValue(playerInfo.m_characterID.UserID, out Vector3 position))
					{
						if (!playerInfo.m_publicPosition)
						{
							info.m_position = position;
						}
					}
					info.m_publicPosition = true;
				}
				playerInfos.Add(info);
			}
			__instance.m_players = playerInfos;
		}
	}
}
