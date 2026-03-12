using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.DropdownSystem;
using Timberborn.Goods;
using Timberborn.GoodsUI;
using UnityEngine;

namespace Calloatti.ResourceMonitor
{
  internal class ResourceMonitorGoodsDropdownProvider : BaseComponent, IAwakableComponent, IStartableComponent, IExtendedTooltipDropdownProvider, IExtendedDropdownProvider, IDropdownProvider
  {
    private readonly IGoodService _goodService;
    private readonly GoodDescriber _goodDescriber;
    private ResourceMonitor _resourceMonitor;

    public IReadOnlyList<string> Items { get; private set; }

    public ResourceMonitorGoodsDropdownProvider(IGoodService goodService, GoodDescriber goodDescriber)
    {
      _goodService = goodService;
      _goodDescriber = goodDescriber;
    }

    public void Awake()
    {
      _resourceMonitor = GetComponent<ResourceMonitor>();
    }

    public void Start()
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
      if (string.IsNullOrEmpty(goodId)) return "None";
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