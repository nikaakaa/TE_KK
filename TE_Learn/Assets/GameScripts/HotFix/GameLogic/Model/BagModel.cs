using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class BagModel 
    {
        //这个应该是配置表?还是luban?还是SO?
        public static int SLOT_ID = 1001;
        public static int ITEM_ID = 1001;
        public static readonly string BagSlotPref = "Slot";
        public static readonly string BagItemPref = "Item";
        public List<BagItemData> items = new List<BagItemData>();
        public List<BagSlotData> slots = new List<BagSlotData>();
    }

    public class BagItemData
    {
        public int itemId;
        public int count;
        public Color color;
        public BagSlotData bagSlotData;
    }
    public class BagSlotData
    {
        public int slotId;
        public BagItemData haveItemData;
    }
}
