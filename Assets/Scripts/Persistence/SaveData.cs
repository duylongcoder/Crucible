using System;
using System.Collections.Generic;

namespace Alchemy.Persistence
{
    // Dạng JSON của file save (JsonUtility). ulong lưu dạng chuỗi, double lưu dạng bit (long) để khôi phục chính xác (I5).

    [Serializable]
    internal sealed class SaveFile
    {
        public int version;
        public string runDefId;
        public string seed;
        public List<CommandData> actions = new List<CommandData>();

        /// <summary>Số action đã áp khi chụp <see cref="snapshot"/>; khôi phục = snapshot + replay actions từ đây.</summary>
        public int snapshotAt;
        public SnapshotData snapshot;
    }

    [Serializable]
    internal sealed class CommandData
    {
        public string type;
        public int a;
        public int b;
        public int c;
        public int d;
    }

    [Serializable]
    internal sealed class SnapshotData
    {
        public int season;
        public int phase;
        public int brewsUsed;
        public int reputation;
        public int gold;
        public int wins;
        public int outcome;
        public int bagCapacity;
        public int rerolls;
        public string modifier;
        public string nextModifier;
        public List<ProductData> bag = new List<ProductData>();

        /// <summary>Theo thứ tự quét hàng; id rỗng = ô trống.</summary>
        public List<ProductData> grid = new List<ProductData>();
        public List<OfferData> offers = new List<OfferData>();
        public List<string> rewards = new List<string>();
        public List<int> priceFactors = new List<int>();
        public List<string> rng = new List<string>();
    }

    [Serializable]
    internal sealed class ProductData
    {
        public string id;
        public long essence;
        public long potency;
        public List<StatData> stats = new List<StatData>();
    }

    [Serializable]
    internal sealed class StatData
    {
        public string id;
        public long value;
        public bool revealed;
    }

    [Serializable]
    internal sealed class OfferData
    {
        public string id;
        public int price;
        public bool sold;
    }
}
