using System.Collections.Generic;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// Every giveable item definition (<c>GameBalance.Me.itemDefs</c>) with its display name, the
/// category list derived from their types ("All" first, <c>None</c> shown as "Misc"), and the
/// search/category filter the item adder applies.
/// </summary>
internal sealed class ItemCatalog
{
    private readonly List<Entry> _all = new List<Entry>();
    private readonly List<ItemType?> _categoryTypes = new List<ItemType?>();
    private readonly List<string> _categoryLabels = new List<string>();

    public ItemCatalog()
    {
        Load();
        BuildCategories();
    }

    public int CategoryCount => _categoryLabels.Count;

    public string CategoryLabel(int index)
    {
        return _categoryLabels[index];
    }

    /// <summary>Fills <paramref name="into"/> with entries matching the id/name query and category.</summary>
    public void Filter(string query, int categoryIndex, List<Entry> into)
    {
        string lowerQuery = query.Trim().ToLowerInvariant();
        ItemType? category = _categoryTypes[categoryIndex];

        into.Clear();
        foreach (Entry e in _all)
        {
            if (category.HasValue && e.Type != category.Value)
            {
                continue;
            }
            if (lowerQuery.Length > 0 && !e.LowerId.Contains(lowerQuery) && !e.LowerName.Contains(lowerQuery))
            {
                continue;
            }
            into.Add(e);
        }
    }

    private void Load()
    {
        List<ItemDef> defs = GameBalance.Me?.itemDefs;
        if (defs == null)
        {
            return;
        }
        foreach (ItemDef def in defs)
        {
            if (def == null || string.IsNullOrEmpty(def.id) || def.id == ItemDef.EMPTY_ITEM_ID)
            {
                continue;
            }
            string name;
            try
            {
                name = def.GetHeader();
            }
            catch
            {
                name = def.id;
            }
            if (string.IsNullOrEmpty(name))
            {
                name = def.id;
            }
            _all.Add(new Entry
            {
                Def = def,
                Name = name,
                Type = def.type,
                LowerId = def.id.ToLowerInvariant(),
                LowerName = name.ToLowerInvariant(),
            });
        }
    }

    private void BuildCategories()
    {
        _categoryTypes.Add(null);
        _categoryLabels.Add("All");
        var seen = new HashSet<ItemType>();
        foreach (Entry e in _all)
        {
            if (seen.Add(e.Type))
            {
                _categoryTypes.Add(e.Type);
                _categoryLabels.Add(e.Type == ItemType.None ? "Misc" : e.Type.ToString());
            }
        }
    }

    public struct Entry
    {
        public ItemDef Def;
        public string Name;
        public string LowerId;
        public string LowerName;
        public ItemType Type;
    }
}
