using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Guilds
{
	[PublicAPI]
	public class GuildIconUI : MonoBehaviour
	{
		[Header("Root UI")] 
		public RectTransform rootTransform = null!;
		public GameObject rootGameObject = null!;
		public Image backgroundBack = null!;
		public Image background = null!;

		[Header("Header UI")] 
		public RectTransform headerTransform = null!;
		public HorizontalLayoutGroup headerHlg = null!;
		public Image headerImageLeft = null!;
		public TextMeshProUGUI headerTextTMP = null!;
		public Image headerImageRight = null!;

		[Header("Close Button UI")] 
		public Button buttonClose = null!;
		public Image buttonCloseImage = null!;
		public TextMeshProUGUI buttonCloseText = null!;

		[Header("Content UI")] 
		public RectTransform contentTransform = null!;
		public ScrollRect contentScrollRect = null!;
		public Image contentScrollRectImage = null!;
		public Scrollbar guildIconListScroll = null!;
		public Image guildIconListScrollImage = null!;
		public RectTransform guildIconListScrollSlidingArea = null!;
		public RectTransform guildIconListScrollHandle = null!;
		public Image guildIconListScrollHandleImage = null!;

		[Header("Content UI - Guild Icons")] 
		public RectTransform guildIconListRoot = null!;
		public GridLayoutGroup guildIconListRootGlg = null!;
		public ContentSizeFitter guildIconListRootSizeFitter = null!;

		[Header("Guild Icon Placeholder")] 
		public GameObject guidIconElementPrefab = null!;
		public GuildIconElement guildIconElementPrefabComponent = null!;
		public Button guildIconElementButton = null!;
		public Image guildIconElementButtonImage = null!;
		public Image guildIconElementButtonIconBackground = null!;
		public Image guildIconElementButtonIcon = null!;

		public List<GameObject> guildIconList = new();

		public Action<int> selectedGuildIcon = null!;

		public void Awake()
		{
			FillTrophyList();
		}
		
		public void FillTrophyList()
		{
			foreach (KeyValuePair<int, Sprite> kv in Interface.GuildIcons)
			{
				GuildIconElement guildIcon = Instantiate(guidIconElementPrefab, guildIconListRoot).GetComponent<GuildIconElement>();
				guildIcon.guildIcon.sprite = kv.Value;
				guildIcon.gameObject.SetActive(true);
				guildIcon.guildIconUI = this;
				guildIcon.guildIconId = kv.Key;
			}
		}
		
		public void OnButtonClosed_Clicked()
		{
			gameObject.SetActive(false);
		}
	}
}
