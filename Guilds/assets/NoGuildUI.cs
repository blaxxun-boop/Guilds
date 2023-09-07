using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class NoGuildUI : MonoBehaviour
	{
		[Header("Root")]
		public RectTransform root = null!;
		public Image Background = null!;
		public Image BackgroundBackImg = null!;
		
		[Header("Close Button")]
		public Button ButtonClose = null!;
		public Image ButtonCloseImage = null!;
		public RectTransform ButtonCloseRect = null!;
		public TextMeshProUGUI ButtonCloseText = null!;

		[Header("Content")]
		public RectTransform content = null!;
		public VerticalLayoutGroup contentVLG = null!;
		public TextMeshProUGUI contentText = null!;

		[Header("Content Panel")]
		public RectTransform panelRect = null!;
		public HorizontalLayoutGroup panelHLG = null!;
		public Button panelButtonCreate = null!;
		public Image panelButtonCreateImg = null!;
		public TextMeshProUGUI panelButtonCreateText = null!;
		public Button panelButtonConnect = null!;
		public Image panelButtonConnectImg = null!;
		public TextMeshProUGUI panelButtonConnectText = null!;
		public Image Border = null!;

		public void OnEnable()
		{
			if (API.GetOwnAppliedGuild() is { } guild)
			{
				contentText.text = Localization.instance.Localize("$guilds_pending_application", guild.Name);
			}
			else
			{
				contentText.text = Localization.instance.Localize("$guilds_noguild_message");
			}
		}

		public void OnButtonClosed_Clicked()
		{
			Interface.HideUI();
		}

		public void OnButtonCreate_Clicked()
		{
			if (Guilds.allowGuildCreation.Value == Toggle.Off && !Guilds.configSync.IsAdmin)
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_creation_admin_only", "$guilds_creation_admin_only_details", (PopupButtonCallback)UnifiedPopup.Pop));
				return;
			}
			Interface.SwitchUI(Interface.CreateGuildUI);
		}

		public void OnButtonConnect_Clicked()
		{
			Interface.SwitchUI(Interface.SearchGuildUI);
		}
	}
}
