using System;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Nomad.Commons.Configuration;
using Nomad.Commons.Plugin;

namespace Nomad.Plugin.CmdLinePlugin.Properties
{
  [SettingsProvider(typeof(ConfigurableSettingsProvider))]
  internal class Settings : ApplicationSettingsBase
  {
    private static Settings _Default;

    public static Settings Default
    {
      get { return _Default ?? (_Default = new Settings()); }
    }

    [UserScopedSetting]
    public string[] History
    {
      get { return (string[])this["History"] ?? new string[0]; }
      set { this["History"] = value; }
    }
  }

  [Guid("1305A76C-D68B-4604-97CC-5F6903276429")]
  [ExportExtension(typeof(ApplicationSettingsBase))]
  [ExtensionDependency(typeof(CommandLinePlugin))]
  public class SettingsReference : IObjectReference
  {
    public object GetRealObject(StreamingContext context)
    {
      return Settings.Default;
    }
  }
}
