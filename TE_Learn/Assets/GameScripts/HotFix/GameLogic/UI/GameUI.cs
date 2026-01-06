using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Window(UILayer.UI, location: "GameUI")]
    public partial class GameUI
    {
        #region 事件

        private partial void OnClick_OpenBagBtn()
        {
            // 通过状态机请求打开背包
            HotFixHFSMModule.Instance?.RequestOpenBag();
        }

        #endregion
    }
}
