using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class ApplicationsUIRow : MonoBehaviour
	{
		private PlayerReference applicant;
		private Application application = null!;
		private int guildId = -1;

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

		[Header("WhyMe Area")]
		public RectTransform whyMeArea = null!;
		public VerticalLayoutGroup whyMeAreaVlg = null!;
		public Button whyMeButton = null!;
		public Image whyMeButtonImg = null!;
		public TextMeshProUGUI whyMeAreaText = null!;

		[Header("Applied Area")]
		public RectTransform appliedAreaTransform = null!;
		public VerticalLayoutGroup appliedAreaLayoutGroup = null!;
		public RectTransform statusTextTransform = null!;
		public TextMeshProUGUI statusText = null!;

		[Header("Action Area")]
		public RectTransform actionAreaTransform = null!;
		public HorizontalLayoutGroup actionAreaLayoutGroup = null!;
		public Button actionAreaAcceptMemberButton = null!;
		public Image actionAreaAcceptMemberButtonImage = null!;
		public Image actionAreaAcceptMemberButtonCheckmark = null!;

		public Button actionAreaDenyMemberButton = null!;
		public Image actionAreaDenyMemberButtonImage = null!;
		public TextMeshProUGUI actionAreaDenyMemberButtonTextTMP = null!;

		public ApplicationsUI applicationsUI = null!;

		public void Setup(ApplicationsUI applicationsUI, PlayerReference applicant, Application application)
		{
			this.applicationsUI = applicationsUI;
			this.applicant = applicant;
			this.application = application;

			nameText.text = applicant.name;
			whyMeAreaText.text = application.description;
			statusText.text = Localization.instance.Localize("$guilds_apply_applied", Tools.GetHumanFriendlyTime((int)(DateTime.Now - application.applied).TotalSeconds));

			guildId = API.GetOwnGuild()!.General.id;
		}

		public void OnDenyMember_ButtonClicked()
		{
			if (API.GetGuild(guildId) is not { } guild)
			{
				return;
			}

			API.RemovePlayerApplication(applicant, guild);
			
			Guilds.SendMessageToPlayer(applicant, Localization.instance.Localize("$guilds_application_declined", guild.Name));
		}

		public void OnAcceptMember_ButtonClicked()
		{
			if (API.GetGuild(guildId) is not { } guild)
			{
				return;
			}

			if (Guilds.maximumGuildMembers.Value > 0 && guild.Members.Count >= Guilds.maximumGuildMembers.Value)
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_guild_full", "$guilds_guild_full_details", (PopupButtonCallback)UnifiedPopup.Pop));
				return;
			}
			
			API.RemovePlayerApplication(applicant, guild);
			guild = API.GetGuild(guildId);
			if (guild is null)
			{
				return;
			}

			API.AddPlayerToGuild(applicant, guild);

			Guilds.SendMessageToPlayer(applicant, Localization.instance.Localize("$guilds_application_accepted", guild.Name));
		}

		public void OnWhyMe_ButtonClicked()
		{
			applicationsUI.popupRootRect.gameObject.SetActive(true);
			applicationsUI.popupHeaderText.text = applicant.name;
			applicationsUI.popupViewportContentBodyText.text = application.description;
		}
	}
}
