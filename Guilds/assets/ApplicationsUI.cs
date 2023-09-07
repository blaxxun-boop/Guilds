using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	[PublicAPI]
	public class ApplicationsUI : MonoBehaviour
	{
		[Header("Placeholder Variables")]
		public ApplicationsUIRow rowPlaceHolderPrefab = null!;
		public Transform rowPlaceHolderParentList = null!;
		public List<ApplicationsUIRow> rowElements = null!;

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

		[Header("Row Content UI - WhyMe Area")]
		public RectTransform whyMeArea = null!;
		public VerticalLayoutGroup whyMeAreaVlg = null!;
		public Button whyMeButton = null!;
		public Image whyMeButtonImg = null!;
		public TextMeshProUGUI whyMeAreaText = null!;

		[Header("Row Content UI - Applied Area")]
		public RectTransform appliedAreaRect = null!;
		public VerticalLayoutGroup appliedAreaVlg = null!;
		public TextMeshProUGUI appliedAreaAppliedStatusTextTMP = null!;

		[Header("Row Content UI - Action Area")]
		public RectTransform actionArea = null!;
		public Button actionAreaAcceptMemberButton = null!;
		public Image actionAreaAcceptMemberButtonImage = null!;
		public Image actionAreaAcceptMemberButtonCheckmark = null!;

		public Button actionAreaDenyMemberButton = null!;
		public TextMeshProUGUI actionAreaDenyMemberButtonTextTMP = null!;
		public Image actionAreaDenyMemberButtonImage = null!;

		[Header("Root UI Global Scrollbar")]
		public Scrollbar scrollbar = null!;
		public RectTransform scrollbarRect = null!;
		public RectTransform scrollbarSlidingArea = null!;
		public RectTransform scrollbarHandle = null!;
		public Image scrollbarHandleImage = null!;

		[Header("Back Button")]
		public Button backButton = null!;
		public Image backButtonImage = null!;
		public TextMeshProUGUI backButtonText = null!;

		[Header("Accept Guild Member Button")]
		public Button acceptGuildMemberButton = null!;
		public Image acceptGuildMemberButtonImage = null!;
		public Image acceptGuildMemberButtonCheckmark = null!;

		[Header("Deny Guild Member Button")]
		public Button denyGuildMemberButton = null!;
		public Image denyGuildMemberImage = null!;
		public TextMeshProUGUI denyGuildMemberText = null!;

		[Header("Popup")]
		public RectTransform popupRootRect = null!;
		public RectTransform popupBkgBlocking = null!;
		public Image popupBkgBlockingImg = null!;
		public CanvasGroup popupCanvasGroup = null!; // This is really for blocking raycasts and making everything in it interactible
		public RectTransform popupBkg = null!;
		public Image popupBkgImg = null!;
		public RectTransform popupScrollviewRect = null!;
		public ScrollRect popupScrollview = null!;
		public Image popupScrollviewImage = null!;
		public RectTransform popupViewport = null!;
		public Image popupViewportImage = null!;
		public RectTransform popupViewportContentRect = null!;
		public RectTransform popupViewportContentBodyTextRect = null!;
		public TextMeshProUGUI popupViewportContentBodyText = null!;
		public RectTransform popupScrollbarRect = null!;
		public Scrollbar popupScrollbar = null!;
		public Image popupScrollbarImg = null!;
		public RectTransform popupSlidingAreaRect = null!;
		public RectTransform popupHandleRect = null!;
		public Image popupHandleImg = null!;
		public RectTransform popupHeaderTextRect = null!;
		public TextMeshProUGUI popupHeaderText = null!;
		public RectTransform popupButtonGroupRect = null!;
		public HorizontalLayoutGroup popupButtonGroup = null!;
		public RectTransform popupButtonOkRect = null!;
		public Button popupButtonOk = null!;
		public Image popupButtonOkImg = null!;
		public RectTransform popupButtonOkTextRect = null!;
		public Text popupButtonOkText = null!;

		private List<ApplicationsUIRow> _activeRows = new();
		private Queue<ApplicationsUIRow> _pooledRows = new();

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

				PopulateRows(API.GetOwnGuild()!.Applications);
			}
		}

		public void PopulateRows(Dictionary<PlayerReference, Application> members)
		{
			foreach (ApplicationsUIRow? row in _activeRows)
			{
				row.gameObject.SetActive(false);
				_pooledRows.Enqueue(row);
			}

			_activeRows.Clear();

			foreach (KeyValuePair<PlayerReference, Application> member in members)
			{
				ApplicationsUIRow row = GetRow();
				row.Setup(this, member.Key, member.Value);
				_activeRows.Add(row);
			}
		}

		private ApplicationsUIRow GetRow()
		{
			ApplicationsUIRow row = _pooledRows.Count > 0 ? _pooledRows.Dequeue() : Instantiate(rowPlaceHolderPrefab.gameObject, rowPlaceHolderParentList).GetComponent<ApplicationsUIRow>();
			row.gameObject.SetActive(true);
			return row;
		}

		private void ReturnRowToPool(ApplicationsUIRow row)
		{
			row.gameObject.SetActive(false);
			_pooledRows.Enqueue(row);
		}

		public void whyMeAreaClicked()
		{
			// The game object is already toggled for you in Unity, you just need to pass the information into the popup
			// Add your listener to the whyMeButton
		}

		public void OnButtonClosed_Clicked()
		{
			Interface.HideUI();
		}

		public void ButtonBackClicked()
		{
			Interface.SwitchUI(Interface.GuildManagementUI);
		}

		public void ButtonEditGuildClicked() { }
		public void ButtonApplicationsClicked() { }
	}
}
