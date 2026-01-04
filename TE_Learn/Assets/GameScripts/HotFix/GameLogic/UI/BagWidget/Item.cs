using GameLogic;
using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包物品 Widget - 支持拖拽（MVE 模式重构版）
/// </summary>
public class Item : UIWidget
{
    #region 字段

    // UI 组件
    private Image m_img_Icon;

    // 状态
    private Slot m_currentSlot;
    private Vector3 m_originalPosition;

    // 物品数据（从 ItemData 同步）
    public int ItemId { get; private set; }
    public string ItemName { get; private set; }

    /// <summary>
    /// 当前所在槽位
    /// </summary>
    public Slot CurrentSlot => m_currentSlot;

    #endregion

    #region 生命周期

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

    #endregion

    #region 数据刷新（MVE 核心：数据驱动显示）

    /// <summary>
    /// 根据 ItemData 刷新显示
    /// </summary>
    public void Refresh(ItemData itemData)
    {
        ItemId = itemData.Id;
        ItemName = itemData.Name;

        if (m_img_Icon != null)
        {
            m_img_Icon.color = itemData.IconColor;
        }

        gameObject.name = $"Item_{itemData.Id}_{itemData.Name}";
    }

    #endregion

    #region 槽位绑定

    /// <summary>
    /// 绑定到槽位
    /// </summary>
    public void BindToSlot(Slot slot)
    {
        // 清理旧槽位引用
        m_currentSlot?.ClearItem();

        m_currentSlot = slot;
        slot.SetItem(this);

        // 设置位置到槽位中心
        rectTransform.SetParent(slot.rectTransform);
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localScale = Vector3.one;
    }

    #endregion

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
        // 记录原始位置
        m_originalPosition = rectTransform.position;

        // 禁用射线检测，让下方UI可被检测
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

        // 通知背包UI
        var bagUI = OwnerWindow as GameBagUI;
        bagUI?.OnItemBeginDrag(this);
    }

    private void OnDrag(BaseEventData data)
    {
        var pointer = data as PointerEventData;

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
        var targetSlot = bagUI?.GetHoveredSlot();

        if (targetSlot != null && targetSlot != m_currentSlot)
        {
            // 通知 UI 层处理移动（UI 层会调用 BagSystem）
            bagUI?.OnItemEndDrag(this, targetSlot);
        }
        else
        {
            // 返回原位（先回到原槽位）
            if (m_currentSlot != null)
            {
                rectTransform.SetParent(m_currentSlot.rectTransform);
                rectTransform.localPosition = Vector3.zero;
            }
            else
            {
                rectTransform.position = m_originalPosition;
            }

            bagUI?.OnItemEndDrag(this, null);
        }
    }

    private void OnClick(BaseEventData data)
    {
        var bagUI = OwnerWindow as GameBagUI;
        bagUI?.OnItemClicked(this);
    }

    #endregion
}
