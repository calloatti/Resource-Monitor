using System;
using Timberborn.AutomationBuildings;
using Timberborn.AutomationBuildingsUI;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.DropdownSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using UnityEngine.UIElements;

namespace Calloatti.ResourceMonitor
{
  internal class ResourceMonitorFragment : IEntityPanelFragment
  {
    private static readonly string ModeLocKeyPrefix = "Building.ResourceMonitor.Mode.";

    // Our new localization keys matching your CSV
    private static readonly string TurnOnIfLocKey = "Building.ResourceMonitor.TurnOnIf";
    private static readonly string TurnOffIfLocKey = "Building.ResourceMonitor.TurnOffIf";
    private static readonly string MeasurementQuantityLocKey = "Building.ResourceMonitor.MeasurementQuantity";
    private static readonly string MeasurementPercentLocKey = "Building.ResourceMonitor.MeasurementPercent";

    private readonly VisualElementLoader _visualElementLoader;
    private readonly DropdownItemsSetter _dropdownItemsSetter;
    private readonly RadioToggleFactory _radioToggleFactory;
    private readonly ILoc _loc;

    private ResourceMonitor _resourceMonitor;
    private ResourceMonitorGoodsDropdownProvider _goodsDropdownProvider;

    private VisualElement _root;
    private Dropdown _goodDropdown;
    private RadioToggle _modeRadioToggle;
    private Toggle _includeInputsToggle;
    private Label _measurement;

    // ON Controls
    private IntegerField _thresholdOnField;
    private Label _fillRateLabelOn;
    private PreciseSlider _fillRateSliderOn;

    // OFF Controls
    private IntegerField _thresholdOffField;
    private Label _fillRateLabelOff;
    private PreciseSlider _fillRateSliderOff;

    public ResourceMonitorFragment(
        VisualElementLoader visualElementLoader,
        DropdownItemsSetter dropdownItemsSetter,
        RadioToggleFactory radioToggleFactory,
        ILoc loc)
    {
      _visualElementLoader = visualElementLoader;
      _dropdownItemsSetter = dropdownItemsSetter;
      _radioToggleFactory = radioToggleFactory;
      _loc = loc;
    }

    public VisualElement InitializeFragment()
    {
      _root = _visualElementLoader.LoadVisualElement("Game/EntityPanel/ResourceCounterFragment");
      var bottomSection = _root.Q<VisualElement>("BottomSection");

      _modeRadioToggle = _radioToggleFactory.CreateLocalizable<ResourceCounterMode>(ModeLocKeyPrefix, _root.Q<VisualElement>("ModeRadioToggleContainer"));
      _modeRadioToggle.RadioButtonSelected += (sender, index) => {
        // Consistency Fix: Added null check
        if (_resourceMonitor != null)
        {
          _resourceMonitor.SetMode((ResourceCounterMode)index);
          UpdateFragment();
        }
      };

      _goodDropdown = _root.Q<Dropdown>("Good");
      _measurement = _root.Q<Label>("Measurement");

      _includeInputsToggle = _root.Q<Toggle>("Toggle");

      // Static Layout Padding: 27px total around checkbox (Only renders in Quantity mode)
      _includeInputsToggle.style.marginTop = 14;
      _includeInputsToggle.style.marginBottom = 13;

      _includeInputsToggle.RegisterValueChangedCallback(evt => {
        // Consistency Fix: Added null check
        if (_resourceMonitor != null)
        {
          _resourceMonitor.SetIncludeInputs(evt.newValue);
        }
      });

      // 1. Setup ON Controls
      var onWrapper = _root.Q<VisualElement>("ComparisonWrapper");
      onWrapper.Q<Dropdown>("ComparisonMode").ToggleDisplayStyle(false); // Destroy the dropdown completely

      _thresholdOnField = onWrapper.Q<IntegerField>("Threshold");
      _fillRateLabelOn = _root.Q<Label>("FillRateLabel");
      _fillRateSliderOn = _root.Q<PreciseSlider>("FillRateSlider");

      _thresholdOnField.isDelayed = true;
      _thresholdOnField.RegisterValueChangedCallback(evt => {
        if (_resourceMonitor != null)
        {
          int val = Math.Max(0, evt.newValue);

          _thresholdOnField.SetValueWithoutNotify(val);
          _resourceMonitor.SetThresholdOn(val);
        }
      });

      _fillRateSliderOn.SetStepWithoutNotify(0.01f);
      _fillRateSliderOn.SetValueChangedCallback(val => {
        if (_resourceMonitor != null)
        {
          _fillRateLabelOn.text = $"{Math.Round(val * 100)}%";
          _resourceMonitor.SetFillRateThresholdOn(val);
        }
      });

      // Use ILoc for the ON title
      var onTitle = new Label(_loc.T(TurnOnIfLocKey));
      onTitle.AddToClassList("game-text-normal");
      onTitle.style.marginTop = 10;
      bottomSection.Insert(bottomSection.IndexOf(onWrapper), onTitle);


      // 2. Setup OFF Controls
      var offTemplate = _visualElementLoader.LoadVisualElement("Game/EntityPanel/ResourceCounterFragment");
      var offWrapper = offTemplate.Q<VisualElement>("ComparisonWrapper");
      offWrapper.Q<Dropdown>("ComparisonMode").ToggleDisplayStyle(false); // Destroy the dropdown completely

      _thresholdOffField = offWrapper.Q<IntegerField>("Threshold");

      // Static Layout Padding: 13px (Only renders in Quantity mode)
      _thresholdOffField.style.marginBottom = 13;

      _fillRateLabelOff = offTemplate.Q<Label>("FillRateLabel");
      _fillRateSliderOff = offTemplate.Q<PreciseSlider>("FillRateSlider");

      _thresholdOffField.isDelayed = true;
      _thresholdOffField.RegisterValueChangedCallback(evt => {
        if (_resourceMonitor != null)
        {
          int val = Math.Max(0, evt.newValue);

          _thresholdOffField.SetValueWithoutNotify(val);
          _resourceMonitor.SetThresholdOff(val);
        }
      });

      _fillRateSliderOff.SetStepWithoutNotify(0.01f);
      _fillRateSliderOff.SetValueChangedCallback(val => {
        if (_resourceMonitor != null)
        {
          _fillRateLabelOff.text = $"{Math.Round(val * 100)}%";
          _resourceMonitor.SetFillRateThresholdOff(val);
        }
      });

      // Use ILoc for the OFF title
      var offTitle = new Label(_loc.T(TurnOffIfLocKey));
      offTitle.AddToClassList("game-text-normal");
      offTitle.style.marginTop = 10;
      bottomSection.Add(offTitle);
      bottomSection.Add(offWrapper);
      bottomSection.Add(_fillRateLabelOff);
      bottomSection.Add(_fillRateSliderOff);

      _root.ToggleDisplayStyle(false);
      return _root;
    }

