using GameLogic;
using TEngine;
using UnityEngine;

[EventInterface(EEventGroup.GroupUI)]
public interface IGameBagUIEvent
{
    void OnAddSlot(BagSlotData slotData);
    void OnAddItem(BagItemData itemData);
    void OnFailSwapOrMove(BagItemData dragItemData);
    void OnMoveItem(BagItemData dragItemData);
    void OnSwapItem(BagItemData dragItemData, BagItemData pointerItemData);
}