using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class EditGuildUI : MonoBehaviour
	{
		private int guildIconId;

		[Header("Root Objects")]
		public Canvas canvas = null!;
		public RectTransform root = null!; // This is equal to "EditGuild" rect in the heirarchy
		public Image Background = null!;
		public Image BackgroundBack = null!;

		[Header("GuildsColorPicker - Instance")]
		public GameObject guildsColorPicker = null!;
		public RectTransform guildsColorPickerRect = null!;
		public GuildColorPicker guildsColorPickerInstance = null!; // Please note, this is the script attached to the panel, not the root gameobject.
		
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

		public RectTransform Col2ButtonEditRect = null!;
		public Image Col2ButtonEditImg = null!;
		public Button Col2ButtonEdit = null!;
		public TextMeshProUGUI Col2ButtonEditTMP = null!;

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

		public void OnEnable()
		{
			Guild guild = API.GetOwnGuild()!;
			Col2InputFieldGuildName.text = guild.Name;
			Col2InputFieldGuildDescription.text = guild.General.description;
			Col1IconContainerIcon.sprite = API.GetGuildIcon(guild);
			guildIconId = guild.General.icon;
			guildsColorPickerInstance.chosenColor = guild.General.color;
			if (ColorUtility.TryParseHtmlString(guildsColorPickerInstance.chosenColor, out Color color))
			{
				guildsColorPlaceholderImg.color = color;
			}
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

		public void OnButtonEdit_Clicked()
		{
			Guild guild = API.GetOwnGuild()!;
			if (guild.General.description != Col2InputFieldGuildDescription.text)
			{
				guild.General.description = Col2InputFieldGuildDescription.text;
				API.SaveGuild(guild);
			}

			if (guild.General.icon != guildIconId)
			{
				guild.General.icon = guildIconId;
				API.SaveGuild(guild);
			}
			
			if (guild.General.color != guildsColorPickerInstance.chosenColor)
			{
				guild.General.color = guildsColorPickerInstance.chosenColor;
				API.SaveGuild(guild);
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

			if (guild.Name != Col2InputFieldGuildName.text && !API.RenameGuild(guild, Col2InputFieldGuildName.text.Trim()))
			{
				UnifiedPopup.Push(new WarningPopup("$guilds_name_taken", "$guilds_name_taken_details", (PopupButtonCallback)UnifiedPopup.Pop));
				return;
			}

			Interface.SwitchUI(Interface.GuildManagementUI);
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
