using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Remoting;
using System.Text;
using System.Windows.Forms;
using Nomad.Commons.Controls;
using Nomad.Commons.IO;
using Nomad.FileSystem.Virtual;
using Nomad.Shared;

namespace Nomad.Plugin
{
  [ToolboxItem(false)]
  public class CommandLinePanel : Panel
  {
    private IVirtualFolder _CurrentFolder;
    private Label _DirectoryLabel;
    private TextBox _CommandBox;

    public CommandLinePanel()
    {
      BackColor = SystemColors.Window;
      BorderStyle = BorderStyle.FixedSingle;
      Font = SystemFonts.IconTitleFont;
      Padding = new Padding(2);

      _DirectoryLabel = new Label();
      _DirectoryLabel.AutoSize = true;
      _DirectoryLabel.BackColor = SystemColors.Window;
      _DirectoryLabel.TextAlign = ContentAlignment.MiddleLeft;
      _DirectoryLabel.Dock = DockStyle.Left;

      _CommandBox = new TextBox();
      _CommandBox.BorderStyle = BorderStyle.None;
      _CommandBox.Dock = DockStyle.Fill;
      _CommandBox.TabStop = false;
      _CommandBox.AllowDrop = true;
      _CommandBox.DragEnter += CommandBox_DragEnter;
      _CommandBox.DragOver += CommandBox_DragOver;
      _CommandBox.DragLeave += CommandBox_DragLeave;
      _CommandBox.DragDrop += CommandBox_DragDrop;

      SuspendLayout();

      Height = _CommandBox.Height + 10;
      Controls.Add(_CommandBox);
      Controls.Add(_DirectoryLabel);

      ResumeLayout();
    }

    private void UpdateDirectoryLabel()
    {
      if (_CurrentFolder == null)
        _DirectoryLabel.Text = ">";
      else
      {
        string FullName = CurrentDirectory;
        if (!PathHelper.IsRootPath(FullName))
          FullName = PathHelper.ExcludeTrailingDirectorySeparator(FullName);
        _DirectoryLabel.Text = FullName + '>';
      }
    }

    private void CommandBox_DragEnter(object sender, DragEventArgs e)
    {
      DragImage.DragEnter((Control)sender, e);
      if (((e.AllowedEffect & (DragDropEffects.Copy | DragDropEffects.Link)) > 0) && e.Data.HasFileDrop())
        e.Effect = DragDropEffects.Copy;
      else
        e.Effect = DragDropEffects.None;
    }

    private void CommandBox_DragOver(object sender, DragEventArgs e)
    {
      DragImage.DragOver((Control)sender, e);
    }

    private void CommandBox_DragLeave(object sender, EventArgs e)
    {
      DragImage.DragLeave((Control)sender);
    }

    private void CommandBox_DragDrop(object sender, DragEventArgs e)
    {
      DragImage.DragLeave((Control)sender);
      foreach (string NextFileName in e.Data.GetFileDrop())
        _CommandBox.Text += PathHelper.EnquoteString(NextFileName) + ' ';
    }

    private string DequoteString(string value, out int endIndex)
    {
      if (value.StartsWith("\"", StringComparison.Ordinal))
      {
        StringBuilder ResultBuilder = new StringBuilder();
        endIndex = 1;
        while (endIndex < value.Length)
        {
          if (value[endIndex] == '"')
          {
            if ((++endIndex < value.Length) && (value[endIndex] == '"'))
              ResultBuilder.Append('"');
            else
              break;
          }
          else
            ResultBuilder.Append(value[endIndex++]);
        }
        return ResultBuilder.ToString();
      }
      endIndex = -1;
      return value;
    }

    private bool ExtractCommand(string commandLine, out string command, out string arguments)
    {
      command = string.Empty;
      arguments = string.Empty;
      if (string.IsNullOrEmpty(commandLine))
        return false;

      if (commandLine.StartsWith("\"", StringComparison.Ordinal))
      {
        int ArgumentsIndex;
        command = DequoteString(commandLine, out ArgumentsIndex);
        if (ArgumentsIndex > 0)
          arguments = commandLine.Substring(ArgumentsIndex).TrimStart();
      }
      else
      {
        int DelimIndex = commandLine.IndexOf(' ');
        if (DelimIndex < 0)
          command = commandLine;
        else
        {
          command = commandLine.Substring(0, DelimIndex);
          arguments = commandLine.Substring(DelimIndex + 1);
        }
      }

      return true;
    }

