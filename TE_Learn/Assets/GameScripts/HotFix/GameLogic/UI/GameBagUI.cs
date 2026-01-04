using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 背包UI - MVE 模式（事件驱动）
    /// View 层只负责显示，业务逻辑交给 BagSystem
    /// </summary>
    [Window(UILayer.UI, location: "GameBagUI")]
    public partial class GameBagUI
    {
        #region 字段

        // Widget 列表
        private readonly List<Slot> m_slots = new List<Slot>();
        private readonly Dictionary<int, Item> m_itemWidgets = new Dictionary<int, Item>(); // itemId -> Widget

        // 拖拽状态
        private Item m_draggingItem;
        private Slot m_hoveredSlot;

        // 预制体资源路径
        private const string SlotPrefabPath = "Slot";
        private const string ItemPrefabPath = "Item";

        // 缓存的预制体引用
        private GameObject m_slotPrefab;
        private GameObject m_itemPrefab;
        private bool m_prefabsLoaded;

        // 容器引用
        private Transform m_slotContainer;

        #endregion

        #region 生命周期

        protected override void OnCreate()
        {
            base.OnCreate();

            // 查找 ScrollRect 的 content 作为槽位容器
            var scrollRect = gameObject.GetComponentInChildren<ScrollRect>();
            m_slotContainer = scrollRect != null && scrollRect.content != null
                ? scrollRect.content
                : transform;

            // 异步加载预制体
            LoadPrefabsAsync();
        }

        /// <summary>
        /// 注册事件监听（重要：MVE 模式核心）
        /// </summary>
        protected override void RegisterEvent()
        {
            // 订阅背包事件
            AddUIEvent<int>(BagEvents.BagInitialized, OnBagInitialized);
            AddUIEvent<ItemData>(BagEvents.ItemAdded, OnItemAdded);
            AddUIEvent<int>(BagEvents.ItemRemoved, OnItemRemoved);
            AddUIEvent<int, int, int>(BagEvents.ItemMoved, OnItemMoved);
            AddUIEvent(BagEvents.BagFull, OnBagFull);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            
            // 如果 BagSystem 已初始化，刷新显示
            if (BagSystem.Instance.IsInitialized)
            {
                RefreshAllItems();
            }
        }

        protected override void OnDestroy()
        {
            m_slots.Clear();
            m_itemWidgets.Clear();
            base.OnDestroy();
        }

        #endregion

        #region 资源加载

        private async void LoadPrefabsAsync()
        {
            try
            {
                var slotTask = GameModule.Resource.LoadAssetAsync<GameObject>(SlotPrefabPath);
                var itemTask = GameModule.Resource.LoadAssetAsync<GameObject>(ItemPrefabPath);

                m_slotPrefab = await slotTask;
                m_itemPrefab = await itemTask;
                m_prefabsLoaded = true;

                Log.Info($"预制体加载完成 - Slot: {m_slotPrefab != null}, Item: {m_itemPrefab != null}");

                // 初始化 BagSystem（如果还没初始化）
                if (!BagSystem.Instance.IsInitialized)
                {
                    BagSystem.Instance.Initialize(9);
                }
                else
                {
                    // 已经初始化，直接创建槽位并刷新
                    CreateSlotWidgets(BagSystem.Instance.SlotCount);
                    RefreshAllItems();
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"预制体加载失败: {e.Message}");
                m_prefabsLoaded = true;

                if (!BagSystem.Instance.IsInitialized)
                {
                    BagSystem.Instance.Initialize(9);
                }
            }
        }

        #endregion

        #region 事件处理（响应 BagData 变化）

        /// <summary>
        /// 背包初始化完成
        /// </summary>
        private void OnBagInitialized(int slotCount)
        {
            Log.Info($"[GameBagUI] 收到背包初始化事件，槽位数: {slotCount}");
            CreateSlotWidgets(slotCount);
        }

        /// <summary>
        /// 物品添加
        /// </summary>
        private void OnItemAdded(ItemData itemData)
        {
            Log.Info($"[GameBagUI] 收到物品添加事件: {itemData.Name} -> 槽位 {itemData.SlotIndex}");
            CreateItemWidget(itemData);
        }

        /// <summary>
        /// 物品移除
        /// </summary>
        private void OnItemRemoved(int itemId)
        {
            Log.Info($"[GameBagUI] 收到物品移除事件: ID={itemId}");
            RemoveItemWidget(itemId);
        }

        /// <summary>
        /// 物品移动
        /// </summary>
        private void OnItemMoved(int itemId, int fromSlotIndex, int toSlotIndex)
        {
            Log.Info($"[GameBagUI] 收到物品移动事件: ID={itemId}, {fromSlotIndex} -> {toSlotIndex}");
            RefreshItemPosition(itemId, toSlotIndex);
        }

        /// <summary>
        /// 背包已满
        /// </summary>
        private void OnBagFull()
        {
            Log.Warning("[GameBagUI] 背包已满！");
            // 可以在这里显示提示 UI
        }

        #endregion

        #region 槽位管理

        private void CreateSlotWidgets(int count)
        {
            // 清理旧的槽位
            foreach (var slot in m_slots)
            {
                if (slot.gameObject != null)
                {
                    Object.Destroy(slot.gameObject);
                }
            }
            m_slots.Clear();

            // 创建新的槽位
            for (int i = 0; i < count; i++)
            {
                CreateSlotWidget(i);
            }

            Log.Info($"[GameBagUI] 创建了 {count} 个槽位");
        }

        private Slot CreateSlotWidget(int index)
        {
            GameObject slotGo;

            if (m_slotPrefab != null)
            {
                slotGo = Object.Instantiate(m_slotPrefab, m_slotContainer);
                slotGo.SetActive(true);
            }
            else
            {
                slotGo = CreateSimpleSlotGo();
            }

            slotGo.name = $"Slot_{index}";

            var slot = new Slot();
            slot.Create(this, slotGo);
            slot.Init(index);

            m_slots.Add(slot);
            return slot;
        }

        private GameObject CreateSimpleSlotGo()
        {
            var go = new GameObject("Slot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(m_slotContainer, false);

            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(80, 80);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            return go;
        }

        #endregion

        #region 物品 Widget 管理

        private void CreateItemWidget(ItemData itemData)
        {
            if (itemData.SlotIndex < 0 || itemData.SlotIndex >= m_slots.Count)
            {
                Log.Error($"无效的槽位索引: {itemData.SlotIndex}");
                return;
            }

            var slot = m_slots[itemData.SlotIndex];

            GameObject itemGo;
            if (m_itemPrefab != null)
            {
                itemGo = Object.Instantiate(m_itemPrefab, slot.rectTransform);
                itemGo.SetActive(true);
            }
            else
            {
                itemGo = CreateSimpleItemGo(slot.rectTransform);
            }

            var item = new Item();
            item.Create(this, itemGo);
            item.Refresh(itemData);  // 使用新的 Refresh 方法
            item.BindToSlot(slot);

            m_itemWidgets[itemData.Id] = item;
        }

        private void RemoveItemWidget(int itemId)
        {
            if (m_itemWidgets.TryGetValue(itemId, out var item))
            {
                // 清理槽位引用
                var slot = item.CurrentSlot;
                slot?.ClearItem();

                // 销毁 Widget
                if (item.gameObject != null)
                {
                    Object.Destroy(item.gameObject);
                }

                m_itemWidgets.Remove(itemId);
            }
        }

        private void RefreshItemPosition(int itemId, int newSlotIndex)
        {
            if (!m_itemWidgets.TryGetValue(itemId, out var item))
                return;

            if (newSlotIndex < 0 || newSlotIndex >= m_slots.Count)
                return;

            var newSlot = m_slots[newSlotIndex];

            // 如果目标槽位有其他物品，也需要更新
            var existingItem = newSlot.CurrentItem;
            if (existingItem != null && existingItem != item)
            {
                // 获取原槽位
                var oldSlot = item.CurrentSlot;
                if (oldSlot != null)
                {
                    existingItem.BindToSlot(oldSlot);
                }
            }
            else
            {
                // 清空原槽位
                item.CurrentSlot?.ClearItem();
            }

            // 绑定到新槽位
            item.BindToSlot(newSlot);
        }

        private void RefreshAllItems()
        {
            // 清理旧的物品 Widget
            foreach (var kvp in m_itemWidgets)
            {
                if (kvp.Value.gameObject != null)
                {
                    Object.Destroy(kvp.Value.gameObject);
                }
            }
            m_itemWidgets.Clear();

            // 清理槽位引用
            foreach (var slot in m_slots)
            {
                slot.ClearItem();
            }

            // 根据当前数据重新创建
            foreach (var itemData in BagSystem.Instance.BagData.Items)
            {
                CreateItemWidget(itemData);
            }
        }

        private GameObject CreateSimpleItemGo(Transform parent)
        {
            var go = new GameObject("Item", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(60, 60);
            rectTransform.localPosition = Vector3.zero;

            var image = go.GetComponent<Image>();
            image.color = Color.white;

            return go;
        }

        #endregion

        #region 拖拽回调（供 Widget 调用）

        public Slot GetHoveredSlot() => m_hoveredSlot;

        public void OnItemBeginDrag(Item item)
        {
            m_draggingItem = item;
        }

        public void OnItemEndDrag(Item item, Slot targetSlot)
        {
            m_draggingItem = null;

            // 通过 BagSystem 移动物品（数据驱动）
            if (targetSlot != null && targetSlot != item.CurrentSlot)
            {
                BagSystem.Instance.MoveItem(item.ItemId, targetSlot.SlotIndex);
            }
        }

        public void OnItemClicked(Item item)
        {
            Log.Info($"点击物品: {item.ItemName} (ID: {item.ItemId})");
            // 可以在这里显示物品详情、使用物品等
        }

        public void OnSlotHoverEnter(Slot slot)
        {
            m_hoveredSlot = slot;
        }

        public void OnSlotHoverExit(Slot slot)
        {
            if (m_hoveredSlot == slot)
            {
                m_hoveredSlot = null;
            }
        }

        #endregion

        #region 按钮事件

        private partial void OnClick_CloneBagBtn()
        {
            GameModule.UI.HideUI<GameBagUI>();
        }

        private partial void OnClick_AddItemBtn()
        {
            // 通过 BagSystem 添加物品（数据驱动）
            string[] itemNames = { "剑", "盾", "药水", "金币", "宝石", "卷轴" };
            var randomName = itemNames[Random.Range(0, itemNames.Length)];
            BagSystem.Instance.TryAddItem(randomName);
        }

        #endregion
    }
}
