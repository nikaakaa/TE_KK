using GameLogic;
using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包物品 Widget - 支持拖拽
/// </summary>
public class Item : UIWidget
{
    // UI 组件
    private Image m_img_Icon;
    private CanvasGroup m_canvasGroup;

    // 状态
    private Slot m_originalSlot;    // 原始所在槽位
    private Slot m_targetSlot;      // 拖拽目标槽位
    private Vector3 m_originalPosition;

    // 物品数据（简单示例）
    public int ItemId { get; private set; }
    public string ItemName { get; private set; }

    protected override void OnCreate()
    {
        // 获取组件引用
        m_img_Icon = transform.Find("Icon")?.GetComponent<Image>();
        if (m_img_Icon == null)
        {
            m_img_Icon = gameObject.GetComponent<Image>();
        }

        // 添加事件触发器
        var trigger = gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<EventTrigger>();
        }

        AddTrigger(trigger, EventTriggerType.BeginDrag, OnBeginDrag);
        AddTrigger(trigger, EventTriggerType.Drag, OnDrag);
        AddTrigger(trigger, EventTriggerType.EndDrag, OnEndDrag);
        AddTrigger(trigger, EventTriggerType.PointerClick, OnClick);
    }

    /// <summary>
    /// 设置物品数据
    /// </summary>
    public void SetData(int itemId, string itemName, Color color)
    {
        ItemId = itemId;
        ItemName = itemName;

        if (m_img_Icon != null)
        {
            m_img_Icon.color = color;
        }

        Log.Info($"物品设置: ID={itemId}, Name={itemName}");
    }

    /// <summary>
    /// 绑定到槽位
    /// </summary>
    public void BindToSlot(Slot slot)
    {
        m_originalSlot = slot;
        slot.SetItem(this);

        // 设置位置到槽位中心
        rectTransform.SetParent(slot.rectTransform);
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localScale = Vector3.one;
    }

    #region 事件处理

    private void AddTrigger(EventTrigger trigger, EventTriggerType type,
        UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    private void OnBeginDrag(BaseEventData data)
    {
        var pointer = data as PointerEventData;

        // 记录原始位置和父节点
        m_originalPosition = rectTransform.position;
        m_targetSlot = null;

        // // 禁用射线检测，让下方UI可被检测
        if (m_img_Icon != null)
        {
            m_img_Icon.raycastTarget = false;
        }

        // 将物品移到 Canvas 根节点，避免被 ScrollRect 的 Mask 裁剪
        var canvas = OwnerWindow.Canvas;
        if (canvas != null)
        {
            rectTransform.SetParent(canvas.transform);
        }

        // 提升层级，显示在最前
        rectTransform.SetAsLastSibling();

        // 通知背包UI当前正在拖拽的物品
        var bagUI = OwnerWindow as GameBagUI;
        bagUI?.OnItemBeginDrag(this);

        Log.Info($"开始拖拽物品: {ItemName}");
    }

    private void OnDrag(BaseEventData data)
    {
        var pointer = data as PointerEventData;

        // 将屏幕坐标转换为 Canvas 坐标
        var canvas = OwnerWindow?.Canvas;
        if (canvas != null)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform,
                pointer.position,
                canvas.worldCamera,
                out Vector3 worldPos);
            rectTransform.position = worldPos;
        }
    }

    private void OnEndDrag(BaseEventData data)
    {
        // 恢复射线检测
        if (m_img_Icon != null)
        {
            m_img_Icon.raycastTarget = true;
        }

        var bagUI = OwnerWindow as GameBagUI;
        m_targetSlot = bagUI?.GetHoveredSlot();

        if (m_targetSlot != null && m_targetSlot != m_originalSlot)
        {
            // 交换物品
            var otherItem = m_targetSlot.CurrentItem;

            if (otherItem != null)
            {
                // 槽位有物品，交换位置
                otherItem.BindToSlot(m_originalSlot);
            }
            else
            {
                // 槽位为空，清空原槽位
                m_originalSlot.ClearItem();
            }

            // 绑定到新槽位
            BindToSlot(m_targetSlot);
            Log.Info($"物品 {ItemName} 移动到新槽位");
        }
        else
        {
            // 返回原位
            rectTransform.position = m_originalPosition;
            Log.Info($"物品 {ItemName} 返回原位");
        }

        bagUI?.OnItemEndDrag(this);
    }

    private void OnClick(BaseEventData data)
    {
        Log.Info($"点击物品: {ItemName} (ID: {ItemId})");

        var bagUI = OwnerWindow as GameBagUI;
        bagUI?.OnItemClicked(this);
    }

    #endregion
}