    private bool ExecuteCommandLine(string commandLine)
    {
      string Command;
      string Arguments;
      if (ExtractCommand(commandLine, out Command, out Arguments))
      {
        // Special processing for CD command
        if (Command.Equals("cd", StringComparison.OrdinalIgnoreCase))
        {
          int DummyIndex;
          string FolderPath = DequoteString(Arguments, out DummyIndex);
          if (!string.IsNullOrEmpty(FolderPath))
          {
            ITwoPanelTab TwoPanelTab = GetService(typeof(ITab)) as ITwoPanelTab;
            if (TwoPanelTab != null)
            {
              try
              {
                // Convert FolderPath string to virtual folder object
                IFileSystemManager FileSystemManager = (IFileSystemManager)GetService(typeof(IFileSystemManager));
                TwoPanelTab.CurrentPanel.CurrentFolder =
                  (IVirtualFolder)FileSystemManager.FromFullName(FolderPath, VirtualItemType.Folder, CurrentFolder);
              }
              catch (ArgumentException)
              {
                // Skip invalid FolderPath value, but it is nice to show warning here
                return false;
              }
            }
          }
          return true;
        }

        // Try to start new process
        try
        {
          ProcessStartInfo StartInfo = new ProcessStartInfo(Command, Arguments);

          string CurrentDir = CurrentDirectory;
          if (!string.IsNullOrEmpty(CurrentDir))
            StartInfo.WorkingDirectory = CurrentDir;

          Process.Start(StartInfo);
          return true;
        }
        catch (Win32Exception)
        {
          // Skip any start process error, but it is nice to show warning here
          return false;
        }
      }
      return false;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
      switch (keyData)
      {
        case Keys.Escape:
        case Keys.Up:
        case Keys.Down:
          if (keyData == Keys.Escape)
            _CommandBox.Clear();
          ITwoPanelTab CurrentTwoPanelTab = GetService(typeof(ITab)) as ITwoPanelTab;
          if ((CurrentTwoPanelTab != null) && CurrentTwoPanelTab.CurrentPanel.Activate())
            return true;
          else
            if ((Parent != null) && Parent.SelectNextControl(this, true, true, false, true))
              return true;
          break;
        case Keys.Return:
          if (ExecuteCommandLine())
            return true;
          break;
      }
      return base.ProcessCmdKey(ref msg, keyData);
    }

    public bool ExecuteCommandLine()
    {
      if (ExecuteCommandLine(CommandLine))
      {
        CommandLine = string.Empty;
        return true;
      }
      return false;
    }

    public string CommandLine
    {
      get { return _CommandBox.Text.Trim(); }
      set { _CommandBox.Text = value; }
    }

    public string CurrentDirectory
    {
      get
      {
        if (_CurrentFolder != null)
        {
          IGetFileSystemInfoService GetFileSystemInfo = _CurrentFolder.GetService<IGetFileSystemInfoService>();
          if (GetFileSystemInfo != null)
          {
            DirectoryInfo Info = GetFileSystemInfo.Info as DirectoryInfo;
            if (Info != null)
            {
              try
              {
                return Info.FullName;
              }
              catch (RemotingException)
              {
                // A little knowledge about Nomad's internals
                return _CurrentFolder.FullName;
              }
            }
          }
        }
        return null;
      }
    }

    public IVirtualFolder CurrentFolder
    {
      get { return _CurrentFolder; }
      set
      {
        if (value != null)
        {
          // Check is value is local system folder or find it in ancestor chain
          IGetFileSystemInfoService GetFileSystemInfo = value.GetService<IGetFileSystemInfoService>();
          while ((value != null) && ((GetFileSystemInfo == null) || !(GetFileSystemInfo.Info is DirectoryInfo)))
          {
            value = value.Parent;
            GetFileSystemInfo = value.GetService<IGetFileSystemInfoService>();
          }
          if ((GetFileSystemInfo == null) || !(GetFileSystemInfo.Info is DirectoryInfo))
            value = null;
        }
        _CurrentFolder = value;
        UpdateDirectoryLabel();
      }
    }
  }
}