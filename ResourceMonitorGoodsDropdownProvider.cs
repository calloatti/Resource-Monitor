using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.DropdownSystem;
using Timberborn.EntitySystem; // FIXED: Required for IInitializableEntity
using Timberborn.Goods;
using Timberborn.GoodsUI;
using Timberborn.Localization;
using UnityEngine;

namespace Calloatti.ResourceMonitor
{
  // 1.1 FIX: Swapped out broken legacy Start methods for the official IInitializableEntity hook
  internal class ResourceMonitorGoodsDropdownProvider : BaseComponent, IAwakableComponent, IInitializableEntity, IExtendedTooltipDropdownProvider, IExtendedDropdownProvider, IDropdownProvider
  {
    private static readonly string AutomationNoneLocKey = "Automation.AutomationNone";

    private readonly IGoodService _goodService;
    private readonly GoodDescriber _goodDescriber;
    private readonly ILoc _loc;
    private ResourceMonitor _resourceMonitor;

    public IReadOnlyList<string> Items { get; private set; }

    public ResourceMonitorGoodsDropdownProvider(IGoodService goodService, GoodDescriber goodDescriber, ILoc loc)
    {
      _goodService = goodService;
      _goodDescriber = goodDescriber;
      _loc = loc;
    }

    public void Awake()
    {
      _resourceMonitor = GetComponent<ResourceMonitor>();
    }

    // 1.1 FIX: Populates the items array cleanly during initialization so it isn't null when clicked
    public void InitializeEntity()
    {
      Items = _goodService.Goods.OrderBy((string good) => FormatDisplayText(good, selected: false)).ToImmutableArray();
    }

    public string GetValue()
    {
      string currentGood = _resourceMonitor.GoodId;

      if (string.IsNullOrEmpty(currentGood) && Items != null && Items.Count > 0)
      {
        currentGood = Items.Contains("Water") ? "Water" : Items[0];
        _resourceMonitor.SetGoodId(currentGood);
      }

      return currentGood;
    }

    public void SetValue(string goodId)
    {
      if (!string.IsNullOrEmpty(goodId))
      {
        _resourceMonitor.SetGoodId(goodId);
      }
    }

    public string FormatDisplayText(string goodId, bool selected)
    {
      if (string.IsNullOrEmpty(goodId)) return _loc.T(AutomationNoneLocKey);

      return _goodService.GetGood(goodId).DisplayName.Value;
    }

    public Sprite GetIcon(string goodId)
    {
      if (string.IsNullOrEmpty(goodId)) return null;
      return _goodDescriber.GetIcon(goodId);
    }

    public ImmutableArray<string> GetItemClasses(string value) => ImmutableArray<string>.Empty;

    public string GetDropdownTooltip(string value) => FormatDisplayText(value, selected: false);
  }
}