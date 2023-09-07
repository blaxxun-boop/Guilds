using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class SearchGuildUIRow : MonoBehaviour
	{
		private Guild guild = null!;
		private ApplyUI applyUI = null!;
		
		[Header("Row Root")]
		public RectTransform rowRootTransform = null!;
		public GameObject rowRootGameObject = null!;
		public RectTransform back = null!;
		public Image backImage = null!;
		public Image borderImage = null!;

		[Header("Content Area")]
		public RectTransform contentAreaRectTransform = null!;

		[Header("Left Column")]
		public RectTransform leftColumnRect = null!;
		public HorizontalLayoutGroup leftColumnHLayoutGroup = null!;
		[Header("Left Column - Icon Container")]
		public RectTransform iconContainerRect = null!;
		public Image iconContainerImg = null!;
		public RectTransform guildIconImgRect = null!;
		public RectTransform guildIconBorderRect = null!;
		public Image guildIconBorderImg = null!;
		public Image guildIconImg = null!;
		public Button guildIconButton = null!;

		[Header("Left Column - Naming Area")]
		public RectTransform namingAreaRect = null!;
		public VerticalLayoutGroup namingAreaVLayoutGroup = null!;
		public RectTransform levelTextRect = null!;
		public TextMeshProUGUI levelText = null!;
		public RectTransform nameTextRect = null!;
		public TextMeshProUGUI nameText = null!;
		public RectTransform leaderTextRect = null!;
		public TextMeshProUGUI leaderText = null!;

		[Header("Right Column")]
		public RectTransform rightColumnRect = null!;
		public Image rightColumnBkg = null!;
		public RectTransform rightColumnInputFieldRect = null!;
		public TMP_InputField rightColumnInputField = null!;
		public TextMeshProUGUI rightColumnInputFieldPlaceholderText = null!;
		public TextMeshProUGUI rightColumnInputFieldText = null!;

		public void Setup(Guild guild, ApplyUI applyUI)
		{
			this.guild = guild;
			this.applyUI = applyUI;
			leaderText.text = Localization.instance.Localize("$guilds_rank_leader: ") + API.GetGuildLeader(guild).name;
			nameText.text = guild.Name;
			guildIconImg.sprite = Interface.GuildIcons.TryGetValue(guild.General.icon, out Sprite sprite) ? sprite : Interface.GuildIcons[1];
			levelText.text = Localization.instance.Localize("$guilds_level ") + guild.General.level;
			rightColumnInputField.text = guild.General.description;
		}

		public void OnGuildIconButton_Clicked()
		{
			applyUI.Setup(guild);
		}
	}
}
