using GameLogic;
using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包槽位 Widget - 接收物品拖放（MVE 模式重构版）
/// </summary>
public class Slot : UIWidget
{
    #region 字段

    // UI 组件
    private Image m_img_Background;

    // 状态
    public int SlotIndex { get; private set; }
    public Item CurrentItem { get; private set; }
    public bool IsEmpty => CurrentItem == null;

    // 悬停状态
    private Color m_normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private Color m_hoverColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    #endregion

    #region 生命周期

    protected override void OnCreate()
    {
        // 获取背景图片
        m_img_Background = gameObject.GetComponent<Image>();
        if (m_img_Background != null)
        {
            m_normalColor = m_img_Background.color;
            m_hoverColor = m_normalColor * 1.3f;
        }

        // 添加事件触发器
        var trigger = gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<EventTrigger>();
        }

        AddTrigger(trigger, EventTriggerType.PointerEnter, OnPointerEnter);
        AddTrigger(trigger, EventTriggerType.PointerExit, OnPointerExit);
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化槽位
    /// </summary>
    public void Init(int index)
    {
        SlotIndex = index;
        CurrentItem = null;
    }

    #endregion

    #region 物品管理

    /// <summary>
    /// 设置物品到此槽位
    /// </summary>
    public void SetItem(Item item)
    {
        CurrentItem = item;
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void ClearItem()
    {
        CurrentItem = null;
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

    private void OnPointerEnter(BaseEventData data)
    {
        // 高亮显示
        if (m_img_Background != null)
        {
            m_img_Background.color = m_hoverColor;
        }

        // 通知背包UI
        var bagUI = OwnerWindow as GameBagUI;
        bagUI?.OnSlotHoverEnter(this);
    }

    private void OnPointerExit(BaseEventData data)
    {
        // 恢复颜色
        if (m_img_Background != null)
        {
            m_img_Background.color = m_normalColor;
        }

        // 通知背包UI
        var bagUI = OwnerWindow as GameBagUI;
        bagUI?.OnSlotHoverExit(this);
    }

    #endregion
}
