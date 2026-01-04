using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 背包数据容器（Model 层）
    /// 负责管理物品数据，数据变更时发送事件通知 View 层刷新
    /// </summary>
    public class BagData
    {
        #region 字段

        /// <summary>
        /// 槽位总数
        /// </summary>
        public int SlotCount { get; private set; }

        /// <summary>
        /// 物品列表
        /// </summary>
        private readonly List<ItemData> m_items = new List<ItemData>();

        /// <summary>
        /// 槽位占用状态（slotIndex -> itemId，0 表示空）
        /// </summary>
        private readonly Dictionary<int, int> m_slotToItem = new Dictionary<int, int>();

        /// <summary>
        /// 物品 ID 计数器
        /// </summary>
        private int m_nextItemId = 1;

        /// <summary>
        /// 预设颜色列表（演示用）
        /// </summary>
        private static readonly Color[] s_colors = new Color[]
        {
            Color.red, Color.green, Color.blue,
            Color.yellow, Color.cyan, Color.magenta
        };

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取所有物品（只读）
        /// </summary>
        public IReadOnlyList<ItemData> Items => m_items;

        /// <summary>
        /// 当前物品数量
        /// </summary>
        public int ItemCount => m_items.Count;

        /// <summary>
        /// 背包是否已满
        /// </summary>
        public bool IsFull => m_items.Count >= SlotCount;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化背包
        /// </summary>
        /// <param name="slotCount">槽位数量</param>
        public void Initialize(int slotCount)
        {
            SlotCount = slotCount;
            m_items.Clear();
            m_slotToItem.Clear();
            m_nextItemId = 1;

            // 初始化槽位状态
            for (int i = 0; i < slotCount; i++)
            {
                m_slotToItem[i] = 0;
            }

            // 发送初始化完成事件
            GameEvent.Send(BagEvents.BagInitialized, slotCount);
        }

        #endregion

        #region 物品操作

        /// <summary>
        /// 添加物品到第一个空槽位
        /// </summary>
        /// <param name="itemName">物品名称</param>
        /// <returns>添加的物品数据，失败返回默认值</returns>
        public ItemData AddItem(string itemName)
        {
            // 查找第一个空槽位
            int emptySlot = FindFirstEmptySlot();
            if (emptySlot < 0)
            {
                Log.Warning("背包已满！");
                GameEvent.Send(BagEvents.BagFull);
                return default;
            }

            return AddItemToSlot(itemName, emptySlot);
        }

        /// <summary>
        /// 添加物品到指定槽位
        /// </summary>
        /// <param name="itemName">物品名称</param>
        /// <param name="slotIndex">目标槽位</param>
        /// <returns>添加的物品数据</returns>
        public ItemData AddItemToSlot(string itemName, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
            {
                Log.Error($"无效的槽位索引: {slotIndex}");
                return default;
            }

            if (!IsSlotEmpty(slotIndex))
            {
                Log.Warning($"槽位 {slotIndex} 已被占用");
                return default;
            }

            // 创建物品数据
            int itemId = m_nextItemId++;
            var color = s_colors[itemId % s_colors.Length];
            var itemData = ItemData.Create(itemId, itemName, slotIndex, color);

            // 更新数据
            m_items.Add(itemData);
            m_slotToItem[slotIndex] = itemId;

            Log.Info($"[BagData] 添加物品: {itemName} (ID:{itemId}) 到槽位 {slotIndex}");

            // 发送事件
            GameEvent.Send(BagEvents.ItemAdded, itemData);

            return itemData;
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        /// <param name="itemId">物品 ID</param>
        /// <returns>是否成功</returns>
        public bool RemoveItem(int itemId)
        {
            int index = m_items.FindIndex(item => item.Id == itemId);
            if (index < 0)
            {
                Log.Warning($"物品不存在: {itemId}");
                return false;
            }

            var itemData = m_items[index];

            // 清除槽位占用
            if (itemData.SlotIndex >= 0)
            {
                m_slotToItem[itemData.SlotIndex] = 0;
            }

            m_items.RemoveAt(index);

            Log.Info($"[BagData] 移除物品: {itemData.Name} (ID:{itemId})");

            // 发送事件
            GameEvent.Send(BagEvents.ItemRemoved, itemId);

            return true;
        }

        /// <summary>
        /// 移动物品到新槽位
        /// </summary>
        /// <param name="itemId">物品 ID</param>
        /// <param name="toSlotIndex">目标槽位</param>
        /// <returns>是否成功</returns>
        public bool MoveItem(int itemId, int toSlotIndex)
        {
            if (toSlotIndex < 0 || toSlotIndex >= SlotCount)
            {
                Log.Error($"无效的目标槽位: {toSlotIndex}");
                return false;
            }

            int index = m_items.FindIndex(item => item.Id == itemId);
            if (index < 0)
            {
                Log.Warning($"物品不存在: {itemId}");
                return false;
            }

            var itemData = m_items[index];
            int fromSlotIndex = itemData.SlotIndex;

            // 如果目标槽位有物品，交换位置
            int targetItemId = m_slotToItem.GetValueOrDefault(toSlotIndex, 0);
            if (targetItemId > 0)
            {
                // 交换两个物品的槽位
                int targetIndex = m_items.FindIndex(item => item.Id == targetItemId);
                if (targetIndex >= 0)
                {
                    var targetItem = m_items[targetIndex];
                    targetItem.SlotIndex = fromSlotIndex;
                    m_items[targetIndex] = targetItem;
                    m_slotToItem[fromSlotIndex] = targetItemId;
                }
            }
            else
            {
                // 目标槽位为空，直接移动
                m_slotToItem[fromSlotIndex] = 0;
            }

            // 更新物品槽位
            itemData.SlotIndex = toSlotIndex;
            m_items[index] = itemData;
            m_slotToItem[toSlotIndex] = itemId;

            Log.Info($"[BagData] 移动物品: {itemData.Name} 从槽位 {fromSlotIndex} 到 {toSlotIndex}");

            // 发送事件
            GameEvent.Send(BagEvents.ItemMoved, itemId, fromSlotIndex, toSlotIndex);

            return true;
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取物品数据
        /// </summary>
        public ItemData GetItem(int itemId)
        {
            return m_items.Find(item => item.Id == itemId);
        }

        /// <summary>
        /// 获取槽位中的物品
        /// </summary>
        public ItemData GetItemAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                return default;

            int itemId = m_slotToItem.GetValueOrDefault(slotIndex, 0);
            if (itemId <= 0)
                return default;

            return GetItem(itemId);
        }

        /// <summary>
        /// 槽位是否为空
        /// </summary>
        public bool IsSlotEmpty(int slotIndex)
        {
            return m_slotToItem.GetValueOrDefault(slotIndex, 0) <= 0;
        }

        /// <summary>
        /// 查找第一个空槽位
        /// </summary>
        /// <returns>槽位索引，-1 表示没有空槽位</returns>
        public int FindFirstEmptySlot()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (IsSlotEmpty(i))
                    return i;
            }
            return -1;
        }

        #endregion
    }
}
