using System.Configuration;
using System.Runtime.Serialization;
using Nomad.Commons.Plugin;

namespace Nomad.Plugin.CmdPrompt.Properties
{
  internal class CmdPromptSettingsProvider : PluginSettingsProviderBase
  {
    public CmdPromptSettingsProvider() : base(typeof(CmdPromptTabController)) { }
  }

  [SettingsProvider(typeof(CmdPromptSettingsProvider))]
  partial class Settings
  {
  }

  [ExportExtension(typeof(ApplicationSettingsBase))]
  public class SettingsReference : IObjectReference
  {
    public object GetRealObject(StreamingContext context)
    {
      return Settings.Default;
    }
  }
}