using System.Collections.Generic;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 背包UI - 管理槽位和物品
    /// </summary>
    [Window(UILayer.UI, location: "GameBagUI")]
    public partial class GameBagUI
    {
        #region 字段

        // 槽位和物品列表
        private List<Slot> m_slots = new List<Slot>();
        private List<Item> m_items = new List<Item>();

        // 当前拖拽的物品
        private Item m_draggingItem;

        // 当前悬停的槽位
        private Slot m_hoveredSlot;

        // 预制体引用（需要在预制体中设置，或通过 UIBindComponent 获取）
        private GameObject m_slotPrefab;
        private GameObject m_itemPrefab;

        // 容器引用
        private Transform m_slotContainer;

        // 物品ID计数器
        private int m_itemIdCounter = 0;

        // 随机颜色用于演示
        private Color[] m_itemColors = new Color[]
        {
            Color.red, Color.green, Color.blue,
            Color.yellow, Color.cyan, Color.magenta
        };

        #endregion

        #region 生命周期

        protected override void OnCreate()
        {
            base.OnCreate();

            // 查找 ScrollRect 并使用其 content 作为槽位容器
            var scrollRect = gameObject.GetComponentInChildren<ScrollRect>();
            if (scrollRect != null && scrollRect.content != null)
            {
                m_slotContainer = scrollRect.content;
            }
            else
            {
                // 备用：直接使用自身
                m_slotContainer = transform;
            }

            // 查找现有的槽位预制体
            FindPrefabs();

            // 创建初始槽位
            CreateSlots(9);

            Log.Info("背包UI创建完成");
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            Log.Info("背包UI刷新");
        }

        protected override void OnDestroy()
        {
            m_slots.Clear();
            m_items.Clear();
            base.OnDestroy();
        }

        #endregion

        #region 槽位管理

        /// <summary>
        /// 查找预制体引用
        /// </summary>
        private void FindPrefabs()
        {
            // 尝试从子节点查找模板预制体
            var slotTemplate = m_slotContainer.Find("SlotTemplate");
            if (slotTemplate != null)
            {
                m_slotPrefab = slotTemplate.gameObject;
                m_slotPrefab.SetActive(false);
            }

            var itemTemplate = transform.Find("ItemTemplate");
            if (itemTemplate != null)
            {
                m_itemPrefab = itemTemplate.gameObject;
                m_itemPrefab.SetActive(false);
            }
        }

        /// <summary>
        /// 创建槽位
        /// </summary>
        private void CreateSlots(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateSlot(i);
            }
        }

        /// <summary>
        /// 创建单个槽位
        /// </summary>
        private Slot CreateSlot(int index)
        {
            GameObject slotGo;

            if (m_slotPrefab != null)
            {
                slotGo = Object.Instantiate(m_slotPrefab, m_slotContainer);
                slotGo.SetActive(true);
            }
            else
            {
                // 如果没有预制体，创建简单的槽位
                slotGo = CreateSimpleSlot();
            }

            slotGo.name = $"Slot_{index}";

            var slot = new Slot();
            slot.Create(this, slotGo);
            slot.Init(index);

            m_slots.Add(slot);
            return slot;
        }

        /// <summary>
        /// 创建简单槽位（无预制体时使用）
        /// </summary>
        private GameObject CreateSimpleSlot()
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

        #region 物品管理

        /// <summary>
        /// 添加物品到第一个空槽位
        /// </summary>
        public Item AddItem(string itemName)
        {
            // 找到第一个空槽位
            Slot emptySlot = null;
            foreach (var slot in m_slots)
            {
                if (slot.IsEmpty)
                {
                    emptySlot = slot;
                    break;
                }
            }

            if (emptySlot == null)
            {
                Log.Warning("背包已满！");
                return null;
            }

            return AddItemToSlot(emptySlot, itemName);
        }

        /// <summary>
        /// 添加物品到指定槽位
        /// </summary>
        private Item AddItemToSlot(Slot slot, string itemName)
        {
            GameObject itemGo;

            if (m_itemPrefab != null)
            {
                itemGo = Object.Instantiate(m_itemPrefab, slot.rectTransform);
                itemGo.SetActive(true);
            }
            else
            {
                // 如果没有预制体，创建简单的物品
                itemGo = CreateSimpleItem(slot.rectTransform);
            }

            var item = new Item();
            item.Create(this, itemGo);

            // 设置物品数据
            m_itemIdCounter++;
            var randomColor = m_itemColors[m_itemIdCounter % m_itemColors.Length];
            item.SetData(m_itemIdCounter, itemName, randomColor);

            // 绑定到槽位
            item.BindToSlot(slot);

            m_items.Add(item);

            Log.Info($"添加物品: {itemName} 到槽位 {slot.SlotIndex}");
            return item;
        }

        /// <summary>
        /// 创建简单物品（无预制体时使用）
        /// </summary>
        private GameObject CreateSimpleItem(Transform parent)
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

        #region 拖拽回调

        /// <summary>
        /// 获取当前悬停的槽位
        /// </summary>
        public Slot GetHoveredSlot()
        {
            return m_hoveredSlot;
        }

        /// <summary>
        /// 物品开始拖拽
        /// </summary>
        public void OnItemBeginDrag(Item item)
        {
            m_draggingItem = item;
            Log.Info($"开始拖拽: {item.ItemName}");
        }

        /// <summary>
        /// 物品结束拖拽
        /// </summary>
        public void OnItemEndDrag(Item item)
        {
            m_draggingItem = null;
            Log.Info($"结束拖拽: {item.ItemName}");
        }

        /// <summary>
        /// 物品被点击
        /// </summary>
        public void OnItemClicked(Item item)
        {
            Log.Info($"点击物品: {item.ItemName}");
            // 可以在这里显示物品详情、使用物品等
        }

        /// <summary>
        /// 槽位悬停进入
        /// </summary>
        public void OnSlotHoverEnter(Slot slot)
        {
            m_hoveredSlot = slot;
        }

        /// <summary>
        /// 槽位悬停离开
        /// </summary>
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
            // 添加一个随机物品
            string[] itemNames = { "剑", "盾", "药水", "金币", "宝石", "卷轴" };
            var randomName = itemNames[Random.Range(0, itemNames.Length)];
            AddItem(randomName);
        }

        #endregion
    }
}
