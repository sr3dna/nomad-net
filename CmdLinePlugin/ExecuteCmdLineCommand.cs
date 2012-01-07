using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Nomad.Commons.Controls.Actions;
using Nomad.Commons.Plugin;
using Nomad.Shared;

namespace Nomad.Plugin
{
  [Guid("241B8F64-654C-49C6-A8C2-D1AC1D8857EE")]
  [ExportExtension(typeof(IAction))]
  [Category("catMisc")]
  [DisplayName("Execute Command Line")]
  [ToolboxItem(false)]
  public class ExecuteCmdLineCommand : CmdLineCommandBase, IAction, ISupportInitialize
  {
    private IAction _OpenCommand;

    public bool Execute(object source, object target)
    {
      CommandLinePanel CommandLine = CurrentCommandLine;
      if ((CommandLine != null) && !string.IsNullOrEmpty(CommandLine.CommandLine))
        return CommandLine.ExecuteCommandLine();
      if (_OpenCommand != null)
        return _OpenCommand.Execute(source, target);
      return false;
    }

    public ActionState Update(object source, object target)
    {
      CommandLinePanel CommandLine = CurrentCommandLine;
      if ((CommandLine != null) && !string.IsNullOrEmpty(CommandLine.CommandLine))
        return ActionState.Visible | ActionState.Enabled;
      if (_OpenCommand != null)
        return _OpenCommand.Update(source, target);
      return ActionState.Visible;
    }

    public void BeginInit()
    {
    }

    public void EndInit()
    {
      ICommandManager CommandManager = (ICommandManager)GetService(typeof(ICommandManager));
      _OpenCommand = CommandManager[DefaultCommands.Open];
    }
  }
}