    public void ShowFragment(BaseComponent entity)
    {
      _resourceMonitor = entity.GetComponent<ResourceMonitor>();
      if (_resourceMonitor != null)
      {
        _thresholdOnField.SetValueWithoutNotify(_resourceMonitor.ThresholdOn);
        _fillRateSliderOn.UpdateValuesWithoutNotify(_resourceMonitor.FillRateThresholdOn, 1f);

        _thresholdOffField.SetValueWithoutNotify(_resourceMonitor.ThresholdOff);
        _fillRateSliderOff.UpdateValuesWithoutNotify(_resourceMonitor.FillRateThresholdOff, 1f);

        _includeInputsToggle.SetValueWithoutNotify(_resourceMonitor.IncludeInputs);

        _goodsDropdownProvider = _resourceMonitor.GetComponent<ResourceMonitorGoodsDropdownProvider>();
        _dropdownItemsSetter.SetItems(_goodDropdown, _goodsDropdownProvider);

        _root.ToggleDisplayStyle(true);
      }
    }

    public void ClearFragment()
    {
      _resourceMonitor = null;
      _root.ToggleDisplayStyle(false);
    }

    public void UpdateFragment()
    {
      if (_resourceMonitor != null)
      {
        _modeRadioToggle.Update((int)_resourceMonitor.Mode);
        _fillRateSliderOn.SetMarker(_resourceMonitor.SampledFillRate);
        _fillRateSliderOff.SetMarker(_resourceMonitor.SampledFillRate);

        // Uses ILoc for the dynamic measurement text, passing in the arguments for the {0} placeholder.
        _measurement.text = _resourceMonitor.Mode == ResourceCounterMode.StockLevel
            ? _loc.T(MeasurementQuantityLocKey, _resourceMonitor.SampledResourceCount)
            : _loc.T(MeasurementPercentLocKey, Math.Round(_resourceMonitor.SampledFillRate * 100));

        _fillRateLabelOn.text = $"{Math.Round(_resourceMonitor.FillRateThresholdOn * 100)}%";
        _fillRateLabelOff.text = $"{Math.Round(_resourceMonitor.FillRateThresholdOff * 100)}%";

        bool isFillRate = _resourceMonitor.Mode == ResourceCounterMode.FillRate;

        _fillRateSliderOn.ToggleDisplayStyle(isFillRate);
        _fillRateLabelOn.ToggleDisplayStyle(isFillRate);
        _thresholdOnField.ToggleDisplayStyle(!isFillRate);

        _fillRateSliderOff.ToggleDisplayStyle(isFillRate);
        _fillRateLabelOff.ToggleDisplayStyle(isFillRate);
        _thresholdOffField.ToggleDisplayStyle(!isFillRate);

        _includeInputsToggle.ToggleDisplayStyle(!isFillRate);
      }
    }
  }
}