namespace GameLogic
{
    /// <summary>
    /// 背包相关事件 ID 定义
    /// </summary>
    public static class BagEvents
    {
        /// <summary>
        /// 背包初始化完成
        /// 参数: int slotCount
        /// </summary>
        public const int BagInitialized = 10001;

        /// <summary>
        /// 物品添加
        /// 参数: ItemData itemData
        /// </summary>
        public const int ItemAdded = 10002;

        /// <summary>
        /// 物品移除
        /// 参数: int itemId
        /// </summary>
        public const int ItemRemoved = 10003;

        /// <summary>
        /// 物品移动（换槽位）
        /// 参数: int itemId, int fromSlotIndex, int toSlotIndex
        /// </summary>
        public const int ItemMoved = 10004;

        /// <summary>
        /// 背包已满（添加失败）
        /// 无参数
        /// </summary>
        public const int BagFull = 10005;
    }
}
