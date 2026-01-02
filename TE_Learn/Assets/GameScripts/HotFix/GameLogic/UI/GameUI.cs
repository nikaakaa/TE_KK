using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	[Window(UILayer.UI, location : "GameUI")]
	public partial class GameUI
	{
		#region 事件

		private partial void OnClick_ShowBagBtn()
		{
			GameModule.UI.ShowUIAsync<GameBagUI>();
		}

		#endregion
	}
}
