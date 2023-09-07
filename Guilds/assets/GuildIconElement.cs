using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Guilds
{
	[PublicAPI]
	public class GuildIconElement : MonoBehaviour
	{
		public Button guildIconButton = null!;
		public RectTransform guildIconButtonRect = null!;
		public Image guildIconBkg = null!;
		public RectTransform guildIconBkgRect = null!;
		public Image guildIcon = null!;
		public RectTransform guildIconRect = null!;
		
		public GuildIconUI guildIconUI = null!;
		public int guildIconId;

		public void OnGuildIconElement_Clicked()
		{
			guildIconUI.selectedGuildIcon(guildIconId);
			guildIconUI.gameObject.SetActive(false);
		}
	}
}
