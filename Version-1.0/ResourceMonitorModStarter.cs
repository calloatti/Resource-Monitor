using System;
using Timberborn.Modding;
using Timberborn.ModManagerScene;

namespace Calloatti.ResourceMonitor
{
  public class ResourceMonitorModStarter : IModStarter
  {
    public void StartMod(IModEnvironment modEnvironment)
    {
      Console.WriteLine("[Calloatti.ResourceMonitor] Mod started successfully.");
    }
  }
}