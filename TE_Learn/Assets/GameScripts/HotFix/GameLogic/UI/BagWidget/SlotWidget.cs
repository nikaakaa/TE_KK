using System;
using GameLogic;
using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlotWidget : UIWidget
{
    public BagSlotData slotData;
    public GameBagUI ownerBagUI => OwnerWindow as GameBagUI;
    private EventTrigger eventTrigger;
    public GameBagSystem BagSystem => ownerBagUI.bagSystem;
    protected override void OnCreate()
    {
        eventTrigger = gameObject.AddComponent<EventTrigger>();
        AddTrigger(EventTriggerType.PointerEnter, OnPointerEnter);
        AddTrigger(EventTriggerType.PointerExit, OnPointerExit);
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
        BagSystem.currentPointerSlot = slotData;
    }
    public void OnPointerExit(BaseEventData eventData)
    {
        if(BagSystem.currentPointerSlot == slotData)
            BagSystem.currentPointerSlot = null;
    }
}