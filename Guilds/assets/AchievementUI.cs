using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class AchievementUI : MonoBehaviour
	{
		public Canvas canvas = null!;
		public RectTransform root = null!;
		public Image BackgroundBack = null!;
		public Button ButtonClose = null!;
		public Image ButtonCloseImage = null!;
		public TextMeshProUGUI ButtonCloseTMP = null!;
		public Image Background = null!;
		public RectTransform Header = null!;
		public HorizontalLayoutGroup HeaderHLG = null!;
		public Image HeaderImageLeft = null!;
		public Image HeaderImageRight = null!;
		public TextMeshProUGUI HeaderTMP = null!;
		public RectTransform content = null!;
		public ScrollRect contentScrollRect = null!;
		public Image contentScrollRectImage = null!;
		public RectTransform contentList = null!;
		public VerticalLayoutGroup contentListVLG = null!;
		public ContentSizeFitter contentListContentSizeFitter = null!;
		public TextMeshProUGUI guildLevel = null!;
		public TextMeshProUGUI achievementsCompleted = null!;

		[Header("Placeholder")]
		public RectTransform RowPlaceholder = null!;
		public AchievementUIRow PlaceholderInstance = null!;
		public RectTransform PlaceholderBack = null!;
		public RectTransform PlaceholderBackBg = null!;
		public RectTransform PlaceholderBackBgImg = null!;
		public RectTransform PlaceholderBackBorder = null!;
		public RectTransform PlaceholderBackBorderImg = null!;
		public RectTransform PlaceholderContent = null!;
		public RectTransform PlaceholderContentLeftCol = null!;
		public HorizontalLayoutGroup PlaceholderContentLeftColHLG = null!;
		public RectTransform PlaceholderContentLeftColIcon = null!;
		public Image PlaceholderContentLeftColIconImage = null!;
		public Image PlaceholderContentLeftColIconImageSelected = null!;
		public Image PlaceholderContentLeftColIconImageBorder = null!;

		public RectTransform PlaceholderContentRightColIcon = null!;
		public Image PlaceholderContentRightColIconImage = null!;
		public Image PlaceholderContentRightColIconImageSelected = null!;
		public Image PlaceholderContentRightColIconImageBorder = null!;
		public TextMeshProUGUI PlaceholderContentRightColIconGuildLevelTMP = null!;
		public RectTransform PlaceholderContentAchievementInformationRect = null!;
		public RectTransform PlaceholderContentAchievementInformationHeader = null!;
		public HorizontalLayoutGroup PlaceholderContentAchievementInformationHeaderHLG = null!;
		public TextMeshProUGUI PlaceholderContentAchievementInformationHeaderTitle = null!;
		public TextMeshProUGUI PlaceholderContentAchievementInformationHeaderDate = null!;
		public TextMeshProUGUI PlaceholderContentAchievementInformationHeaderDesc = null!;
		public RectTransform PlaceholderContentAchievementInformationProgressBar = null!;
		public Image PlaceholderContentAchievementInformationProgressBarBg = null!;
		public Image PlaceholderContentAchievementInformationProgressBarGreen = null!;
		public TextMeshProUGUI PlaceholderContentAchievementInformationProgressBarText = null!;
		public RectTransform PlaceholderContentRightCol = null!;
		public Image PlaceholderContentRightColBg = null!;

		[Header("Content - Scroll")]
		public Scrollbar contentScrollbar = null!;

		public RectTransform contentScrollbarSlidingArea = null!;
		public Image contentScrollbarImage = null!;
		public RectTransform contentScrollbarHandle = null!;
		public Image contentScrollbarHandleImage = null!;

		private List<AchievementUIRow> _activeRows = new();
		private Queue<AchievementUIRow> _pooledRows = new();

		public void OnEnable()
		{
			UpdateRows();
		}

		public void UpdateRows()
		{
			if (gameObject.activeSelf)
			{
				if (API.GetOwnGuild() is null)
				{
					Interface.SwitchUI(Interface.NoGuildUI);
					return;
				}

				PopulateRows(API.GetOwnGuild()!.Achievements);
			}
		}

		public void PopulateRows(Dictionary<string, AchievementData> achievements)
		{
			int completed = 0;

			foreach (AchievementUIRow? row in _activeRows)
			{
				row.gameObject.SetActive(false);
				_pooledRows.Enqueue(row);
			}

			_activeRows.Clear();

			foreach (KeyValuePair<string, AchievementConfig> kv in Achievements.AllAchievementConfigs())
			{
				if (achievements.TryGetValue(kv.Key, out AchievementData? data))
				{
					if (data.completed.Count >= kv.Value.progress.Count)
					{
						++completed;
					}
				}

				if (kv.Value.first && (data?.completed.Count ?? 0) < 1 && GuildList.guildList.Values.Any(g => g.Achievements.TryGetValue(kv.Key, out AchievementData d) && d.completed.Count > 0))
				{
					continue;
				}
				
				AchievementUIRow row = GetRow();
				row.Setup(kv.Value, data);
				_activeRows.Add(row);
			}

			guildLevel.text = Localization.instance.Localize("$guilds_guildlevel", API.GetOwnGuild()!.General.level.ToString());
			achievementsCompleted.text = Localization.instance.Localize("$guilds_achievements_completed", completed.ToString());
		}

		private AchievementUIRow GetRow()
		{
			AchievementUIRow row = _pooledRows.Count > 0 ? _pooledRows.Dequeue() : Instantiate(RowPlaceholder.gameObject, contentList).GetComponent<AchievementUIRow>();
			row.gameObject.SetActive(true);
			return row;
		}

		private void ReturnRowToPool(AchievementUIRow row)
		{
			row.gameObject.SetActive(false);
			_pooledRows.Enqueue(row);
		}

		public void OnButtonClosed_Clicked()
		{
			Interface.HideUI();
		}

		public void OnButtonGoBack_Clicked()
		{
			Interface.SwitchUI(Interface.GuildManagementUI);
		}
	}
}
