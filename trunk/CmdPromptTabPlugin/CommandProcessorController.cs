using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Nomad.Plugin
{
  public class CommandProcessorController : Component
  {
    private static object EventBeforeCommand = new object();
    private static object EventCurrentDirectoryChanged = new object();
    private static object EventIdle = new object();
    private static object EventSubstitute = new object();

    private int _MaxLines = 255;
    private bool _NoEcho; // = false;
    private bool _PushDirectory;// = false;
    private Encoding _StandardEncoding;

    private Process _CmdProcess;
    private TextBox _OutputBox;
    private StringBuilder _InputBuilder;
    private int _InputIndex;

    private string _CurrentDirectory;
    private EventWaitHandle _IdleEvent;
    private bool _SkipOutput;
    private object _OutputLock = new object();
    private object _DisposeLock = new object();

    public CommandProcessorController()
    {
      if (!Microsoft.OS.IsWinNT)
        throw new PlatformNotSupportedException();
    }

    protected virtual TextBox CreateOutputBox()
    {
      return new TextBox();
    }

    protected override void Dispose(bool disposing)
    {
      Application.Idle -= Event_ApplicationIdle;

      if (!disposing)
        return;

      lock (_DisposeLock)
      {
        if (_CmdProcess != null)
        {
          _CmdProcess.StandardInput.Close();
          if (!_CmdProcess.HasExited)
            _CmdProcess.Kill();
          _CmdProcess.Close();
          _CmdProcess = null;
        }

        if (!_OutputBox.IsDisposed && !_OutputBox.Disposing)
        {
          if (_OutputBox.InvokeRequired)
          {
            // To prevent dead-lock
            _OutputBox.Disposed -= Event_DisposedOrExited;
            _OutputBox.Invoke(new MethodInvoker(_OutputBox.Dispose));
          }
          else
            _OutputBox.Dispose();
        }
        _IdleEvent.Close();
      }
    }

    protected void InsertToInputBuffer(string text)
    {
      if (_InputIndex < _InputBuilder.Length)
      {
        ReplaceOutputText(_OutputBox.TextLength - _InputBuilder.Length + _InputIndex, 0, text);
        _InputBuilder.Insert(_InputIndex, text);
      }
      else
      {
        _OutputBox.AppendText(text);
        _InputBuilder.Append(text);
      }
      _InputIndex += text.Length;
    }

    protected void ReplaceOutputText(int start, int length, string text)
    {
      using (new LockWindowRedraw(_OutputBox, false))
      {
        _OutputBox.Select(start, length);
        _OutputBox.SelectedText = text;
      }
    }

    private string RecodeInputString(string text)
    {
      // Fix bug in Process class, it is not possible to change standard input encoding
      if (_CmdProcess.StandardInput.Encoding.CodePage != _StandardEncoding.CodePage)
      {
        byte[] CommandBytes = _StandardEncoding.GetBytes(text);
        return _CmdProcess.StandardInput.Encoding.GetString(CommandBytes);
      }
      return text;
    }

    protected void WriteToStandardInput(string text)
    {
      _CmdProcess.StandardInput.Write(RecodeInputString(text));
    }

    protected void WriteLineToStandardInput(string text)
    {
      _CmdProcess.StandardInput.WriteLine(RecodeInputString(text));
    }

    protected void UpdateCaretPosition()
    {
      int RememberSelectionStart = _OutputBox.SelectionStart;

      if (_InputIndex < _InputBuilder.Length)
        _OutputBox.Select(_OutputBox.TextLength - _InputBuilder.Length + _InputIndex, 0);
      else
        _OutputBox.Select(int.MaxValue, 0);

      if (RememberSelectionStart != _OutputBox.SelectionStart)
        _OutputBox.ScrollToCaret();
    }

    protected void UpdateLineCount()
    {
      int LineCount = _OutputBox.GetLineFromCharIndex(int.MaxValue);
      if (LineCount > MaxLines)
      {
        using (new LockWindowRedraw(_OutputBox.Parent, false))
        {
          _OutputBox.Select(0, _OutputBox.GetFirstCharIndexFromLine(LineCount - MaxLines));
          _OutputBox.SelectedText = string.Empty;
        }
      }
    }

    protected virtual void OnBeforeCommand(BeforeCommandEventArgs e)
    {
      var BeforeCommand = Events[EventBeforeCommand] as EventHandler<BeforeCommandEventArgs>;
      if (BeforeCommand != null)
        BeforeCommand(this, e);
    }

    protected virtual void OnCurrentDirectoryChanged(EventArgs e)
    {
      EventHandler CurrentDirectoryChanged = Events[EventCurrentDirectoryChanged] as EventHandler;
      if (CurrentDirectoryChanged != null)
        CurrentDirectoryChanged(this, e);
    }

    protected virtual void OnIdle(EventArgs e)
    {
      EventHandler Idle = Events[EventIdle] as EventHandler;
      if (Idle != null)
        Idle(this, e);
    }

    protected virtual void OnSubstitute(SubstituteEventArgs e)
    {
      var Substitute = Events[EventBeforeCommand] as EventHandler<SubstituteEventArgs>;
      if (Substitute != null)
        Substitute(this, e);
    }

    private void Event_ApplicationIdle(object sender, EventArgs e)
    {
      if (!_OutputBox.IsDisposed && !_OutputBox.Disposing)
      {
        UpdateLineCount();
        UpdateCaretPosition();
      }
    }

    private void Event_DisposedOrExited(object sender, EventArgs e)
    {
      Dispose();
    }

    private void OutputBox_HandleCreated(object sender, EventArgs e)
    {
      // Execute this handler only once
      _OutputBox.HandleCreated -= OutputBox_HandleCreated;

      Thread OutputThread = new Thread(CmdProcess_ReadStandardOutput_Thread);
      OutputThread.Name = "Read Standard Output";
      OutputThread.IsBackground = true;
      OutputThread.Start(_CmdProcess.StandardOutput);

      Thread ErrorThread = new Thread(CmdProcess_ReadStandardError_Thread);
      ErrorThread.Name = "Read Standard Error";
      ErrorThread.IsBackground = true;
      ErrorThread.Start(_CmdProcess.StandardError);

      if (_InputBuilder == null)
        _InputBuilder = new StringBuilder(80);
      Application.Idle += Event_ApplicationIdle;

      // Wait for cloned controller idle
      if (_SkipOutput)
      {
        WaitForIdle();
        _SkipOutput = false;
      }
    }

    private void OutputBox_KeyDown(object sender, KeyEventArgs e)
    {
      if (!Running)
      {
        Dispose();
        return;
      }

      // When CMD is not idling we will skip input buffer processing
      if (!Idling)
        return;

      switch (e.KeyData)
      {
        case Keys.Back:
          if (_InputIndex > 0)
          {
            ReplaceOutputText(_OutputBox.TextLength - _InputBuilder.Length + _InputIndex - 1, 1, string.Empty);

            if (_InputIndex < _InputBuilder.Length)
              _InputBuilder.Remove(_InputIndex - 1, 1);
            else
              _InputBuilder.Length--;
            _InputIndex--;

            UpdateCaretPosition();
          }
          e.SuppressKeyPress = true;
          break;
        case Keys.Return:
          ReplaceOutputText(_OutputBox.TextLength - _InputBuilder.Length, _InputBuilder.Length, string.Empty);

          BeforeCommandEventArgs BeforeCommandArgs = new BeforeCommandEventArgs(_InputBuilder.ToString());
          _InputBuilder.Length = 0;
          _InputIndex = 0;

          OnBeforeCommand(BeforeCommandArgs);
          if (!BeforeCommandArgs.Cancel)
          {
            if (EchoOff)
              _OutputBox.AppendText(BeforeCommandArgs.Command + "\r\n");
            WriteLineToStandardInput(BeforeCommandArgs.Command);
          }

          _OutputBox.Select(int.MaxValue, 0);
          _OutputBox.ScrollToCaret();
          e.SuppressKeyPress = true;
          break;
        case Keys.Escape:
          if (_InputBuilder.Length > 0)
          {
            ReplaceOutputText(_OutputBox.TextLength - _InputBuilder.Length, _InputBuilder.Length, string.Empty);

            _InputBuilder.Length = 0;
            _InputIndex = 0;

            UpdateCaretPosition();
          }
          e.SuppressKeyPress = true;
          break;
        case Keys.End:
          if (_InputIndex < _InputBuilder.Length)
          {
            _InputIndex = _InputBuilder.Length;
            UpdateCaretPosition();
          }
          break;
        case Keys.Home:
          if (_InputIndex > 0)
          {
            _InputIndex = 0;
            UpdateCaretPosition();
          }
          break;
        case Keys.Left:
          if (_InputIndex > 0)
          {
            _InputIndex--;
            UpdateCaretPosition();
          }
          break;
        case Keys.Right:
          if (_InputIndex < _InputBuilder.Length)
          {
            _InputIndex++;
            UpdateCaretPosition();
          }
          break;
        case Keys.Delete:
          if (_InputIndex < _InputBuilder.Length)
          {
            ReplaceOutputText(_OutputBox.TextLength - _InputBuilder.Length + _InputIndex, 1, string.Empty);

            if (_InputIndex < _InputBuilder.Length)
              _InputBuilder.Remove(_InputIndex, 1);
            else
              _InputBuilder.Length--;

            UpdateCaretPosition();
          }
          break;
        case Keys.Tab:
          e.SuppressKeyPress = true;
          int StartIndex = -1;
          int QuoteIndex = -1;
          int QuoteCount = 0;

          for (int I = _InputIndex - 1; I >= 0; I--)
          {
            if ((_InputBuilder[I] == ' ') && (StartIndex < 0))
              StartIndex = I;
            else
              if (_InputBuilder[I] == '"')
              {
                if (QuoteIndex < 0)
                  QuoteIndex = I;
                QuoteCount++;
              }
          }

          if (QuoteCount % 2 == 0)
          {
            QuoteIndex = -1;
            StartIndex++;
          }
          else
            StartIndex = QuoteIndex + 1;

          int EndIndex = _InputIndex;
          while ((EndIndex < _InputBuilder.Length) &&
            ((QuoteIndex < 0) || (_InputBuilder[EndIndex] != '"')) && ((QuoteIndex >= 0) || (_InputBuilder[EndIndex] != ' ')))
            EndIndex++;

          if (StartIndex < _InputIndex)
          {
            string OriginalString = _InputBuilder.ToString(StartIndex, _InputIndex - StartIndex);
            SubstituteEventArgs SubstituteArgs = new SubstituteEventArgs(OriginalString);
            OnSubstitute(SubstituteArgs);

            string ReplaceString = SubstituteArgs.SubstituteString;
            if (!string.Equals(OriginalString, ReplaceString))
            {
              if (QuoteIndex >= 0)
              {
                if (EndIndex >= _InputBuilder.Length)
                  ReplaceString += '"';
              }
              else
              {
                if (ReplaceString.IndexOf(' ') >= 0)
                  ReplaceString = '"' + ReplaceString + '"';
              }

              int ReplaceLength = EndIndex - StartIndex;
              ReplaceOutputText(_OutputBox.TextLength - _InputBuilder.Length + StartIndex, ReplaceLength, ReplaceString);
              _InputBuilder.Remove(StartIndex, ReplaceLength);
              _InputBuilder.Insert(StartIndex, ReplaceString);
              _InputIndex = StartIndex + ReplaceString.Length;
              UpdateCaretPosition();
            }
          }
          break;
        case Keys.V | Keys.Control:
        case Keys.Insert | Keys.Control:
          e.SuppressKeyPress = true;
          PasteFromClipboard();
          break;
        default:
          e.Handled = true;
          break;
      }
    }

    private void OutputBox_KeyPress(object sender, KeyPressEventArgs e)
    {
      if (!Running)
      {
        Dispose();
        return;
      }

      string KeyString = new string(e.KeyChar, 1);
      // When CMD is not idling we will skip input buffer processing
      if (Idling)
        InsertToInputBuffer(KeyString);
      else
      {
        if (EchoOff)
          _OutputBox.AppendText(KeyString);
        WriteToStandardInput(KeyString);
      }
    }

    private void OutputBox_MouseDown(object sender, MouseEventArgs e)
    {
      if (_OutputBox.Capture)
        _OutputBox.Capture = false;
    }

    private static string PeekRead(TextReader reader, char firstChar)
    {
      StringBuilder ResultBuilder = new StringBuilder(80);
      ResultBuilder.Append(firstChar);

      while (reader.Peek() >= 0)
      {
        int NextChar = reader.Read();
        if (NextChar >= 0)
          ResultBuilder.Append((char)NextChar);
      }

      return ResultBuilder.ToString();
    }

    private string ReadStreamAndOutput(TextReader reader, char firstChar)
    {
      lock (_OutputLock)
      {
        string Content = PeekRead(reader, (char)firstChar);

        lock (_DisposeLock)
        {
          if (_OutputBox.IsDisposed && _OutputBox.Disposing)
            return null;
        }

        if (!_SkipOutput && _OutputBox.IsHandleCreated)
          _OutputBox.Invoke(new Action<string>(_OutputBox.AppendText), Content);

        return Content;
      }
    }

    private void CmdProcess_ReadStandardError_Thread(object output)
    {
      using (TextReader StandardError = (TextReader)output)
      {
        int NextChar = StandardError.Read();
        while (NextChar >= 0)
        {
          if (ReadStreamAndOutput(StandardError, (char)NextChar) == null)
            return;
          NextChar = StandardError.Read();
        }
      }
    }

    private void CmdProcess_ReadStandardOutput_Thread(object output)
    {
      using (TextReader StandardOutput = (TextReader)output)
      {
        int NextChar = StandardOutput.Read();
        while (NextChar >= 0)
        {
          string Line = ReadStreamAndOutput(StandardOutput, (char)NextChar);
          if (Line == null)
            return;

          // Find current directory path from output
          string DirectoryPath = null;
          if ((Line.Length > 3) && (Line[Line.Length - 1] == '>'))
          {
            int LineStartIndex = Line.LastIndexOfAny(new char[] { '\r', '\n' }) + 1;
            int LineLength = Line.Length - LineStartIndex - 1;

            if ((LineStartIndex + 2 < Line.Length) &&
              char.IsLetter(Line, LineStartIndex) && (Line[LineStartIndex + 1] == ':') && (Line[LineStartIndex + 2] == '\\') &&
              (Line.IndexOfAny(Path.GetInvalidPathChars(), LineStartIndex + 3, LineLength - 3) < 0))
            {
              DirectoryPath = Line.Substring(LineStartIndex, LineLength);
              if (!Directory.Exists(DirectoryPath))
                DirectoryPath = null;
            }
          }

          lock (_DisposeLock)
          {
            if (_OutputBox.IsDisposed || _OutputBox.Disposing)
              return;

            // When current directory is found it means that CMD is idling
            if (DirectoryPath != null)
            {
              bool CurrentDirectoryChanged = !string.Equals(DirectoryPath, _CurrentDirectory, StringComparison.OrdinalIgnoreCase);
              _CurrentDirectory = DirectoryPath;
              _IdleEvent.Set();

              _OutputBox.BeginInvoke(new Action<EventArgs>(OnIdle), EventArgs.Empty);
              if (CurrentDirectoryChanged)
                _OutputBox.BeginInvoke(new Action<EventArgs>(OnCurrentDirectoryChanged), EventArgs.Empty);
            }
            else
              _IdleEvent.Reset();
          }

          NextChar = StandardOutput.Read();
        }
      }
    }

    public CommandProcessorController Clone()
    {
      CommandProcessorController Result = (CommandProcessorController)MemberwiseClone();
      Result._CmdProcess = null;
      Result._OutputBox = null;
      Result._InputBuilder = null;
      Result._InputIndex = 0;
      Result._IdleEvent = null;
      Result._SkipOutput = false;
      Result._OutputLock = new object();
      Result._DisposeLock = new object();

      if (Running)
      {
        Result._SkipOutput = true;
        if (Result.Start(_CurrentDirectory))
        {
          Result._OutputBox.Text = _OutputBox.Text;
          Result._InputBuilder = new StringBuilder(_InputBuilder.ToString());
          Result._InputIndex = _InputIndex;
        }
        else
          Result._SkipOutput = false;
      }

      return Result;
    }

    public void PasteFromClipboard()
    {
      string ClipboardText;

      try
      {
        ClipboardText = Clipboard.GetText();
      }
      catch (ExternalException)
      {
        return;
      }

      if (!string.IsNullOrEmpty(ClipboardText))
      {
        if (Idling)
        {
          int LineBreakIndex = ClipboardText.IndexOfAny(new char[] { '\r', '\n' });
          if (LineBreakIndex < 0)
            InsertToInputBuffer(ClipboardText);
          else
            if (LineBreakIndex > 0)
              InsertToInputBuffer(ClipboardText.Substring(0, LineBreakIndex));
        }
        else
          WriteToStandardInput(ClipboardText);
      }
    }

    public bool Start(string workingDirectory)
    {
      StringBuilder ArgumentBuilder = new StringBuilder();
      ArgumentBuilder.Append(' ');
      if (EchoOff)
        ArgumentBuilder.Append("/Q ");
      if (UnicodeOutput)
        ArgumentBuilder.Append("/U ");
      if (PushDirectory && !string.IsNullOrEmpty(workingDirectory))
        ArgumentBuilder.AppendFormat("/S /K PUSHD \"{0}\" ", workingDirectory);
      ArgumentBuilder.Length--;

      ProcessStartInfo CmdStartInfo = new ProcessStartInfo();
      CmdStartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
      CmdStartInfo.Arguments = ArgumentBuilder.ToString();
      if (!PushDirectory && !string.IsNullOrEmpty(workingDirectory))
        CmdStartInfo.WorkingDirectory = workingDirectory;
      CmdStartInfo.CreateNoWindow = true;
      CmdStartInfo.UseShellExecute = false;

      CmdStartInfo.RedirectStandardError = true;
      CmdStartInfo.RedirectStandardInput = true;
      CmdStartInfo.RedirectStandardOutput = true;

      if (_StandardEncoding == null)
        _StandardEncoding = DefaultEncoding;

      CmdStartInfo.StandardErrorEncoding = _StandardEncoding;
      CmdStartInfo.StandardOutputEncoding = _StandardEncoding;

      _CmdProcess = new Process();
      _CmdProcess.StartInfo = CmdStartInfo;
      _CmdProcess.EnableRaisingEvents = true;
      _CmdProcess.Exited += Event_DisposedOrExited;

      if (_CmdProcess.Start())
      {
        _OutputBox = CreateOutputBox();
        _OutputBox.AcceptsReturn = true;
        _OutputBox.AcceptsTab = true;
        _OutputBox.AllowDrop = true;
        _OutputBox.BackColor = Color.Black;
        _OutputBox.Font = new Font("Courier New", 10f);
        _OutputBox.ForeColor = Color.Silver;
        _OutputBox.Cursor = Cursors.Default;
        _OutputBox.Multiline = true;
        _OutputBox.ReadOnly = true;
        _OutputBox.ScrollBars = ScrollBars.Both;
        _OutputBox.WordWrap = true;
        _OutputBox.Disposed += Event_DisposedOrExited;
        _OutputBox.HandleCreated += OutputBox_HandleCreated;
        _OutputBox.KeyDown += OutputBox_KeyDown;
        _OutputBox.KeyPress += OutputBox_KeyPress;
        _OutputBox.MouseDown += OutputBox_MouseDown;

        _IdleEvent = new ManualResetEvent(false);
        return true;
      }

      _CmdProcess.Close();
      _CmdProcess = null;
      return false;
    }

    public void SendCommand(string command)
    {
      SendCommand(command, true);
    }

    public void SendCommand(string command, bool echo)
    {
      if (command == null)
        throw new ArgumentNullException();
      if (_OutputBox == null)
        throw new InvalidOperationException();

      if (_CmdProcess == null)
        throw new ObjectDisposedException("CommandProcessorController");
      else
        if (_CmdProcess.HasExited)
        {
          Dispose();
          throw new ObjectDisposedException("CommandProcessorController");
        }

      if (!_IdleEvent.WaitOne(0, false))
        throw new InvalidOperationException();

      BeforeCommandEventArgs Args = new BeforeCommandEventArgs(command);
      OnBeforeCommand(Args);
      if (Args.Cancel)
        return;

      // Clear input buffer if any, because its content can mess with command output
      if (_InputBuilder.Length > 0)
      {
        ReplaceOutputText(_OutputBox.TextLength - _InputBuilder.Length, _InputBuilder.Length, string.Empty);
        _InputBuilder.Length = 0;
        _InputIndex = 0;
      }

      if (echo && EchoOff)
        _OutputBox.AppendText(Args.Command + "\r\n");

      _SkipOutput = !echo;
      try
      {
        _IdleEvent.Reset();
        WriteLineToStandardInput(Args.Command);
        _IdleEvent.WaitOne();
      }
      finally
      {
        _SkipOutput = false;
      }
    }

    public void SetCurrentDirectory(string directoryPath)
    {
      if (directoryPath == null)
        throw new ArgumentNullException();
      if ((directoryPath.Length == 0) || (directoryPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0))
        throw new ArgumentException();

      // CMD does not support UNC paths as current directories
      string DirectoryRoot = Path.GetPathRoot(directoryPath);
      if (DirectoryRoot.Length != 3)
        throw new ArgumentException();

      // Preserve input buffer if any
      string CurrentInput = _InputBuilder.ToString();
      int CurrentInputIndex = _InputIndex;

      SendCommand("cd " + directoryPath, false);
      if (!_CurrentDirectory.StartsWith(DirectoryRoot, StringComparison.OrdinalIgnoreCase))
        SendCommand(DirectoryRoot.Substring(0, 2), false);

      // Display new directory path instead of last one
      ReplaceOutputText(_OutputBox.GetFirstCharIndexOfCurrentLine(), int.MaxValue, _CurrentDirectory + '>');
      // Restore and display input buffer if any
      if (CurrentInput.Length > 0)
      {
        _OutputBox.AppendText(CurrentInput);
        _InputBuilder = new StringBuilder(CurrentInput);
        _InputIndex = CurrentInputIndex;
        UpdateCaretPosition();
      }
    }

    public void WaitForIdle()
    {
      WaitForIdle(Timeout.Infinite);
    }

    public bool WaitForIdle(int millisecondsTimeout)
    {
      if (_OutputBox == null)
        throw new InvalidOperationException();
      return _IdleEvent.WaitOne(millisecondsTimeout, false);
    }

    public Process CommandProcessor
    {
      get { return _CmdProcess; }
    }

    public string CurrentDirectory
    {
      get { return _CurrentDirectory; }
    }

    public virtual Encoding DefaultEncoding
    {
      get { return Encoding.Default; }
    }

    public bool Idling
    {
      get
      {
        if (_OutputBox == null)
          throw new InvalidOperationException();
        return _IdleEvent.WaitOne(0, false);
      }
    }

    public bool EchoOff
    {
      get { return _NoEcho; }
      set
      {
        if (_OutputBox != null)
          throw new InvalidOperationException();
        _NoEcho = value;
      }
    }

    public int MaxLines
    {
      get { return _MaxLines; }
      set
      {
        if (value < 16)
          throw new ArgumentOutOfRangeException();
        _MaxLines = value;
        if ((_OutputBox != null) && _OutputBox.IsHandleCreated && !_OutputBox.IsDisposed && !_OutputBox.Disposing)
          UpdateLineCount();
      }
    }

    public bool PushDirectory
    {
      get { return _PushDirectory; }
      set
      {
        if (_OutputBox != null)
          throw new InvalidOperationException();
        _PushDirectory = value;
      }
    }

    public bool Running
    {
      get { return (_CmdProcess != null) && !_CmdProcess.HasExited; }
    }

    public TextBox OutputBox
    {
      get { return _OutputBox; }
    }

    public bool UnicodeOutput
    {
      get { return (_StandardEncoding != null) && (_StandardEncoding.CodePage == Encoding.Unicode.CodePage); }
      set
      {
        if (_OutputBox != null)
          throw new InvalidOperationException();
        _StandardEncoding = value ? Encoding.Unicode : null;
      }
    }

    public event EventHandler<BeforeCommandEventArgs> BeforeCommand
    {
      add { Events.AddHandler(EventBeforeCommand, value); }
      remove { Events.RemoveHandler(EventBeforeCommand, value); }
    }

    public event EventHandler CurrentDirectoryChanged
    {
      add { Events.AddHandler(EventCurrentDirectoryChanged, value); }
      remove { Events.RemoveHandler(EventCurrentDirectoryChanged, value); }
    }

    public event EventHandler Idle
    {
      add { Events.AddHandler(EventIdle, value); }
      remove { Events.RemoveHandler(EventIdle, value); }
    }

    public event EventHandler<SubstituteEventArgs> Substitute
    {
      add { Events.AddHandler(EventSubstitute, value); }
      remove { Events.RemoveHandler(EventSubstitute, value); }
    }
  }

  public class BeforeCommandEventArgs : CancelEventArgs
  {
    private string _Command;

    public BeforeCommandEventArgs(string command)
    {
      if (command == null)
        throw new ArgumentNullException();
      _Command = command;
    }

    public string Command
    {
      get { return _Command; }
      set
      {
        if (value == null)
          throw new ArgumentNullException();
        _Command = value;
      }
    }
  }

  public class SubstituteEventArgs : EventArgs
  {
    private string _SubstituteString;

    public SubstituteEventArgs(string substituteString)
    {
      _SubstituteString = substituteString;
    }

    public string SubstituteString
    {
      get { return _SubstituteString ?? string.Empty; }
      set { _SubstituteString = value; }
    }
  }
}