using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class SearchGuildUI : MonoBehaviour
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

		public RectTransform GuildRowPlaceholder = null!;
		public RectTransform GuildRowPlaceholderBack = null!;
		public RectTransform GuildRowPlaceholderBackBg = null!;
		public RectTransform GuildRowPlaceholderBackBgImg = null!;
		public RectTransform GuildRowPlaceholderBackBorder = null!;
		public RectTransform GuildRowPlaceholderBackBorderImg = null!;
		public RectTransform GuildRowPlaceholderContent = null!;
		public RectTransform GuildRowPlaceholderContentLeftCol = null!;
		public HorizontalLayoutGroup GuildRowPlaceholderContentLeftColHLG = null!;
		public RectTransform GuildRowPlaceholderContentLeftColIcon = null!;
		public Image GuildRowPlaceholderContentLeftColIconImage = null!;
		public Image GuildRowPlaceholderContentLeftColIconImageSelected = null!;
		public Image GuildRowPlaceholderContentLeftColIconImageBorder = null!;
		public RectTransform GuildRowPlaceholderContentLeftColNaming = null!;
		public VerticalLayoutGroup GuildRowPlaceholderContentLeftColNamingVLG = null!;
		public RectTransform GuildRowPlaceholderContentLeftColNamingLvl = null!;
		public TextMeshProUGUI GuildRowPlaceholderContentLeftColNamingLvlTMP = null!;
		public RectTransform GuildRowPlaceholderContentLeftColNamingName = null!;
		public TextMeshProUGUI GuildRowPlaceholderContentLeftColNamingNameTMP = null!;
		public RectTransform GuildRowPlaceholderContentLeftColNamingLeader = null!;
		public TextMeshProUGUI GuildRowPlaceholderContentLeftColNamingLeaderTMP = null!;
		public RectTransform GuildRowPlaceholderContentRightCol = null!;
		public Image GuildRowPlaceholderContentRightColBg = null!;
		public TMP_InputField GuildRowPlaceholderContentRightColInputField = null!;

		public Scrollbar contentScrollbar = null!;

		public RectTransform contentScrollbarSlidingArea = null!;
		public Image contentScrollbarImage = null!;
		public RectTransform contentScrollbarHandle = null!;
		public Image contentScrollbarHandleImage = null!;

		private ApplyUI applyUI = null!;

		private List<SearchGuildUIRow> _activeRows = new();
		private Queue<SearchGuildUIRow> _pooledRows = new();

		public void Awake()
		{
			GuildRowPlaceholder.gameObject.SetActive(false);
			applyUI = transform.Find("ApplyUI").GetComponent<ApplyUI>();
		}

		public void OnEnable()
		{
			UpdateRows();
		}

		public void UpdateRows()
		{
			if (gameObject.activeSelf)
			{
				if (API.GetOwnGuild() is not null)
				{
					gameObject.SetActive(false);
					Interface.GuildManagementUI.SetActive(true);
					return;
				}

				PopulateRows(API.GetGuilds());
			}
		}

		private void PopulateRows(List<Guild> guilds)
		{
			foreach (SearchGuildUIRow? row in _activeRows)
			{
				row.gameObject.SetActive(false);
				_pooledRows.Enqueue(row);
			}

			_activeRows.Clear();

			// Populate rows based on member data
			foreach (Guild guild in guilds)
			{
				SearchGuildUIRow row = GetRow();
				row.Setup(guild, applyUI);
				_activeRows.Add(row);
			}
		}

		private SearchGuildUIRow GetRow()
		{
			SearchGuildUIRow row = _pooledRows.Count > 0 ? _pooledRows.Dequeue() : Instantiate(GuildRowPlaceholder.gameObject, contentList).GetComponent<SearchGuildUIRow>();
			row.gameObject.SetActive(true);
			return row;
		}

		private void ReturnRowToPool(SearchGuildUIRow row)
		{
			row.gameObject.SetActive(false);
			_pooledRows.Enqueue(row);
		}

		public void OnButtonClosed_Clicked()
		{
			Interface.HideUI();
		}

		public void GuildRowPlaceholderContentRightColInputFieldOnValueChanged()
		{

		}

		public void GuildRowPlaceholderContentRightColInputFieldOnEndEdit()
		{

		}

		public void GuildRowPlaceholderContentRightColInputFieldOnSelect()
		{

		}

		public void GuildRowPlaceholderContentRightColInputFieldOnDeselect()
		{

		}
	}
}
