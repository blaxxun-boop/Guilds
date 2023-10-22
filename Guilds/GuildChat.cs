using System;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Guilds;

public static class GuildChat
{
	private static string guildChatPlaceholder = null!;
	private static bool guildChatActive => Chat.instance && Chat.instance.m_input.transform.Find("Text Area/Placeholder").GetComponent<TextMeshProUGUI>().text == guildChatPlaceholder;

	[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
	public class AddChatCommands
	{
		private static void Postfix()
		{
			guildChatPlaceholder = Localization.instance.Localize("$guilds_guild_chat_placeholder");
			
			_ = new Terminal.ConsoleCommand("g", "toggles the guild chat on", (Terminal.ConsoleEvent)(args =>
			{
				if (Chat.instance == null)
				{
					return;
				}

				if (API.GetOwnGuild() is not { } guild)
				{
					args.Context.AddString("You are not in a guild.");

					return;
				}

				if (args.FullLine.Length > 2)
				{
					string message = args.FullLine.Substring(2);

					foreach (ZNet.PlayerInfo player in ZNet.instance.m_players)
					{
						if (player.m_characterID.UserID != 0 && guild.Members.ContainsKey(PlayerReference.fromPlayerInfo(player)))
						{
							ZRoutedRpc.instance.InvokeRoutedRPC(player.m_characterID.UserID, "Guilds ChatMessage", UserInfo.GetLocalUser(), message);
						}
					}
				}
				else
				{
					ToggleGuildsChat(!guildChatActive);
				}
			}));
		}
	}

	public static void ToggleGuildsChat(bool active)
	{
		if (Chat.instance)
		{
			TextMeshProUGUI placeholder = Chat.instance.m_input.transform.Find("Text Area/Placeholder").GetComponent<TextMeshProUGUI>();
			if (active)
			{
				placeholder.text = guildChatPlaceholder;
				Localization.instance.textMeshStrings[placeholder] = guildChatPlaceholder;
			}
			else if (placeholder.text == guildChatPlaceholder)
			{
				placeholder.text = Localization.instance.Localize("$chat_entertext");
				Localization.instance.textMeshStrings[placeholder] = "$chat_entertext";
			}
		}
	}

	[HarmonyPatch(typeof(Chat), nameof(Chat.Awake))]
	public class AddGuildChat
	{
		private static void Postfix(Chat __instance)
		{
			int insertIndex = Math.Max(0, __instance.m_chatBuffer.Count - 5);
			__instance.m_chatBuffer.Insert(insertIndex, "/g [text] Guild chat");
			__instance.m_chatBuffer.Insert(insertIndex, "/g Toggle guild chat");
			__instance.UpdateChat();
		}
	}

	[HarmonyPatch(typeof(Chat), nameof(Chat.InputText))]
	public class SendMessageToGuild
	{
		private static void Prefix(Chat __instance)
		{
			if (__instance.m_input.text.Length != 0 && guildChatActive && __instance.m_input.text[0] != '/')
			{
				__instance.m_input.text = "/g " + __instance.m_input.text;
			}
		}
	}

	[HarmonyPatch(typeof(Game), nameof(Game.Start))]
	private static class AddRPCs
	{
		private static void Postfix()
		{
			ZRoutedRpc.instance.Register<UserInfo, string>("Guilds ChatMessage", onChatMessageReceived);
			ZRoutedRpc.instance.Register<Vector3, int, UserInfo, string>("Guilds MapPing", Map.onMapPing);
			ZRoutedRpc.instance.Register<Vector3>("Guilds UpdatePosition", Map.onUpdatePosition);
		}
	}

	private static void onChatMessageReceived(long senderId, UserInfo name, string message)
	{
		string color = Guilds.guildColors.Value == Toggle.Off ? "#" + ColorUtility.ToHtmlStringRGBA(Guilds.guildChatColor.Value) : API.GetOwnGuild()!.General.color;
		Chat.instance.AddString("<color=orange>" + name.Name + $"</color>: <color={color}>" + message + "</color>");
		Chat.instance.m_hideTimer = 0f;
		ZDOID playerZDO = ZNet.instance.m_players.FirstOrDefault(p => p.m_characterID.UserID == senderId).m_characterID;
		if (playerZDO != ZDOID.None && ZNetScene.instance.FindInstance(playerZDO) is { } playerObject && playerObject.GetComponent<Player>() is { } player)
		{
			if (Minimap.instance && Player.m_localPlayer && Minimap.instance.m_mode == Minimap.MapMode.None && Vector3.Distance(Player.m_localPlayer.transform.position, player.GetHeadPoint()) > Minimap.instance.m_nomapPingDistance)
			{
				return;
			}
			Chat.instance.AddInworldText(playerObject, senderId, player.GetHeadPoint(), Talker.Type.Normal, name, $"<color={color}>" + message + "</color>");
		}
	}
}
