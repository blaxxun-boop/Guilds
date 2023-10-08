using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class AchievementPopup : MonoBehaviour
	{
		[Header("Row Root")]
		public RectTransform rowRootTransform = null!;
		public AchievementPopup rowInstance = null!;
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

		[Header("Animator")]
		public Animator animatorComp = null!;

		public Sprite defaultIcon = null!;
		private Queue<QueuedPopup> queue = new();

		public void Awake()
		{
			RectTransform rect = GetComponent<RectTransform>();
			rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.25f);
			defaultIcon = achievementIconImg.sprite;
		}

		public void Show()
		{
			animatorComp.SetBool("visible", true);
		}

		public void Hide()
		{
			if (!animatorComp.GetBool("visible"))
			{
				return;
			}
			animatorComp.SetBool("visible", false);
		}

		private struct QueuedPopup
		{
			public PlayerReference Player;
			public AchievementConfig Config;
			public int Level;
		}

		public static void Queue(PlayerReference player, AchievementConfig config, int level)
		{
			if (!Interface.AchievementPopup)
			{
				return;
			}
			Interface.AchievementPopup.GetComponent<AchievementPopup>().Queue(new QueuedPopup{ Player = player, Config = config, Level = level });
		}
		
		private void Queue(QueuedPopup popup)
		{
			queue.Enqueue(popup);
			if (!gameObject.activeSelf)
			{
				gameObject.SetActive(true);

				IEnumerator Dequeue()
				{
					while (queue.Count > 0)
					{
						QueuedPopup active = queue.Dequeue();

						achievementIconImg.sprite = active.Config.GetIcon() ?? defaultIcon;
						aiDescription.text = active.Config.name;
						guildLevelText.text = active.Config.GetLevel(active.Level).ToString();
						
						Show();
						yield return new WaitForSeconds(5);
						Hide();
						yield return new WaitForSeconds(2);
					}

					gameObject.SetActive(false);
				}
				StartCoroutine(Dequeue());
			}
		}
	}
}
