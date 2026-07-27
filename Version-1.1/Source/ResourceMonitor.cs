using System;
using Timberborn.Automation;
using Timberborn.AutomationBuildings;
using Timberborn.BaseComponentSystem;
using Timberborn.DuplicationSystem;
using Timberborn.EntitySystem;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;

namespace Calloatti.ResourceMonitor
{
  public class ResourceMonitor : BaseComponent, ISamplingTransmitter, ITransmitter, IPersistentEntity, IDuplicable<ResourceMonitor>, IDuplicable, IInitializableEntity, IDeletableEntity
  {
    private static readonly ComponentKey ResourceMonitorKey = new ComponentKey("ResourceMonitor");
    private static readonly PropertyKey<string> GoodIdKey = new PropertyKey<string>("GoodId");
    private static readonly PropertyKey<ResourceCounterMode> ModeKey = new PropertyKey<ResourceCounterMode>("Mode");
    private static readonly PropertyKey<bool> IncludeInputsKey = new PropertyKey<bool>("IncludeInputs");

    private static readonly PropertyKey<int> ThresholdOnKey = new PropertyKey<int>("ThresholdOn");
    private static readonly PropertyKey<float> FillRateOnKey = new PropertyKey<float>("FillRateOn");

    private static readonly PropertyKey<int> ThresholdOffKey = new PropertyKey<int>("ThresholdOff");
    private static readonly PropertyKey<float> FillRateOffKey = new PropertyKey<float>("FillRateOff");

    private static readonly PropertyKey<bool> CurrentlyActiveKey = new PropertyKey<bool>("CurrentlyActive");

    private readonly IGoodService _goodService;
    private readonly SamplingResourcesService _samplingResourcesService;

    private Automator _automator;
    private DistrictBuilding _districtBuilding;

    // FIX: Converted into a smart property. If early lifecycle methods query this before initialization,
    // it safely self-initializes on-demand as soon as the global goods registry is ready.
    private string _goodId;
    public string GoodId
    {
      get
      {
        if (string.IsNullOrEmpty(_goodId) && _goodService != null && _goodService.Goods.Count > 0)
        {
          _goodId = _goodService.Goods.Contains("Water") ? "Water" : _goodService.Goods[0];
        }
        return _goodId;
      }
      private set => _goodId = value;
    }

    public ResourceCounterMode Mode { get; private set; }
    public bool IncludeInputs { get; private set; }

    public int ThresholdOn { get; private set; } = 20;
    public float FillRateThresholdOn { get; private set; } = 0.2f;

    public int ThresholdOff { get; private set; } = 100;
    public float FillRateThresholdOff { get; private set; } = 0.8f;

    public int SampledResourceCount { get; private set; }
    public float SampledFillRate { get; private set; }

    private bool _currentlyActive;
    public event EventHandler<string> GoodChanged;

    internal ResourceMonitor(IGoodService goodService, SamplingResourcesService samplingResourcesService)
    {
      _goodService = goodService;
      _samplingResourcesService = samplingResourcesService;
    }

    public void Awake()
    {
      // No-op: Initialization is now securely handled on-demand by the smart property getter
    }

    public void InitializeEntity()
    {
      _automator = GetComponent<Automator>();
      _districtBuilding = GetComponent<DistrictBuilding>();

      if (_districtBuilding != null)
      {
        _districtBuilding.ReassignedInstantDistrict += OnReassignedInstantDistrict;
        _districtBuilding.ReassignedConstructionDistrict += OnReassignedConstructionDistrict;
      }

      Sample();
    }

    public void Start()
    {
      _automator?.SetState(_currentlyActive);
    }

    public void DeleteEntity()
    {
      if (_districtBuilding != null)
      {
        _districtBuilding.ReassignedInstantDistrict -= OnReassignedInstantDistrict;
        _districtBuilding.ReassignedConstructionDistrict -= OnReassignedConstructionDistrict;
      }
    }

    public void Save(IEntitySaver entitySaver)
    {
      var component = entitySaver.GetComponent(ResourceMonitorKey);
      component.Set(GoodIdKey, GoodId);
      component.Set(ModeKey, Mode);
      component.Set(IncludeInputsKey, IncludeInputs);

      component.Set(ThresholdOnKey, ThresholdOn);
      component.Set(FillRateOnKey, FillRateThresholdOn);

      component.Set(ThresholdOffKey, ThresholdOff);
      component.Set(FillRateOffKey, FillRateThresholdOff);

      component.Set(CurrentlyActiveKey, _currentlyActive);
    }

