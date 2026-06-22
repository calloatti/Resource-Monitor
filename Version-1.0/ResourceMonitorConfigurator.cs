using Bindito.Core;
using Timberborn.Automation;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;

namespace Calloatti.ResourceMonitor
{
  [Context("Game")]
  internal class ResourceMonitorConfigurator : Configurator
  {
    private class EntityPanelModuleProvider : IProvider<EntityPanelModule>
    {
      private readonly ResourceMonitorFragment _fragment;

      public EntityPanelModuleProvider(ResourceMonitorFragment fragment)
      {
        _fragment = fragment;
      }

      public EntityPanelModule Get()
      {
        EntityPanelModule.Builder builder = new EntityPanelModule.Builder();
        builder.AddMiddleFragment(_fragment);
        return builder.Build();
      }
    }

    protected override void Configure()
    {
      Bind<ResourceMonitor>().AsTransient();
      Bind<ResourceMonitorGoodsDropdownProvider>().AsTransient();
      Bind<ResourceMonitorBannerSetter>().AsTransient();
      Bind<ResourceMonitorFragment>().AsSingleton();

      MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
      MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule()
    {
      TemplateModule.Builder builder = new TemplateModule.Builder();
      builder.AddDecorator<ResourceMonitorSpec, ResourceMonitor>();

      // THIS IS THE MISSING LINK: It attaches the Automator component to your building!
      builder.AddDecorator<ResourceMonitor, Automator>();

      builder.AddDecorator<ResourceMonitor, ResourceMonitorGoodsDropdownProvider>();
      builder.AddDecorator<ResourceMonitor, ResourceMonitorBannerSetter>();
      builder.AddDecorator<ResourceMonitor, AutomatorIlluminator>();
      return builder.Build();
    }
  }
}