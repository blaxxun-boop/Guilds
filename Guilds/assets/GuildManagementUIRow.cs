using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class GuildManagementUIRow : MonoBehaviour
	{
		public PlayerReference player;

		[Header("Row Root")]
		public RectTransform rowRootTransform = null!;
		public GameObject rowRootGameObject = null!;
		public RectTransform back = null!;
		public Image backImage = null!;
		public Image borderImage = null!;

		[Header("Content Area")]
		public RectTransform contentAreaRectTransform = null!;
		public HorizontalLayoutGroup contentAreaHorizontalLayoutGroup = null!;

		[Header("Name Area")]
		public RectTransform nameAreaTransform = null!;
		public VerticalLayoutGroup nameAreaVerticalLayoutGroup = null!;
		public TextMeshProUGUI nameText = null!;

		[Header("Rank Area")]
		public RectTransform rankAreaTransform = null!;
		public VerticalLayoutGroup rankAreaVerticalLayoutGroup = null!;
		public TMP_Dropdown rankDropdown = null!;
		public Image rankDropdownImage = null!;
		public TextMeshProUGUI rankDropdownLabel = null!;
		public Image rankDropdownArrow = null!;
		public RectTransform rankDropdownTemplate = null!;
		public ScrollRect rankDropdownTemplateScrollRect = null!;
		public Image rankDropdownTemplateScrollRectImage = null!;
		public RectTransform rankDropdownTemplateViewport = null!;
		public Image rankDropdownTemplateViewportImage = null!;
		public RectTransform rankDropdownTemplateContent = null!;
		public RectTransform rankDropdownTemplateItem = null!;
		public Image rankDropdownTemplateItemBackgroundImage = null!;
		public Image rankDropdownTemplateItemCheckmarkImage = null!;
		public TextMeshProUGUI rankDropdownTemplateItemLabel = null!;

		[Header("Rank Dropdown Scrollbar")]
		public RectTransform rankScrollbarTransform = null!;
		public Scrollbar rankScrollbar = null!;
		public Image rankScrollbarImage = null!;
		public RectTransform slidingArea = null!;
		public RectTransform handle = null!;
		public Image handleImage = null!;

		[Header("Online Area")]
		public RectTransform onlineAreaTransform = null!;
		public VerticalLayoutGroup onlineAreaLayoutGroup = null!;
		public RectTransform statusTextTransform = null!;
		public TextMeshProUGUI statusText = null!;

		[Header("Action Area")]
		public RectTransform actionAreaTransform = null!;
		public VerticalLayoutGroup actionAreaLayoutGroup = null!;
		public Button removeMemberButton = null!;
		public Image actionAreaButtonImage = null!;
		public TextMeshProUGUI actionAreaButtonText = null!;

		public void Awake()
		{
			rankDropdown.options = ((Ranks[])typeof(Ranks).GetEnumValues()).Select(rank => new TMP_Dropdown.OptionData(Localization.instance.Localize("$guilds_rank_" + rank.ToString().ToLower()))).ToList();
		}

		public void Setup(KeyValuePair<PlayerReference, GuildMember> memberData)
		{
			nameText.text = memberData.Key.name;
			rankDropdown.SetValueWithoutNotify((int)memberData.Value.rank);
			statusText.text = ZNet.instance.m_players.Any(p => PlayerReference.fromPlayerInfo(p) == memberData.Key) ? Localization.instance.Localize("$guilds_online") : Localization.instance.Localize("$guilds_last_online", Tools.GetHumanFriendlyTime((int)(DateTime.Now - memberData.Value.lastOnline).TotalSeconds));
			player = memberData.Key;

			rankDropdown.interactable = API.GetPlayerRank(PlayerReference.forOwnPlayer()) is Ranks.Leader or Ranks.Coleader;
			removeMemberButton.gameObject.SetActive(API.GetPlayerRank(PlayerReference.forOwnPlayer()) is Ranks.Leader or Ranks.Coleader or Ranks.Officer);
		}

		public void OnRemoveMember_ButtonClicked()
		{
			if (player == PlayerReference.forOwnPlayer())
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_kicked_self", "$guilds_kicked_self_details", UnifiedPopup.Pop));
				return;
			}

			if (API.GetPlayerRank(player) <= API.GetPlayerRank(PlayerReference.forOwnPlayer()))
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_higher_rank_kicked", Localization.instance.Localize("$guilds_higher_rank_kicked_details", player.name), UnifiedPopup.Pop));
				return;
			}

			UnifiedPopup.Push(new YesNoPopup("$guilds_confirm_kick", Localization.instance.Localize("$guilds_confirm_kick_details", player.name), () =>
			{
				API.RemovePlayerFromGuild(player);
				
				Guilds.SendMessageToPlayer(player, Localization.instance.Localize("$guilds_kicked_out", API.GetOwnGuild()!.Name));

				UnifiedPopup.Pop();
			}, UnifiedPopup.Pop));
		}

		// ReSharper disable once RedundantAssignment
		public void OnRankDropdown_ValueChanged(int value)
		{
			value = rankDropdown.value; // why the fuck does unity convert this to a CachedInvokableCall always invoked with value = 0???

			if (value == (int)API.GetPlayerRank(player))
			{
				return;
			}

			if (player == PlayerReference.forOwnPlayer())
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_own_rank_changed", "$guilds_own_rank_changed_details", UnifiedPopup.Pop));
				rankDropdown.value = (int)API.GetPlayerRank(player);
				return;
			}

			if (API.GetPlayerRank(PlayerReference.forOwnPlayer()) is Ranks.Leader)
			{
				if ((Ranks)value is Ranks.Leader)
				{
					rankDropdown.value = (int)API.GetPlayerRank(player);

					UnifiedPopup.Push(new YesNoPopup("$guilds_transfer_guild", Localization.instance.Localize("$guilds_transfer_guild_details", player.name), () =>
					{
						API.UpdatePlayerRank(PlayerReference.forOwnPlayer(), Ranks.Coleader);
						API.UpdatePlayerRank(player, (Ranks)value);

						UnifiedPopup.Pop();
					}, UnifiedPopup.Pop));
				}
				else
				{
					API.UpdatePlayerRank(player, (Ranks)value);
				}

				return;
			}

			if (API.GetPlayerRank(player) is Ranks.Leader or Ranks.Coleader)
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_higher_rank_changed", "$guilds_higher_rank_changed_details", UnifiedPopup.Pop));
				rankDropdown.value = (int)API.GetPlayerRank(player);
				return;
			}

			if ((Ranks)value is Ranks.Leader or Ranks.Coleader)
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_new_rank_too_high", "$guilds_new_rank_too_high_details", UnifiedPopup.Pop));
				rankDropdown.value = (int)API.GetPlayerRank(player);
				return;
			}

			API.UpdatePlayerRank(player, (Ranks)value);
		}
	}
}
