using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class AchievementUIRow : MonoBehaviour
	{
		[Header("Row Root")]
		public RectTransform rowRootTransform = null!;
		public AchievementUIRow rowInstance = null!;
		public GameObject rowRootGameObject = null!;
		public RectTransform back = null!;
		public Image backImage = null!;
		public Image borderImage = null!;
		public GameObject rowNotCompleted = null!;

		[Header("Content Area")]
		public RectTransform contentAreaRectTransform = null!;

		[Header("Left Column")]
		public RectTransform leftColumnRect = null!;
		public HorizontalLayoutGroup leftColumnHLayoutGroup = null!;
		[Header("Left Column - Icon Container")]
		public RectTransform achievementIconContainerRect = null!;
		public Image achievementIconContainerImg = null!;
		public RectTransform achievementIconImgRect = null!;
		public RectTransform achievementIconBorderRect = null!;
		public Image achievementIconBorderImg = null!;
		public Image achievementIconImg = null!;

		[Header("Right Column - Rank Area")]
		public RectTransform rightCol = null!;
		public Image rightColBg = null!;
		public RectTransform rightColIconContainer = null!;
		public Image rightColIconContainerSelectedIcon = null!;
		public Image rightColIconcontainerBorder = null!;
		public RectTransform guildLevelRect = null!;
		public TextMeshProUGUI guildLevelText = null!;

		[Header("Right Column - AchievementInformation")]
		public RectTransform aiRect = null!;
		public RectTransform aiHeader = null!;
		public HorizontalLayoutGroup aiHeaderHLG = null!;
		public TextMeshProUGUI aiHeaderTitle = null!;
		public TextMeshProUGUI aiHeaderDate = null!;
		public TextMeshProUGUI aiDescription = null!;

		public RectTransform progressBar = null!;
		public Image progressBarBg = null!;
		public Image progressBarBgGreen = null!;
		public TextMeshProUGUI progressText = null!;

		public Sprite defaultIcon = null!;

		public void Awake()
		{
			defaultIcon = achievementIconImg.sprite;
		}

		public void Setup(AchievementConfig config, AchievementData? data)
		{
			data ??= new AchievementData();

			int currentStep = Math.Min(data.completed.Count, config.progress.Count - 1);
			float end = config.progress[currentStep];

			aiDescription.text = config.config.Aggregate(Localization.instance.Localize(config.description), (text, kv) => text.Replace("{" + kv.Key + "}", kv.Value));
			aiHeaderTitle.text = config.name;
			achievementIconImg.sprite = config.GetIcon() ?? defaultIcon;

			guildLevelText.text = config.GetLevel(currentStep + 1).ToString();

			progressBar.gameObject.SetActive(end > 1);

			if (config.progress.Count <= data.completed.Count || data.progress is null)
			{
				float last = config.progress.Last();
				progressBarBgGreen.fillAmount = 1;
				progressText.text = $"{last} / {last}";
			}
			else
			{
				progressBarBgGreen.fillAmount = data.progress.Value / end;
				progressText.text = $"{data.progress} / {end}";
			}

			aiHeaderDate.text = data.completed.Count > 0 ? data.completed.Last().ToString("yyyy-MM-dd HH:mm") : "";
			rowNotCompleted.SetActive(data.completed.Count == 0);
		}
	}
}
