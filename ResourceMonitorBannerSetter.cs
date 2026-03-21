using System;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Buildings;
using Timberborn.EntitySystem;
using Timberborn.Goods;
using UnityEngine;

namespace Calloatti.ResourceMonitor
{
  public class ResourceMonitorBannerSetter : BaseComponent, IAwakableComponent, IStartableComponent, IDeletableEntity
  {
    private static readonly Color BannerIconColor = new Color(0.33f, 0.33f, 0.33f);

    private readonly GoodIconVisualizer _goodIconVisualizer;
    private readonly IGoodService _goodService;

    private BlockObject _blockObject;
    private ResourceMonitor _resourceMonitor;
    private MeshRenderer _meshRenderer;

    public ResourceMonitorBannerSetter(GoodIconVisualizer goodIconVisualizer, IGoodService goodService)
    {
      _goodIconVisualizer = goodIconVisualizer;
      _goodService = goodService;
    }

    public void Awake()
    {
      _blockObject = GetComponent<BlockObject>();
      _resourceMonitor = GetComponent<ResourceMonitor>();
      BuildingModel component = GetComponent<BuildingModel>();

      _meshRenderer = component.FinishedModel.GetComponentInChildren<MeshRenderer>();
    }

    public void Start()
    {
      _resourceMonitor.GoodChanged += OnGoodChanged;
      UpdateProperties();
    }

    public void DeleteEntity()
    {
      if (_resourceMonitor != null)
      {
        _resourceMonitor.GoodChanged -= OnGoodChanged;
      }
    }

    private void OnGoodChanged(object sender, string e)
    {
      UpdateProperties();
    }

    private void UpdateProperties()
    {
      string goodId = _resourceMonitor.GoodId;
      if (string.IsNullOrWhiteSpace(goodId))
      {
        _goodIconVisualizer.HideColoredIcon(_meshRenderer.material);
        return;
      }

      GoodSpec good = _goodService.GetGood(goodId);
      _goodIconVisualizer.ShowColoredIcon(_meshRenderer.material, good, _blockObject.FlipMode.IsFlipped, BannerIconColor);
    }
  }
}