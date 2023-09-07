using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class GuildManagementUI : MonoBehaviour
	{
		[Header("Placeholder Variables")]
		public GuildManagementUIRow rowPlaceHolderPrefab = null!;
		public Transform rowPlaceHolderParentList = null!;
		public List<GuildManagementUIRow> rowElements = null!;

		[Header("Root UI")]
		public RectTransform rootTransform = null!;
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
		public RectTransform contentList = null!;
		public VerticalLayoutGroup contentListVlg = null!;
		public ContentSizeFitter contentListSizeFitter = null!;

		[Header("Row Placeholder UI")]
		public RectTransform rowTransform = null!;
		public Image rowBackImage = null!;
		public Image rowBorderImage = null!;
		public HorizontalLayoutGroup contentHlg = null!;

		[Header("Row Content UI - Name Area")]
		public RectTransform nameAreaRect = null!;
		public VerticalLayoutGroup nameAreaVlg = null!;
		public TextMeshProUGUI nameAreaNameTextTMP = null!;

		[Header("Row Content UI - Rank Area")]
		public RectTransform rankArea = null!;
		public VerticalLayoutGroup rankAreaVlg = null!;
		public TMP_Dropdown rankAreaDropdown = null!;
		public Image rankAreaDropdownImage = null!;
		public TextMeshProUGUI rankAreaDropdownLabel = null!;
		public Image rankAreaDropdownArrow = null!;
		public RectTransform rankAreaDropdownTemplate = null!;
		public ScrollRect rankAreaDropdownTemplateScrollRect = null!;
		public Image rankAreaDropdownTemplateScrollRectImage = null!;
		public RectTransform rankAreaDropdownTemplateViewport = null!;
		public Image rankAreaDropdownTemplateViewportImage = null!;
		public RectTransform rankAreaDropdownTemplateContent = null!;
		public RectTransform rankAreaDropdownTemplateItem = null!;
		public Image rankAreaDropdownTemplateItemBackgroundImage = null!;
		public Image rankAreaDropdownTemplateItemCheckmarkImage = null!;
		public TextMeshProUGUI rankAreaDropdownTemplateItemLabel = null!;
		public Scrollbar rankAreaDropdownTemplateScrollbar = null!;
		public Image rankAreaDropdownTemplateScrollbarImage = null!;
		public RectTransform rankAreaDropdownTemplateSlidingAreaRect = null!;
		public RectTransform rankAreaDropdownTemplateHandle = null!;
		public Image rankAreaDropdownTemplateHandleImage = null!;

		[Header("Row Content UI - Online Area")]
		public RectTransform onlineAreaRect = null!;
		public VerticalLayoutGroup onlineAreaVlg = null!;
		public TextMeshProUGUI onlineAreaOnlineStatusTextTMP = null!;

		[Header("Row Content UI - Action Area")]
		public RectTransform actionArea = null!;
		public Button actionAreaRemoveMemberButton = null!;
		public TextMeshProUGUI actionAreaRemoveMemberButtonTextTMP = null!;
		public Image actionAreaRemoveMemberButtonImage = null!;

		[Header("Root UI Global Scrollbar")]
		public Scrollbar scrollbar = null!;
		public RectTransform scrollbarRect = null!;
		public RectTransform scrollbarSlidingArea = null!;
		public RectTransform scrollbarHandle = null!;
		public Image scrollbarHandleImage = null!;

		[Header("Leave Guild Button")]
		public Button leaveGuildButton = null!;
		public Image leaveGuildButtonImage = null!;
		public TextMeshProUGUI leaveGuildButtonText = null!;

		[Header("Edit Guild Button")]
		public Button editGuildButton = null!;
		public Image editGuildButtonImage = null!;
		public TextMeshProUGUI editGuildButtonText = null!;

		[Header("Applications Button")]
		public Button applicationsButton = null!;
		public Image applicationsButtonImage = null!;
		public TextMeshProUGUI applicationsButtonText = null!;

		private List<GuildManagementUIRow> _activeRows = new();
		private Queue<GuildManagementUIRow> _pooledRows = new();
		
		public void OnEnable()
		{
			UpdateRows();

			headerTextTMP.text = API.GetOwnGuild()!.Name;
		}

		public void Awake()
		{
			headerTextTMP.text = "";
			rowPlaceHolderPrefab.gameObject.SetActive(false);
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

				PopulateRows(API.GetOwnGuild()!.Members);
				applicationsButton.gameObject.SetActive(API.GetPlayerRank(PlayerReference.forOwnPlayer()) is Ranks.Leader or Ranks.Coleader or Ranks.Officer);
				editGuildButton.gameObject.SetActive(API.GetPlayerRank(PlayerReference.forOwnPlayer()) is Ranks.Leader or Ranks.Coleader);
			}
		}

		private void PopulateRows(Dictionary<PlayerReference, GuildMember> members)
		{
			foreach (GuildManagementUIRow? row in _activeRows)
			{
				row.gameObject.SetActive(false);
				_pooledRows.Enqueue(row);
			}

			_activeRows.Clear();

			// Populate rows based on member data
			foreach (KeyValuePair<PlayerReference, GuildMember> member in members)
			{
				GuildManagementUIRow row = GetRow();
				row.Setup(member);
				_activeRows.Add(row);
			}
		}

		private GuildManagementUIRow GetRow()
		{
			GuildManagementUIRow row = _pooledRows.Count > 0 ? _pooledRows.Dequeue() : Instantiate(rowTransform.gameObject, rowPlaceHolderParentList).GetComponent<GuildManagementUIRow>();
			row.gameObject.SetActive(true);
			return row;
		}

		private void ReturnRowToPool(GuildManagementUIRow row)
		{
			row.gameObject.SetActive(false);
			_pooledRows.Enqueue(row);
		}

		public void ButtonLeaveGuildClicked()
		{
			if (API.GetPlayerRank(PlayerReference.forOwnPlayer()) is Ranks.Leader)
			{
				if (API.GetOwnGuild()!.Members.Count == 1)
				{
					UnifiedPopup.Push(new YesNoPopup("$guilds_confirm_deletion", "$guilds_confirm_deletion_details", () =>
					{
						API.DeleteGuild(API.GetOwnGuild()!);

						UnifiedPopup.Pop();
					}, UnifiedPopup.Pop));

					return;
				}

				UnifiedPopup.Push(new WarningPopup("$guilds_leader_left", "$guilds_leader_left_details", UnifiedPopup.Pop));
				return;
			}

			UnifiedPopup.Push(new YesNoPopup("$guilds_confirm_leave", "$guilds_confirm_leave_details", () =>
			{
				API.RemovePlayerFromGuild(PlayerReference.forOwnPlayer());

				UnifiedPopup.Pop();
			}, UnifiedPopup.Pop));
		}

		public void ButtonEditGuildClicked()
		{
			Interface.SwitchUI(Interface.EditGuildUI);
		}

		public void ButtonApplicationsClicked()
		{
			Interface.SwitchUI(Interface.ApplicationsUI);
		}

		public void ButtonCloseClicked()
		{
			Interface.HideUI();
		}
	}
}
