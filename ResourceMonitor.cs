using System;
using Timberborn.Automation;
using Timberborn.AutomationBuildings;
using Timberborn.BaseComponentSystem;
using Timberborn.DuplicationSystem;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.Persistence;
using Timberborn.ResourceCountingSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace Calloatti.ResourceMonitor
{
  public class ResourceMonitor : BaseComponent, ISamplingTransmitter, ITransmitter, IPersistentEntity, IDuplicable<ResourceMonitor>, IDuplicable, IStartableComponent
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
    private readonly ResourceCountingService _resourceCountingService;

    private Automator _automator;
    private DistrictBuilding _districtBuilding;

    public string GoodId { get; private set; }
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

    internal ResourceMonitor(IGoodService goodService, ResourceCountingService resourceCountingService)
    {
      _goodService = goodService;
      _resourceCountingService = resourceCountingService;
    }

    public void Awake()
    {
      Debug.Log($"[ResourceMonitor] Awake called.");
      if (_goodService != null && _goodService.Goods.Count > 0 && string.IsNullOrEmpty(GoodId))
      {
        if (_goodService.Goods.Contains("Water")) { GoodId = "Water"; }
        else { GoodId = _goodService.Goods[0]; }
      }

      _automator = GetComponent<Automator>();
      _districtBuilding = GetComponent<DistrictBuilding>();

      Debug.Log($"[ResourceMonitor] Awake Fetch -> Automator: {_automator != null}, DistrictBuilding: {_districtBuilding != null}");

      if (_districtBuilding != null)
      {
        _districtBuilding.ReassignedDistrict += OnReassignedDistrict;
        _districtBuilding.ReassignedInstantDistrict += OnReassignedInstantDistrict;
        _districtBuilding.ReassignedConstructionDistrict += OnReassignedConstructionDistrict;
      }
    }

    public void Start()
    {
      Debug.Log($"[ResourceMonitor] Start called.");
      Sample();
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
      // RETRY LOGIC: If Awake missed them, keep trying every time Sample runs.
      if (_automator == null)
      {
        _automator = GetComponent<Automator>();
        if (_automator != null) Debug.Log($"[ResourceMonitor] Sample Retry -> Automator successfully found!");
      }

      if (_districtBuilding == null)
      {
        _districtBuilding = GetComponent<DistrictBuilding>();
        if (_districtBuilding != null)
        {
          Debug.Log($"[ResourceMonitor] Sample Retry -> DistrictBuilding successfully found! Binding events.");
          _districtBuilding.ReassignedDistrict += OnReassignedDistrict;
          _districtBuilding.ReassignedInstantDistrict += OnReassignedInstantDistrict;
          _districtBuilding.ReassignedConstructionDistrict += OnReassignedConstructionDistrict;
        }
      }

      if (string.IsNullOrEmpty(GoodId) || _resourceCountingService == null)
      {
        return;
      }

      DistrictCenter district = _districtBuilding != null ? _districtBuilding.District : null;

      if (district != null)
      {
        var districtCounter = _resourceCountingService.GetDistrictResourceCounter(district);
        var resourceCount = districtCounter.GetResourceCount(GoodId);

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
        Debug.Log($"[ResourceMonitor] District still NULL.");
        SampledResourceCount = 0;
        SampledFillRate = 0f;
      }

      UpdateOutputState();
    }

    // INDIVIDUAL EVENT HANDLERS TO LOG EXACTLY WHICH ONE FIRED
    private void OnReassignedDistrict(object sender, EventArgs e)
    {
      Debug.Log($"[ResourceMonitor] Event Fired: ReassignedDistrict");
      Sample();
    }

    private void OnReassignedInstantDistrict(object sender, EventArgs e)
    {
      Debug.Log($"[ResourceMonitor] Event Fired: ReassignedInstantDistrict");
      Sample();
    }

    private void OnReassignedConstructionDistrict(object sender, EventArgs e)
    {
      Debug.Log($"[ResourceMonitor] Event Fired: ReassignedConstructionDistrict");
      Sample();
    }

    private void UpdateOutputState()
    {
      bool isOffConditionMet = false;
      bool isOnConditionMet = false;

      if (Mode == ResourceCounterMode.StockLevel)
      {
        if (SampledResourceCount >= ThresholdOff) isOffConditionMet = true;
        if (SampledResourceCount <= ThresholdOn) isOnConditionMet = true;
      }
      else
      {
        if (SampledFillRate >= FillRateThresholdOff) isOffConditionMet = true;
        if (SampledFillRate <= FillRateThresholdOn) isOnConditionMet = true;
      }

      if (isOffConditionMet)
      {
        _currentlyActive = false;
      }
      else if (isOnConditionMet)
      {
        _currentlyActive = true;
      }

      // Output value spam removed as requested
      _automator?.SetState(_currentlyActive);
    }

    private void InvokeGoodChangeEvent(string goodId)
    {
      this.GoodChanged?.Invoke(this, goodId);
    }
  }
}