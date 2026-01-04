using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 背包系统（System 层）
    /// 业务逻辑入口，不依赖 UI
    /// </summary>
    public class BagSystem : Singleton<BagSystem>
    {
        #region 字段

        /// <summary>
        /// 背包数据
        /// </summary>
        private readonly BagData m_bagData = new BagData();

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool m_initialized = false;

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取背包数据（只读访问）
        /// </summary>
        public BagData BagData => m_bagData;

        /// <summary>
        /// 槽位数量
        /// </summary>
        public int SlotCount => m_bagData.SlotCount;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => m_initialized;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化背包系统
        /// </summary>
        /// <param name="slotCount">槽位数量</param>
        public void Initialize(int slotCount = 9)
        {
            if (m_initialized)
            {
                Log.Warning("[BagSystem] 已经初始化过了");
                return;
            }

            m_bagData.Initialize(slotCount);
            m_initialized = true;

            Log.Info($"[BagSystem] 初始化完成，槽位数量: {slotCount}");
        }

        /// <summary>
        /// 重置背包系统
        /// </summary>
        public void Reset()
        {
            m_initialized = false;
        }

        #endregion

        #region 物品操作

        /// <summary>
        /// 尝试添加物品
        /// </summary>
        /// <param name="itemName">物品名称</param>
        /// <returns>是否成功</returns>
        public bool TryAddItem(string itemName)
        {
            if (!m_initialized)
            {
                Log.Error("[BagSystem] 未初始化");
                return false;
            }

            var itemData = m_bagData.AddItem(itemName);
            return itemData.IsValid;
        }

        /// <summary>
        /// 添加物品到指定槽位
        /// </summary>
        /// <param name="itemName">物品名称</param>
        /// <param name="slotIndex">目标槽位</param>
        /// <returns>是否成功</returns>
        public bool TryAddItemToSlot(string itemName, int slotIndex)
        {
            if (!m_initialized)
            {
                Log.Error("[BagSystem] 未初始化");
                return false;
            }

            var itemData = m_bagData.AddItemToSlot(itemName, slotIndex);
            return itemData.IsValid;
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        /// <param name="itemId">物品 ID</param>
        /// <returns>是否成功</returns>
        public bool RemoveItem(int itemId)
        {
            if (!m_initialized)
            {
                Log.Error("[BagSystem] 未初始化");
                return false;
            }

            return m_bagData.RemoveItem(itemId);
        }

        /// <summary>
        /// 移动物品到新槽位
        /// </summary>
        /// <param name="itemId">物品 ID</param>
        /// <param name="toSlotIndex">目标槽位</param>
        /// <returns>是否成功</returns>
        public bool MoveItem(int itemId, int toSlotIndex)
        {
            if (!m_initialized)
            {
                Log.Error("[BagSystem] 未初始化");
                return false;
            }

            return m_bagData.MoveItem(itemId, toSlotIndex);
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取物品数据
        /// </summary>
        public ItemData GetItem(int itemId)
        {
            return m_bagData.GetItem(itemId);
        }

        /// <summary>
        /// 获取槽位中的物品
        /// </summary>
        public ItemData GetItemAtSlot(int slotIndex)
        {
            return m_bagData.GetItemAtSlot(slotIndex);
        }

        /// <summary>
        /// 槽位是否为空
        /// </summary>
        public bool IsSlotEmpty(int slotIndex)
        {
            return m_bagData.IsSlotEmpty(slotIndex);
        }

        #endregion
    }
}
