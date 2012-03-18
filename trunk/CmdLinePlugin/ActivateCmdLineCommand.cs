using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Nomad.Commons.Controls.Actions;
using Nomad.Commons.Plugin;
using Nomad.Shared;

namespace Nomad.Plugin.CmdLinePlugin
{
  [Guid("A13EC0A6-9770-4D0F-B66D-5ECB5A92D9AC")]
  [ExportExtension(typeof(IAction))]
  [Category("catMisc")]
  [DisplayName("Activate Command Line")]
  [ToolboxItem(false)]
  public class ActivateCmdLineCommand : CmdLineCommandBase, IAction
  {
    public bool Execute(object source, object target)
    {
      CommandLinePanel CommandLine = CurrentCommandLine;
      return (CommandLine != null) && CommandLine.SelectNextControl(null, true, false, false, false);
    }

    public ActionState Update(object source, object target)
    {
      return ActionState.Visible | (CurrentCommandLine != null ? ActionState.Enabled : 0);
    }
  }
}