using System.Collections.Generic;
using TEngine;
using TMPro;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{

    /// <summary>
    /// 背包 UI 窗口 - 使用接口事件方式接收背包事件
    /// </summary>
    [Window(UILayer.UI, location: "GameBagUI")]
    public partial class GameBagUI
    {
        public List<SlotWidget> slotWidgetList = new List<SlotWidget>();
        public List<ItemWidget> itemWidgetList = new List<ItemWidget>();
        public GameBagSystem bagSystem;

        public GameObject scrollViewContent => m_scroll_Bag.content.gameObject;

        protected override void RegisterEvent()
        {
            // 使用 Source Generator 生成的事件ID类订阅事件
            AddUIEvent<BagSlotData>(IGameBagUIEvent_Event.OnAddSlot, OnAddSlot);
            AddUIEvent<BagItemData>(IGameBagUIEvent_Event.OnAddItem, OnAddItem);
            AddUIEvent<BagItemData>(IGameBagUIEvent_Event.OnFailSwapOrMove, OnFailSwapOrMove);
            AddUIEvent<BagItemData>(IGameBagUIEvent_Event.OnMoveItem, OnMoveItem);
            AddUIEvent<BagItemData, BagItemData>(IGameBagUIEvent_Event.OnSwapItem, OnSwapItem);
        }

        #region IGameBagUIEvent 接口实现

        public void OnAddSlot(BagSlotData slotData)
        {
            SlotWidget slotWidget
                = CreateWidgetByType<SlotWidget>(scrollViewContent.transform);
            slotWidget.slotData = slotData;
            slotWidgetList.Add(slotWidget);
            // Log.Error("添加了新的 SlotWidget，slotId=" + slotData.slotId);
        }

        public void OnAddItem(BagItemData itemData)
        {
            SlotWidget slotWidget = slotWidgetList.Find(
                slotWidget => slotWidget.slotData.slotId == itemData.bagSlotData.slotId);
            if (slotWidget == null)
            {
                Log.Info($"没有找到 slotId={itemData.bagSlotData.slotId} 的 SlotWidget");
                for (int i = 0; i < slotWidgetList.Count; i++)
                {
                    Log.Info($"已有 SlotWidget 列表：slotId={slotWidgetList[i].slotData.slotId}");
                }
            }
            ItemWidget itemWidget
                = CreateWidgetByType<ItemWidget>(slotWidget.transform);
            itemWidget.transform.localPosition = Vector3.zero;
            itemWidget.itemImage.color = itemData.color;
            itemWidget.itemData = itemData;
            itemWidgetList.Add(itemWidget);
        }

        public void OnFailSwapOrMove(BagItemData dragItemData)
        {
            ItemWidget itemWidget = itemWidgetList.Find(
                widget => widget.itemData.itemId == dragItemData.itemId);
            itemWidget.transform.localPosition = Vector3.zero;
        }

        public void OnMoveItem(BagItemData dragItemData)
        {
            ItemWidget itemWidget = itemWidgetList.Find(
                widget => widget.itemData.itemId == dragItemData.itemId);
            SlotWidget slotWidget = slotWidgetList.Find(
                slotWidget => slotWidget.slotData.slotId == dragItemData.bagSlotData.slotId);
            itemWidget.transform.SetParent(slotWidget.transform);
            itemWidget.transform.localPosition = Vector3.zero;
        }

        public void OnSwapItem(BagItemData dragItemData, BagItemData pointerItemData)
        {
            ItemWidget dragItemWidget = itemWidgetList.Find(
                widget => widget.itemData.itemId == dragItemData.itemId);
            ItemWidget pointerItemWidget = itemWidgetList.Find(
                widget => widget.itemData.itemId == pointerItemData.itemId);
            SlotWidget dragItemSlotWidget = slotWidgetList.Find(
                slotWidget => slotWidget.slotData.slotId == dragItemData.bagSlotData.slotId);
            SlotWidget pointerItemSlotWidget = slotWidgetList.Find(
                slotWidget => slotWidget.slotData.slotId == pointerItemData.bagSlotData.slotId);
            dragItemWidget.transform.SetParent(dragItemSlotWidget.transform);
            pointerItemWidget.transform.SetParent(pointerItemSlotWidget.transform);
            dragItemWidget.transform.localPosition = Vector3.zero;
            pointerItemWidget.transform.localPosition = Vector3.zero;
        }

        #endregion


        #region 按钮事件

        private partial void OnClick_CloseBagBtn()
        {
            // 通过状态机请求关闭背包
            HotFixHFSMModule.Instance?.RequestCloseBag();
        }
        private partial void OnClick_AddItemBtn()
        {
            bagSystem.AddItem();
        }
        private partial void OnClick_AddSlotBtn()
        {
            bagSystem.AddSlot();
        }

        #endregion


    }

    public class OpenBagState : LeafState<HFContext>
    {
        protected override void OnEnter(HFContext ctx)
        {
            Debug.Log("进入 OpenBagState（打开背包）");

            // 清除转换标志
            ctx.WantCloseBag = false;

            // Model 应该通过保存的数据初始化
            if (ctx.BagModel == null)
                ctx.BagModel = new BagModel();
            if (ctx.BagSystem == null)
                ctx.BagSystem = new GameBagSystem(ctx.BagModel);

            // 设置依赖注入
            UIBase.Injector = (ui) =>
            {
                if (ui is GameBagUI gameBagUI)
                    gameBagUI.bagSystem = ctx.BagSystem;
            };

            GameModule.UI.ShowUIAsync<GameBagUI>();
        }

        protected override void OnExit(HFContext ctx)
        {
            Debug.Log("退出 OpenBagState");

            // 关闭背包 UI
            GameModule.UI.CloseUI<GameBagUI>();

            // 清理依赖
        }
    }
}
