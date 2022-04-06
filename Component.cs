using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.ASL;
using LiveSplit.Model;
using LiveSplit.Options;
using HighPrecisionTimer;

namespace LiveSplit.UI.Components
{
    public class ASLComponent : LogicComponent
    {
        public override string ComponentName => "Scriptable Auto Splitter (High Precision)";

        // public so other components (ASLVarViewer) can access
        public ASLScript Script { get; private set; }

        public event EventHandler ScriptChanged;

        private bool _do_reload;
        private string _old_script_path;

        private MultimediaTimer _update_timer;
        private FileSystemWatcher _fs_watcher;

        private ComponentSettings _settings;

        private LiveSplitState _state;

        private readonly SynchronizationContext _mainThreadSynchronizationContext;
        private readonly object _lockObject;

        public ASLComponent(LiveSplitState state)
        {
            _state = state;

            _settings = new ComponentSettings();

            _fs_watcher = new FileSystemWatcher();

            async void handler<T>(object sender, T args)
            {
              await Task.Delay(200);
              _do_reload = true;
            };

            _fs_watcher.Changed += handler;
            _fs_watcher.Renamed += handler;

            _mainThreadSynchronizationContext = SynchronizationContext.Current;
            _lockObject = new object();

            _update_timer = new MultimediaTimer() { Interval = 15, Resolution = 0 };
            _update_timer.Elapsed += (sender, args) => TimerCallbackFunction();
            _update_timer.Start();
        }

        public ASLComponent(LiveSplitState state, string script_path)
            : this(state)
        {
            _settings = new ComponentSettings(script_path);
        }

        public override void Dispose()
        {
            ScriptCleanup();

            try
            {
                ScriptChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            _fs_watcher?.Dispose();
            _update_timer?.Dispose();
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return _settings;
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return _settings.GetSettings(document);
        }

        public override void SetSettings(XmlNode settings)
        {
            _settings.SetSettings(settings);
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height,
            LayoutMode mode) { }

        private void TimerCallbackFunction()
        {
            if (Monitor.TryEnter(_lockObject))
            {
                try
                {
                    _mainThreadSynchronizationContext.Send((o) => UpdateScript(), null);
                }
                finally
                {
                    Monitor.Exit(_lockObject);
                }
            }
        }

        private void UpdateScript()
        {
            // this is ugly, fix eventually!
            if (_settings.ScriptPath != _old_script_path || _do_reload)
            {
                try
                {
                    _do_reload = false;
                    _old_script_path = _settings.ScriptPath;

                    ScriptCleanup();

                    if (string.IsNullOrEmpty(_settings.ScriptPath))
                    {
                        // Only disable file watcher if script path changed to empty
                        // (otherwise detecting file changes may still be wanted)
                        _fs_watcher.EnableRaisingEvents = false;
                    }
                    else
                    {
                        LoadScript();
                    }

                    ScriptChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            if (Script != null)
            {
                try
                {
                    Script.Update(_state);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }

        private void LoadScript()
        {
            Log.Info("[ASL] Loading new script: " + _settings.ScriptPath);

            _fs_watcher.Path = Path.GetDirectoryName(_settings.ScriptPath);
            _fs_watcher.Filter = Path.GetFileName(_settings.ScriptPath);
            _fs_watcher.EnableRaisingEvents = true;

            // New script
            Script = ASLParser.Parse(File.ReadAllText(_settings.ScriptPath));

            Script.RefreshRateChanged += (sender, rate) => { _update_timer.Interval = Math.Max((int)Math.Round(1000 / rate), 1); _update_timer.Stop(); _update_timer.Start(); } ;
            
            _update_timer.Interval = Math.Max((int)Math.Round(1000 / Script.RefreshRate), 1);
            _update_timer.Stop();
            _update_timer.Start();

            Script.GameVersionChanged += (sender, version) => _settings.SetGameVersion(version);
            _settings.SetGameVersion(null);

            // Give custom ASL settings to GUI, which populates the list and
            // stores the ASLSetting objects which are shared between the GUI
            // and ASLScript
            try
            {
                ASLSettings settings = Script.RunStartup(_state);
                _settings.SetASLSettings(settings);
            }
            catch (Exception ex)
            {
                // Script already created, but startup failed, so clean up again
                Log.Error(ex);
                ScriptCleanup();
            }
        }

        private void ScriptCleanup()
        {
            if (Script == null)
                return;

            try
            {
                Script.RunShutdown(_state);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                _settings.SetGameVersion(null);
                _settings.ResetASLSettings();

                // Script should no longer be used, even in case of error
                // (which the ASL shutdown method may contain)
                Script = null;
            }
        }
    }
}
