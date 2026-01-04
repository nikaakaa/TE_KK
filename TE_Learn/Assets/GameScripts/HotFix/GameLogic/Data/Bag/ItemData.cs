namespace GameLogic
{
    /// <summary>
    /// 物品数据结构（纯数据，不依赖 Unity）
    /// </summary>
    public struct ItemData
    {
        /// <summary>
        /// 物品唯一 ID
        /// </summary>
        public int Id;

        /// <summary>
        /// 物品名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 所在槽位索引（-1 表示不在任何槽位中）
        /// </summary>
        public int SlotIndex;

        /// <summary>
        /// 图标颜色（演示用，实际项目可能是图标资源路径）
        /// </summary>
        public UnityEngine.Color IconColor;

        /// <summary>
        /// 创建物品数据
        /// </summary>
        public static ItemData Create(int id, string name, int slotIndex, UnityEngine.Color color)
        {
            return new ItemData
            {
                Id = id,
                Name = name,
                SlotIndex = slotIndex,
                IconColor = color
            };
        }

        /// <summary>
        /// 是否有效（ID > 0 视为有效）
        /// </summary>
        public bool IsValid => Id > 0;
    }
}
