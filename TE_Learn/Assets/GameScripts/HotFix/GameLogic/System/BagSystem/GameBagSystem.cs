using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 背包系统 - 负责背包数据的增删改操作
    /// 通过 IGameBagUIEvent 接口事件通知 UI 层更新
    /// </summary>
    public class GameBagSystem
    {
        public GameBagSystem(BagModel bagModel)
        {
            this.bagModel = bagModel;
        }

        public BagModel bagModel;
        public BagItemData currentDragItem;
        public BagSlotData currentPointerSlot = null;
        public BagItemData currentPointerItem = null;

        /// <summary>
        /// 添加一个空槽位
        /// </summary>
        public void AddSlot()
        {
            BagSlotData newSlot = new BagSlotData()
            {
                slotId = BagModel.SLOT_ID++,
                haveItemData = null
            };
            bagModel.slots.Add(newSlot);

            // 使用接口事件通知 UI
            GameEvent.Get<IGameBagUIEvent>()?.OnAddSlot(newSlot);
        }

        /// <summary>
        /// 添加一个物品到第一个空槽位
        /// </summary>
        public void AddItem()
        {
            int emptySlotIndex = GetFirstEmptySlotIndex();
            if (emptySlotIndex == -1)
            {
                Log.Info("没有空位子了，无法添加物品");
                return;
            }
            BagItemData newItem = new BagItemData()
            {
                itemId = BagModel.ITEM_ID++,
                count = 1,
                color = new Color(Random.value, Random.value, Random.value)
            };
            var toSlotData = bagModel.slots[emptySlotIndex];
            toSlotData.haveItemData = newItem;
            newItem.bagSlotData = toSlotData;
            bagModel.items.Add(newItem);

            // 使用接口事件通知 UI
            GameEvent.Get<IGameBagUIEvent>()?.OnAddItem(newItem);
        }

        /// <summary>
        /// 获取第一个空槽位的索引
        /// </summary>
        public int GetFirstEmptySlotIndex()
        {
            for (int i = 0; i < bagModel.slots.Count; i++)
            {
                if (bagModel.slots[i].haveItemData == null)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 尝试交换或移动物品
        /// </summary>
        public void TrySwapItem()
        {
            if (currentDragItem == null)
            {
                Log.Error("当前没有拖拽物品，无法交换");
                return;
            }

            if (currentPointerItem == null)
            {
                if (currentPointerSlot == null)
                {
                    // 没有改变，直接回归原位
                    GameEvent.Get<IGameBagUIEvent>()?.OnFailSwapOrMove(currentDragItem);
                    return;
                }

                // 移动物品到空格子
                var fromSlot = currentDragItem.bagSlotData;
                var toSlot = currentPointerSlot;
                fromSlot.haveItemData = null;
                toSlot.haveItemData = currentDragItem;
                currentDragItem.bagSlotData = toSlot;

                GameEvent.Get<IGameBagUIEvent>()?.OnMoveItem(currentDragItem);
                return;
            }

            // 交换物品
            var dSlot = currentDragItem.bagSlotData;
            var pSlot = currentPointerItem.bagSlotData;
            dSlot.haveItemData = currentPointerItem;
            pSlot.haveItemData = currentDragItem;
            currentDragItem.bagSlotData = pSlot;
            currentPointerItem.bagSlotData = dSlot;

            GameEvent.Get<IGameBagUIEvent>()?.OnSwapItem(currentDragItem, currentPointerItem);
        }
    }
}