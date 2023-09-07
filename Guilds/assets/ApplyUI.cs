using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class ApplyUI : MonoBehaviour
	{
		public Guild guild = null!;

		[Header("Root Objects")]
		public RectTransform root = null!;
		public Image Background = null!;
		public Image BackgroundBack = null!;

		[Header("Close Button")]
		public Image BackgroundBackButtonImage = null!;
		public Button BackgroundBackButton = null!;
		public TextMeshProUGUI BackgroundBackButtonTextMeshProUGui = null!;

		[Header("Header Area")]
		public RectTransform header = null!;
		public HorizontalLayoutGroup headerHLayoutGroup = null!;

		public Image headerLeftImage = null!;
		public Image headerRightImage = null!;

		public TextMeshProUGUI headerTMP = null!;

		[Header("Content Area")]
		public RectTransform content = null!;
		public HorizontalLayoutGroup contentHLayoutGroup = null!;

		[Header("Content - Text Entry Area")]
		public RectTransform textArea = null!;
		public VerticalLayoutGroup textAreaVLayoutGroup = null!;
		public RectTransform textAreaContainerRect = null!;
		public TMP_InputField textAreaInputField = null!;
		public Image textAreaInputFieldBkg = null!;

		public TextMeshProUGUI textareaInputPlaceholderText = null!;
		public TextMeshProUGUI textAreaInputText = null!;

		[Header("Content - Action Area")]
		public RectTransform actionAreaRect = null!;
		public HorizontalLayoutGroup actionAreaHLayoutGroup = null!;
		public Button actionAreaButtonCancel = null!;
		public Image actionAreaButtonCancelImg = null!;
		public TextMeshProUGUI actionAreaButtonCancelText = null!;
		public Button actionAreaButtonApply = null!;
		public Image actionAreaButtonApplyImg = null!;
		public TextMeshProUGUI actionAreaButtonApplyText = null!;

		public void Setup(Guild guild)
		{
			this.guild = guild;
			headerTMP.text = Localization.instance.Localize("$guilds_applyguildui_title", guild.Name);
		}

		public void OnButtonClose_Clicked()
		{
			Interface.HideUI();
		}

		public void OnButtonCancel_Clicked()
		{
			Interface.SwitchUI(Interface.SearchGuildUI);
		}

		public void OnButtonApply_Clicked()
		{
			API.ApplyToGuild(PlayerReference.forOwnPlayer(), textAreaInputField.text, guild);
			Interface.HideUI();
		}

		public void OnTextAreaInputField_ValueChanged()
		{

		}

		public void OnTextAreaInputField_EndEdit()
		{

		}

		public void OnTextAreaInputField_Select()
		{

		}

		public void OnTextAreaInputField_Deselect()
		{

		}
	}
}
