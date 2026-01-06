using TEngine;
using UnityEngine;
namespace GameLogic
{
    public class GameBagSystem
    {

        public static readonly int OnAddSlotEvent = RuntimeId.ToRuntimeId("OnAddSlotEvent");


        public GameBagSystem(BagModel bagModel)
        {
            this.bagModel = bagModel;
        }
        public BagModel bagModel;
        public BagItemData currentDragItem;
        public BagSlotData currentPointerSlot = null;
        public BagItemData currentPointerItem = null;

        public void AddSlot()
        {
            BagSlotData newSlot = new BagSlotData()
            {
                slotId = BagModel.SLOT_ID++,
                haveItemData = null
            };
            bagModel.slots.Add(newSlot);
            GameEvent.Send(OnAddSlotEvent, newSlot);
        }
        public static readonly int OnAddItemEvent = RuntimeId.ToRuntimeId("OnAddItemEvent");
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
            GameEvent.Send(OnAddItemEvent, newItem);
        }
        public int GetFirstEmptySlotIndex()
        {
            for (int i = 0; i < bagModel.slots.Count; i++)
            {
                if (bagModel.slots[i].haveItemData == null)
                    return i;
            }
            return -1;
        }
        public static readonly int OnFailSwapOrMoveEvent = RuntimeId.ToRuntimeId("OnFailSwapOrMoveEvent");
        public static readonly int OnMoveItemEvent = RuntimeId.ToRuntimeId("OnMoveItemEvent");
        public static readonly int OnSwapItemEvent = RuntimeId.ToRuntimeId("OnSwapItemEvent");
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
                    //没有改变,直接回归原位
                    GameEvent.Send(OnFailSwapOrMoveEvent, currentDragItem);
                    return;
                }
                // 移动物品到空格子
                var fromSlot = currentDragItem.bagSlotData;
                var toSlot = currentPointerSlot;
                fromSlot.haveItemData = null;
                toSlot.haveItemData = currentDragItem;
                currentDragItem.bagSlotData = toSlot;
                GameEvent.Send(OnMoveItemEvent, currentDragItem);
                return;
            }
            // 交换物品
            var dSlot = currentDragItem.bagSlotData;
            var pSlot = currentPointerItem.bagSlotData;
            dSlot.haveItemData = currentPointerItem;
            pSlot.haveItemData = currentDragItem;
            currentDragItem.bagSlotData = pSlot;
            currentPointerItem.bagSlotData = dSlot;
            GameEvent.Send(OnSwapItemEvent, currentDragItem, currentPointerItem);
        }
    }
}