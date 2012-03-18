using System.Configuration;
using System.Runtime.InteropServices;
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

  [Guid("6BB5F467-BF3A-46BE-8ED5-335C259C3873")]
  [ExportExtension(typeof(ApplicationSettingsBase))]
  public class SettingsReference : IObjectReference
  {
    public object GetRealObject(StreamingContext context)
    {
      return Settings.Default;
    }
  }
}