    public void Load(IEntityLoader entityLoader)
    {
      if (entityLoader.TryGetComponent(ResourceMonitorKey, out var objectLoader))
      {
        if (objectLoader.Has(GoodIdKey))
        {
          string loadedGood = objectLoader.Get(GoodIdKey);
          if (!string.IsNullOrEmpty(loadedGood)) { GoodId = loadedGood; }
        }

        if (objectLoader.Has(ModeKey)) Mode = objectLoader.Get(ModeKey);
        if (objectLoader.Has(IncludeInputsKey)) IncludeInputs = objectLoader.Get(IncludeInputsKey);

        if (objectLoader.Has(ThresholdOnKey)) ThresholdOn = objectLoader.Get(ThresholdOnKey);
        if (objectLoader.Has(FillRateOnKey)) FillRateThresholdOn = objectLoader.Get(FillRateOnKey);

        if (objectLoader.Has(ThresholdOffKey)) ThresholdOff = objectLoader.Get(ThresholdOffKey);
        if (objectLoader.Has(FillRateOffKey)) FillRateThresholdOff = objectLoader.Get(FillRateOffKey);

        if (objectLoader.Has(CurrentlyActiveKey)) _currentlyActive = objectLoader.Get(CurrentlyActiveKey);
      }
    }

    public void DuplicateFrom(ResourceMonitor source)
    {
      GoodId = source.GoodId;
      Mode = source.Mode;
      IncludeInputs = source.IncludeInputs;

      ThresholdOn = source.ThresholdOn;
      FillRateThresholdOn = source.FillRateThresholdOn;

      ThresholdOff = source.ThresholdOff;
      FillRateThresholdOff = source.FillRateThresholdOff;

      _currentlyActive = source._currentlyActive;

      InvokeGoodChangeEvent(source.GoodId);
      Sample();
    }

    public void SetGoodId(string goodId) { GoodId = goodId; InvokeGoodChangeEvent(goodId); Sample(); }
    public void SetMode(ResourceCounterMode mode) { Mode = mode; Sample(); }
    public void SetIncludeInputs(bool include) { IncludeInputs = include; Sample(); }

    public void SetThresholdOn(int threshold) { ThresholdOn = threshold; UpdateOutputState(); }
    public void SetFillRateThresholdOn(float fillRate) { FillRateThresholdOn = fillRate; UpdateOutputState(); }

    public void SetThresholdOff(int threshold) { ThresholdOff = threshold; UpdateOutputState(); }
    public void SetFillRateThresholdOff(float fillRate) { FillRateThresholdOff = fillRate; UpdateOutputState(); }

    public void Sample()
    {
      if (string.IsNullOrEmpty(GoodId) || _samplingResourcesService == null)
      {
        return;
      }

      DistrictCenter district = _districtBuilding != null ? _districtBuilding.GetInstantOrConstructionDistrict() : null;

      if (district != null)
      {
        var resourceCount = _samplingResourcesService.GetSampledResourceCount(district, GoodId);

        if (Mode == ResourceCounterMode.StockLevel)
        {
          SampledResourceCount = IncludeInputs ? resourceCount.AllStock : resourceCount.AvailableStock;
        }
        else
        {
          SampledFillRate = resourceCount.FillRate;
        }
      }
      else
      {
        SampledResourceCount = 0;
        SampledFillRate = 0f;
      }

      UpdateOutputState();
    }

    private void OnReassignedInstantDistrict(object sender, EventArgs e)
    {
      Sample();
    }

    private void OnReassignedConstructionDistrict(object sender, EventArgs e)
    {
      Sample();
    }

    private void UpdateOutputState()
    {
      if (_districtBuilding != null && _districtBuilding.GetInstantOrConstructionDistrict() == null)
      {
        _currentlyActive = false;
        _automator?.SetState(_currentlyActive);
        return;
      }

      bool targetState = _currentlyActive;

      if (Mode == ResourceCounterMode.StockLevel && SampledResourceCount <= ThresholdOn) targetState = true;
      if (Mode == ResourceCounterMode.FillRate && SampledFillRate <= FillRateThresholdOn) targetState = true;

      if (Mode == ResourceCounterMode.StockLevel && SampledResourceCount >= ThresholdOff) targetState = false;
      if (Mode == ResourceCounterMode.FillRate && SampledFillRate >= FillRateThresholdOff) targetState = false;

      if (targetState != _currentlyActive)
      {
        _currentlyActive = targetState;
        _automator?.SetState(_currentlyActive);
      }
    }

    private void InvokeGoodChangeEvent(string goodId)
    {
      this.GoodChanged?.Invoke(this, goodId);
    }
  }
}