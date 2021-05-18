using GameOverlay.Drawing;
using GameOverlay.Windows;
using SRTPluginBase;
using SRTPluginProviderRE8;
using SRTPluginProviderRE8.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace SRTPluginUIRE2DirectXOverlay
{
    public class SRTPluginUIRE2DirectXOverlay : PluginBase, IPluginUI
    {
        internal static PluginInfo _Info = new PluginInfo();
        public override IPluginInfo Info => _Info;
        public string RequiredProvider => "SRTPluginProviderRE8";
        private IPluginHostDelegates hostDelegates;
        private IGameMemoryRE8 gameMemory;

        // DirectX Overlay-specific.
        private OverlayWindow _window;
        private Graphics _graphics;
        private SharpDX.Direct2D1.WindowRenderTarget _device;

        private Font _consolasBold;

        private SolidBrush _black;
        private SolidBrush _white;
        private SolidBrush _grey;
        private SolidBrush _darkred;
        private SolidBrush _red;
        private SolidBrush _lightred;
        private SolidBrush _lawngreen;
        private SolidBrush _goldenrod;
        private SolidBrush _greydark;
        private SolidBrush _greydarker;


        public PluginConfiguration config;
        private Process GetProcess() => Process.GetProcessesByName("re8")?.FirstOrDefault();
        private Process gameProcess;
        private IntPtr gameWindowHandle;

        private Dictionary<float, string> Bosses = new Dictionary<float, string>() {
            { 2900f, "Bella"},
            { 3400f, "Cassandra"},
            { 3700f, "Daniella"},
            { 9000f, "Lady D"},
            { 15000f, "Sturm"},
            { 25000f, "Urias"},
            { 26000f, "Moreau"},
            { 30000f, "Miranda"},
            { 100000f, "Heisenberg"},
        };

        [STAThread]
        public override int Startup(IPluginHostDelegates hostDelegates)
        {
            this.hostDelegates = hostDelegates;
            config = LoadConfiguration<PluginConfiguration>();

            gameProcess = GetProcess();
            if (gameProcess == default)
                return 1;
            gameWindowHandle = gameProcess.MainWindowHandle;

            DEVMODE devMode = default;
            devMode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            PInvoke.EnumDisplaySettings(null, -1, ref devMode);

            // Create and initialize the overlay window.
            _window = new OverlayWindow(0, 0, devMode.dmPelsWidth, devMode.dmPelsHeight);
            _window?.Create();

            // Create and initialize the graphics object.
            _graphics = new Graphics()
            {
                MeasureFPS = false,
                PerPrimitiveAntiAliasing = false,
                TextAntiAliasing = true,
                UseMultiThreadedFactories = false,
                VSync = false,
                Width = _window.Width,
                Height = _window.Height,
                WindowHandle = _window.Handle
            };
            _graphics?.Setup();

            // Get a refernence to the underlying RenderTarget from SharpDX. This'll be used to draw portions of images.
            _device = (SharpDX.Direct2D1.WindowRenderTarget)typeof(Graphics).GetField("_device", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_graphics);

            _consolasBold = _graphics?.CreateFont("Consolas", 12, true);

            _black = _graphics?.CreateSolidBrush(0, 0, 0);
            _white = _graphics?.CreateSolidBrush(255, 255, 255);
            _grey = _graphics?.CreateSolidBrush(128, 128, 128);
            _greydark = _graphics?.CreateSolidBrush(64, 64, 64);
            _greydarker = _graphics?.CreateSolidBrush(24, 24, 24);
            _darkred = _graphics?.CreateSolidBrush(153, 0, 0, 100);
            _red = _graphics?.CreateSolidBrush(255, 0, 0);
            _lightred = _graphics?.CreateSolidBrush(255, 183, 183);
            _lawngreen = _graphics?.CreateSolidBrush(124, 252, 0);
            _goldenrod = _graphics?.CreateSolidBrush(218, 165, 32);

            return 0;
        }

        public override int Shutdown()
        {
            SaveConfiguration(config);

            _goldenrod?.Dispose();
            _lawngreen?.Dispose();
            _red?.Dispose();
            _darkred?.Dispose();
            _grey?.Dispose();
            _white?.Dispose();
            _black?.Dispose();

            _consolasBold?.Dispose();

            _device = null; // We didn't create this object so we probably shouldn't be the one to dispose of it. Just set the variable to null so the reference isn't held.
            _graphics?.Dispose(); // This should technically be the one to dispose of the _device object since it was pulled from this instance.
            _graphics = null;
            _window?.Dispose();
            _window = null;

            gameProcess?.Dispose();
            gameProcess = null;

            return 0;
        }

        public int ReceiveData(object gameMemory)
        {
            this.gameMemory = (IGameMemoryRE8)gameMemory;
            _window?.PlaceAbove(gameWindowHandle);
            _window?.FitTo(gameWindowHandle, true);

            try
            {
                _graphics?.BeginScene();
                _graphics?.ClearScene();
                if (config.ScalingFactor != 1f)
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(config.ScalingFactor, 0f, 0f, config.ScalingFactor, 0f, 0f);
                DrawOverlay();
                if (config.ScalingFactor != 1f)
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
            }
            catch (Exception ex)
            {
                hostDelegates.ExceptionMessage.Invoke(ex);
            }
            finally
            {
                _graphics?.EndScene();
            }

            return 0;
        }

        private void DrawOverlay()
        {
            float baseXOffset = config.PositionX;
            float baseYOffset = config.PositionY;

            // Player HP
            float statsXOffset = baseXOffset + 5f;
            float statsYOffset = baseYOffset + 0f;

            var Percent = (gameMemory.PlayerCurrentHealth / gameMemory.PlayerMaxHealth) * 100;
            if (Percent > 66)
            {
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, statsXOffset, statsYOffset += 24, string.Format("Current HP: {0} {1:P1}", gameMemory.PlayerCurrentHealth, Percent / 100));
            }
            else if (Percent <= 66 && Percent > 33)
            {
                _graphics?.DrawText(_consolasBold, 20f, _goldenrod, statsXOffset, statsYOffset += 24, string.Format("Current HP: {0} {1:P1}", gameMemory.PlayerCurrentHealth, Percent / 100));
            }
            else if (Percent <= 33 && Percent > 0)
            {
                _graphics?.DrawText(_consolasBold, 20f, _red, statsXOffset, statsYOffset += 24, string.Format("Current HP: {0} {1:P1}", gameMemory.PlayerCurrentHealth, Percent / 100));
            }
            else
            {
                _graphics?.DrawText(_consolasBold, 20f, _red, statsXOffset, statsYOffset += 24, "Current HP: 0 0%");
            }

            // Stats
            _graphics?.DrawText(_consolasBold, 20f, _grey, statsXOffset, statsYOffset += 24, string.Format("X: {0} - Y: {1} - Z: {2}", gameMemory.PlayerPositionX.ToString("F3"), gameMemory.PlayerPositionY.ToString("F3"), gameMemory.PlayerPositionZ.ToString("F3")));
            _graphics?.DrawText(_consolasBold, 20f, _grey, statsXOffset, statsYOffset += 24, string.Format("DA Rank: {0} DA Score: {1}", gameMemory.Rank.ToString(), gameMemory.RankScore.ToString()));
            _graphics?.DrawText(_consolasBold, 20f, _grey, statsXOffset, statsYOffset += 24, string.Format("Current Chapter: {0}", gameMemory.CurrentChapter));
            _graphics?.DrawText(_consolasBold, 20f, _grey, statsXOffset, statsYOffset += 24, string.Format("Lei: {0}", gameMemory.Lei.ToString()));
            bool IsCutscene = (gameMemory.CutsceneState != 0xFFFFFFFF || gameMemory.CutsceneID != 0xFFFFFFFF); // WORK AROUND FOR CUTSCENES DUE TO INCONSISTANCIES FIX LATER
            _graphics?.DrawText(_consolasBold, 20f, _grey, statsXOffset, statsYOffset += 24, string.Format("Cutscene Playing: {0}", IsCutscene.ToString()));

            // Enemy HP
            var xOffset = config.EnemyHPPositionX == -1 ? statsXOffset : config.EnemyHPPositionX;
            var yOffset = config.EnemyHPPositionY == -1 ? statsYOffset : config.EnemyHPPositionY;
            _graphics?.DrawText(_consolasBold, 20f, _red, xOffset, yOffset += 34f, "Enemy HP");
            foreach (EnemyHP enemyHP in gameMemory.EnemyHealth.Where(a => a.IsAlive).OrderBy(a => a.IsTrigger).ThenBy(a => a.Percentage).ThenByDescending(a => a.CurrentHP))
                DrawProgressBar(ref xOffset, ref yOffset, enemyHP.CurrentHP, enemyHP.MaximumHP, enemyHP.Percentage); 
        }

        private void DrawProgressBar(ref float xOffset, ref float yOffset, float chealth, float mhealth, float percentage = 1f)
        {
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 400f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 397f, yOffset + 20f);
            _graphics.FillRectangle(_darkred, xOffset + 1f, yOffset + 1f, xOffset + (397f * percentage), yOffset + 20f);
            if (Bosses.ContainsKey(mhealth))
            {
                _graphics.DrawText(_consolasBold, 20f, _lightred, xOffset + 10f, yOffset - 2f, string.Format("{0}: {1} / {2}", Bosses[mhealth].ToUpper(), chealth, mhealth));
                _graphics.DrawText(_consolasBold, 20f, _lightred, xOffset + 336f, yOffset - 2f, string.Format("{0:P1}", percentage));
            }
            else
            {
                _graphics.DrawText(_consolasBold, 20f, _lightred, xOffset + 10f, yOffset - 2f, string.Format("{0} / {1}", chealth, mhealth));
                _graphics.DrawText(_consolasBold, 20f, _lightred, xOffset + 336f, yOffset - 2f, string.Format("{0:P1}", percentage));
            }
            
        }
    }
}
