using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class CreateGuildUI : MonoBehaviour
	{
		private int guildIconId = 0;

		[Header("Root Objects")]
		public Canvas canvas = null!;
		public RectTransform root = null!; // This is equal to "CreateGuild" rect in the heirarchy
		public Image Background = null!;
		public Image BackgroundBack = null!;

		[Header("Close Button")]
		public Image BackgroundBackButtonImage = null!;
		public Button BackgroundBackButton = null!;
		public TextMeshProUGUI BackgroundBackButtonTextMeshProUGui = null!;

		[Header("GuildsColorPicker - Instance")]
		public GameObject guildsColorPicker = null!;
		public RectTransform guildsColorPickerRect = null!;
		public GuildColorPicker guildsColorPickerInstance = null!; // Please note, this is the script attached to the panel, not the root gameobject.

		[Header("Header Area")]
		public RectTransform header = null!;
		public HorizontalLayoutGroup headerHLayoutGroup = null!;

		public Image headerLeftImage = null!;
		public Image headerRightImage = null!;

		public TextMeshProUGUI headerTMP = null!;

		[Header("Content Area")]
		public RectTransform content = null!; // Parent Rect of all columns

		[Header("Column 1")]
		public RectTransform Col1 = null!;
		public VerticalLayoutGroup Col1VLayoutGroup = null!;
		public RectTransform Col1IconContainerRect = null!;
		public Image Col1IconContainerIcon = null!;
		public RectTransform Col1IconContainerIconRect = null!;
		public Image Col1IconContainerBorder = null!;
		public RectTransform Col1IconContainerBorderRect = null!;
		public Button Col1ButtonSelect = null!;
		public Image Col1ButtonSelectImage = null!;
		public TextMeshProUGUI Col1ButtonTMP = null!;

		[Header("Column 2")]
		public RectTransform Col2 = null!;
		public VerticalLayoutGroup Col2VLG = null!;
		public RectTransform Col2InputFieldGuildNameRect = null!;
		public TMP_InputField Col2InputFieldGuildName = null!;
		public TextMeshProUGUI Col2InputFieldGuildNamePlaceHolder = null!;
		public TextMeshProUGUI Col2InputFieldGuildNameText = null!;
		public TMP_InputField Col2InputFieldGuildDescription = null!;
		public RectTransform Col2InputFieldGuildDescriptionRect = null!;
		public TextMeshProUGUI Col2InputFieldGuildDescriptionPlaceholder = null!;
		public TextMeshProUGUI Col2InputFieldGuildDescriptionText = null!;

		public RectTransform Col2ButtonCreateRect = null!;
		public Image Col2ButtonCreateImg = null!;
		public Button Col2ButtonCreate = null!;
		public TextMeshProUGUI Col2ButtonCreateTMP = null!;

		public RectTransform Col3 = null!;

		public Image Col3Icon = null!;
		public TextMeshProUGUI Col3RequirementsText = null!;

		[Header("GuildIconUI - Instance")]
		public GameObject guildIconUI = null!;
		public RectTransform guildIconUIRect = null!;
		public GuildIconUI guildIconUIInstance = null!;
		
		public Image guildsColorPlaceholderImg = null!;

		public void Awake()
		{
			Col2InputFieldGuildName.characterValidation = TMP_InputField.CharacterValidation.CustomValidator;
			Col2InputFieldGuildName.inputValidator = ScriptableObject.CreateInstance<Tools.NameValidator>();
			Col3RequirementsText.gameObject.SetActive(false);
			guildsColorPickerInstance.chosenColorPreview = guildsColorPlaceholderImg;
		}

		public void OnButtonClosed_Clicked()
		{
			Interface.HideUI();
		}

		public void OnButtonSelectIcon_Clicked()
		{
			guildIconUIInstance.selectedGuildIcon = idx =>
			{
				guildIconId = idx;
				Col1IconContainerIcon.sprite = Interface.GuildIcons[idx];
			};
		}

		public void OnButtonCreate_Clicked()
		{
			if (guildIconId == 0)
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_no_icon_selected", "$guilds_no_icon_selected_details", (PopupButtonCallback)UnifiedPopup.Pop));
				return;
			}

			if (Col2InputFieldGuildName.text.Trim().Length > Guilds.maximumGuildNameLength.Value)
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_name_too_long",Localization.instance.Localize("$guilds_name_too_long_details", Guilds.maximumGuildNameLength.Value.ToString()), (PopupButtonCallback)UnifiedPopup.Pop));
				return;
			}
			
			if (Col2InputFieldGuildName.text.Trim().Length < Guilds.minimumGuildNameLength.Value)
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_name_too_short", Localization.instance.Localize("$guilds_name_too_short_details", Guilds.minimumGuildNameLength.Value.ToString()), (PopupButtonCallback)UnifiedPopup.Pop));
				return;
			}

			if (API.CreateGuild(Col2InputFieldGuildName.text.Trim(), PlayerReference.forOwnPlayer()) is not { } newGuild)
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_name_taken", "$guilds_name_taken_details", (PopupButtonCallback)UnifiedPopup.Pop));
				return;
			}
			
			API.RemovePlayerApplication(PlayerReference.forOwnPlayer());

			newGuild.General.description = Col2InputFieldGuildDescription.text;
			newGuild.General.icon = guildIconId;
			newGuild.General.color = guildsColorPickerInstance.chosenColor;
			API.SaveGuild(newGuild);
			Interface.HideUI();
			Interface.GuildManagementUI.SetActive(true);
		}

		public void OnCol2InputField_GuildName_ValueChanged()
		{

		}

		public void OnCol2InputField_GuildName_EndEdit()
		{

		}

		public void OnCol2InputField_GuildName_Select()
		{

		}

		public void OnCol2InputField_GuildName_Deselect()
		{

		}

		public void OnCol2InputField_GuildDescription_ValueChanged()
		{

		}

		public void OnCol2InputField_GuildDescription_EndEdit()
		{

		}

		public void OnCol2InputField_GuildDescription_Select()
		{

		}

		public void OnCol2InputField_GuildDescription_Deselect()
		{

		}
	}
}
