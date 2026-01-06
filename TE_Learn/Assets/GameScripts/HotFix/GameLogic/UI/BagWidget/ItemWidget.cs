using System;
using UnityEngine.UI;
using GameLogic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor.Graphs;

public class ItemWidget : UIWidget
{
    public BagItemData itemData;
    public GameBagUI ownerBagUI => OwnerWindow as GameBagUI;
    private EventTrigger eventTrigger;

    public GameBagSystem BagSystem => ownerBagUI.bagSystem;

    public Image itemImage;
    protected override void OnCreate()
    {
        eventTrigger = gameObject.AddComponent<EventTrigger>();
        itemImage = gameObject.GetComponent<Image>();

        AddTrigger(EventTriggerType.BeginDrag, OnBeginDrag);
        AddTrigger(EventTriggerType.Drag, OnDrag);
        AddTrigger(EventTriggerType.EndDrag, OnEndDrag);
        AddTrigger(EventTriggerType.PointerEnter,OnPointerEnter);
        AddTrigger(EventTriggerType.PointerExit,OnPointerExit);
    }
    public void AddTrigger(EventTriggerType triggerType,Action<BaseEventData> OnEventTrigger)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = triggerType;
        entry.callback.AddListener((data) => { OnEventTrigger(data); });
        eventTrigger.triggers.Add(entry);
    }
    public void OnPointerEnter(BaseEventData eventData)
    {
        BagSystem.currentPointerItem = itemData;
    }
    public void OnPointerExit(BaseEventData eventData)
    {
        if(BagSystem.currentPointerItem == itemData)
            BagSystem.currentPointerItem = null;
    }
    private void OnBeginDrag(BaseEventData data)
    {
        BagSystem.currentDragItem = itemData;
        if(BagSystem.currentPointerItem == itemData)
            BagSystem.currentPointerItem = null;
        itemImage.raycastTarget = false;
        
        this.transform.SetParent(ownerBagUI.transform);
    }
    private void OnDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;
        if (pointerData != null)
        {
            Vector3 globalMousePos;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform,
                pointerData.position,
                pointerData.pressEventCamera,
                out globalMousePos))
            {
                rectTransform.position = globalMousePos;
            }
        }
    }
    private void OnEndDrag(BaseEventData data)
    {
        this.transform.SetParent(ownerBagUI.slotWidgetList.Find(
            slotWidget => slotWidget.slotData.slotId == itemData.bagSlotData.slotId).transform);

        BagSystem.TrySwapItem();

        BagSystem.currentDragItem = null;
        itemImage.raycastTarget = true;
    }
}