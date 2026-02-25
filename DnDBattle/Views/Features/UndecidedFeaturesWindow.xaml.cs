using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.ViewModels;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;

namespace DnDBattle.Views.Features
{
    /// <summary>
    /// Code-behind for the Undecided/Experimental Features window.
    /// Creates service instances and wires up UI controls for testing experimental features.
    /// </summary>
    public partial class UndecidedFeaturesWindow : Window
    {
        // Service instances for experimental features
        private readonly WeatherService _weatherService;
        private readonly ElevationService _elevationService;
        private readonly AnimatedTileService _animatedTileService;
        private readonly ProceduralMapService _proceduralMapService;
        private readonly TokenCustomizationService _tokenCustomizationService;
        private readonly MeasurementService _measurementService;
        private readonly DicePhysicsService _dicePhysicsService;
        private readonly DiceStatisticsService _diceStatisticsService;
        private readonly AccessibilityService _accessibilityService;

        public UndecidedFeaturesWindow()
        {
            InitializeComponent();

            // Initialize all experimental services
            _weatherService = new WeatherService(Options.WeatherMaxParticles);
            _elevationService = new ElevationService();
            _animatedTileService = new AnimatedTileService();
            _proceduralMapService = new ProceduralMapService();
            _tokenCustomizationService = new TokenCustomizationService();
            _measurementService = new MeasurementService();
            _dicePhysicsService = new DicePhysicsService();
            _diceStatisticsService = new DiceStatisticsService();
            _accessibilityService = new AccessibilityService();

            // Wire up dice settled event to update statistics display
            _dicePhysicsService.OnDiceSettled += OnDiceSettled;

            LoadCurrentValues();
            LoadAnimatedTileDefinitions();
        }

        /// <summary>
        /// Loads current Options values into all UI controls so they reflect saved state.
        /// </summary>
        private void LoadCurrentValues()
        {
            // Weather & Environment
            ChkEnableWeather.IsChecked = Options.EnableWeatherEffects;
            ChkEnableDayNight.IsChecked = Options.EnableDayNightCycle;
            SliderMaxParticles.Value = Options.WeatherMaxParticles;

            // Elevation
            ChkEnableElevation.IsChecked = Options.EnableElevationSystem;

            // Animated Tiles
            ChkEnableAnimatedTiles.IsChecked = Options.EnableAnimatedTiles;

            // Procedural Maps
            ChkEnableProceduralMaps.IsChecked = Options.EnableProceduralMapGeneration;

            // Token Customization
            ChkEnableTokenCustomization.IsChecked = Options.EnableTokenCustomization;

            // Measurements
            ChkEnableMeasurements.IsChecked = Options.EnableMeasurements;

            // Dice
            ChkEnableDice3D.IsChecked = Options.EnableDiceRoller3D;
            ChkEnableDiceStats.IsChecked = Options.EnableDiceStatistics;

            // Accessibility
            ChkHighContrast.IsChecked = Options.EnableHighContrast;
            ChkColorblindMode.IsChecked = Options.EnableColorblindMode;
            ChkDyslexiaFont.IsChecked = Options.EnableDyslexiaFont;
            ChkKeyboardNav.IsChecked = Options.EnableKeyboardNavigation;
            SliderUIScale.Value = Options.AccessibilityUIScale;

            UpdateAccessibilityPreview();
        }

        /// <summary>
        /// Populates the animated tile definitions ListBox with built-in definitions.
        /// </summary>
        private void LoadAnimatedTileDefinitions()
        {
            var definitions = AnimatedTileService.GetBuiltInDefinitions();
            LstAnimatedTiles.Items.Clear();
            foreach (var def in definitions)
            {
                LstAnimatedTiles.Items.Add($"{def.DisplayName} ({def.Category})");
                // Register each built-in definition so it can be placed
                _animatedTileService.RegisterDefinition(def);
            }
        }

        #region UD.1 Weather & Environment Handlers

        private void ChkEnableWeather_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableWeatherEffects = ChkEnableWeather.IsChecked == true;
        }

        private void ChkEnableDayNight_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableDayNightCycle = ChkEnableDayNight.IsChecked == true;
        }

        private void SliderMaxParticles_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtMaxParticles != null)
            {
                Options.WeatherMaxParticles = (int)SliderMaxParticles.Value;
                TxtMaxParticles.Text = ((int)SliderMaxParticles.Value).ToString();
            }
        }

        private void SliderIntensity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtIntensity != null)
                TxtIntensity.Text = SliderIntensity.Value.ToString("F1");
        }

        private void SliderWindAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtWindAngle != null)
                TxtWindAngle.Text = ((int)SliderWindAngle.Value).ToString();
        }

        private void SliderWindStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtWindStrength != null)
                TxtWindStrength.Text = SliderWindStrength.Value.ToString("F1");
        }

        /// <summary>
        /// Applies the selected weather type, intensity, time of day, and wind settings to the WeatherService.
        /// </summary>
        private void ApplyWeather_Click(object sender, RoutedEventArgs e)
        {
            // Parse weather type from ComboBox selection
            var weatherText = (CmbWeatherType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";
            if (!Enum.TryParse<WeatherType>(weatherText, out var weatherType))
                weatherType = WeatherType.None;

            double intensity = SliderIntensity.Value;
            _weatherService.SetWeather(weatherType, intensity);

            // Apply time of day
            var todText = (CmbTimeOfDay.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Day";
            if (Enum.TryParse<TimeOfDay>(todText, out var tod))
                _weatherService.SetTimeOfDay(tod);

            // Apply wind settings
            _weatherService.WindAngleDegrees = SliderWindAngle.Value;
            _weatherService.WindStrength = SliderWindStrength.Value;

            MessageBox.Show(
                $"Weather set to {weatherType} (intensity {intensity:F1})\n" +
                $"Time: {tod}, Wind: {SliderWindAngle.Value}° at {SliderWindStrength.Value:F1}\n" +
                $"Vision modifier: {_weatherService.GetVisionRangeModifier():F2}\n" +
                $"Lighting overlay opacity: {_weatherService.GetLightingOverlayOpacity():F2}",
                "Weather Applied", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region UD.2 Elevation Handlers

        private void ChkEnableElevation_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableElevationSystem = ChkEnableElevation.IsChecked == true;
        }

        /// <summary>
        /// Initializes the elevation grid with the specified dimensions.
        /// </summary>
        private void InitializeElevation_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtElevGridWidth.Text, out int w) || !int.TryParse(TxtElevGridHeight.Text, out int h) || w < 1 || h < 1)
            {
                MessageBox.Show("Enter valid grid dimensions.", "Elevation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _elevationService.Initialize(w, h);
            MessageBox.Show($"Elevation grid initialized ({w}×{h}).", "Elevation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetTerrainElevation_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtElevX.Text, out int x) || !int.TryParse(TxtElevY.Text, out int y) || !int.TryParse(TxtElevValue.Text, out int elev))
            {
                MessageBox.Show("Enter valid X, Y, and elevation values.", "Elevation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _elevationService.SetTerrainElevation(x, y, elev);
            MessageBox.Show($"Terrain at ({x},{y}) set to {elev} ft.", "Elevation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RaiseTerrain_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtElevX.Text, out int x) || !int.TryParse(TxtElevY.Text, out int y))
            {
                MessageBox.Show("Enter valid X and Y.", "Elevation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _elevationService.RaiseTerrain(x, y);
            int newElev = _elevationService.GetTerrainElevation(x, y);
            MessageBox.Show($"Terrain at ({x},{y}) raised to {newElev} ft.", "Elevation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LowerTerrain_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtElevX.Text, out int x) || !int.TryParse(TxtElevY.Text, out int y))
            {
                MessageBox.Show("Enter valid X and Y.", "Elevation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _elevationService.LowerTerrain(x, y);
            int newElev = _elevationService.GetTerrainElevation(x, y);
            MessageBox.Show($"Terrain at ({x},{y}) lowered to {newElev} ft.", "Elevation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Calculates falling damage dice based on the entered height.
        /// </summary>
        private void CalculateFallingDamage_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtFallingHeight.Text, out int height) || height < 0)
            {
                MessageBox.Show("Enter a valid height in feet.", "Falling Damage", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int dice = _elevationService.CalculateFallingDamageDice(height);
            TxtFallingResult.Text = $"Falling {height} ft = {dice}d6 bludgeoning damage.";
        }

        private void ResetElevation_Click(object sender, RoutedEventArgs e)
        {
            _elevationService.Reset();
            MessageBox.Show("Elevation grid reset.", "Elevation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region UD.3 Animated Tiles Handlers

        private void ChkEnableAnimatedTiles_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableAnimatedTiles = ChkEnableAnimatedTiles.IsChecked == true;
        }

        /// <summary>
        /// Places the selected animated tile definition at the specified grid coordinates.
        /// </summary>
        private void PlaceAnimatedTile_Click(object sender, RoutedEventArgs e)
        {
            if (LstAnimatedTiles.SelectedIndex < 0)
            {
                MessageBox.Show("Select a tile definition first.", "Animated Tiles", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TxtAnimX.Text, out int x) || !int.TryParse(TxtAnimY.Text, out int y))
            {
                MessageBox.Show("Enter valid X and Y.", "Animated Tiles", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var definitions = AnimatedTileService.GetBuiltInDefinitions();
            if (LstAnimatedTiles.SelectedIndex < definitions.Count)
            {
                var def = definitions[LstAnimatedTiles.SelectedIndex];
                _animatedTileService.PlaceAnimatedTile(def.Id, x, y);
                MessageBox.Show($"Placed '{def.DisplayName}' at ({x},{y}).", "Animated Tiles", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveAnimatedTile_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtAnimX.Text, out int x) || !int.TryParse(TxtAnimY.Text, out int y))
            {
                MessageBox.Show("Enter valid X and Y.", "Animated Tiles", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _animatedTileService.RemoveAnimatedTile(x, y);
            MessageBox.Show($"Removed animated tile at ({x},{y}).", "Animated Tiles", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearAnimatedTiles_Click(object sender, RoutedEventArgs e)
        {
            _animatedTileService.ClearAll();
            MessageBox.Show("All animated tiles cleared.", "Animated Tiles", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region UD.4 Procedural Map Generation Handlers

        private void ChkEnableProceduralMaps_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableProceduralMapGeneration = ChkEnableProceduralMaps.IsChecked == true;
        }

        private void SliderMapWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtMapWidth != null)
                TxtMapWidth.Text = ((int)SliderMapWidth.Value).ToString();
        }

        private void SliderMapHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtMapHeight != null)
                TxtMapHeight.Text = ((int)SliderMapHeight.Value).ToString();
        }

        private void SliderRoomCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtRoomCount != null)
                TxtRoomCount.Text = ((int)SliderRoomCount.Value).ToString();
        }

        private void SliderMinRoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtMinRoom != null)
                TxtMinRoom.Text = ((int)SliderMinRoom.Value).ToString();
        }

        private void SliderMaxRoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtMaxRoom != null)
                TxtMaxRoom.Text = ((int)SliderMaxRoom.Value).ToString();
        }

        /// <summary>
        /// Builds a ProceduralMapConfig from the form inputs and generates a map.
        /// </summary>
        private void GenerateMap_Click(object sender, RoutedEventArgs e)
        {
            var typeText = (CmbMapType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Dungeon (BSP)";
            var mapType = typeText switch
            {
                "Cave (Cellular Automata)" => MapGenerationType.Cave,
                "Arena" => MapGenerationType.Arena,
                _ => MapGenerationType.Dungeon
            };

            var config = new ProceduralMapConfig
            {
                Type = mapType,
                Width = (int)SliderMapWidth.Value,
                Height = (int)SliderMapHeight.Value,
                TargetRoomCount = (int)SliderRoomCount.Value,
                MinRoomSize = (int)SliderMinRoom.Value,
                MaxRoomSize = (int)SliderMaxRoom.Value
            };

            var result = _proceduralMapService.Generate(config);
            TxtMapResult.Text = $"Generated {config.Type} map ({result.Width}×{result.Height}) with {result.Rooms.Count} rooms.";
        }

        #endregion

        #region UD.5 Token Customization Handlers

        private void ChkEnableTokenCustomization_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableTokenCustomization = ChkEnableTokenCustomization.IsChecked == true;
        }

        private void SliderBorderThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtBorderThickness != null)
                TxtBorderThickness.Text = SliderBorderThickness.Value.ToString("F1");
        }

        private void SliderNamePlateFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtNamePlateFontSize != null)
                TxtNamePlateFontSize.Text = ((int)SliderNamePlateFontSize.Value).ToString();
        }

        /// <summary>
        /// Builds a TokenCustomization from the form and applies it to the currently selected token.
        /// </summary>
        private void ApplyTokenCustomization_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected token from the main view model
            var mainWindow = Owner as MainWindow;
            var vm = mainWindow?.DataContext as MainViewModel;
            var selectedToken = vm?.SelectedToken;

            if (selectedToken == null)
            {
                MessageBox.Show("No token selected. Select a token in the main window first.",
                    "Token Customization", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Parse shape from ComboBox
            var shapeText = (CmbTokenShape.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Circle";
            Enum.TryParse<TokenShape>(shapeText, out var shape);

            // Parse border style
            var borderStyleText = (CmbBorderStyle.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Solid";
            Enum.TryParse<TokenBorderStyle>(borderStyleText, out var borderStyle);

            // Parse name plate position
            var npPosText = (CmbNamePlatePosition.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Below";
            Enum.TryParse<NamePlatePosition>(npPosText, out var npPos);

            var customization = new TokenCustomization
            {
                TokenId = selectedToken.Id.ToString(),
                Shape = shape,
                BorderColorHex = TxtBorderColor.Text,
                BorderThickness = SliderBorderThickness.Value,
                BorderStyle = borderStyle,
                ShowGlow = ChkShowGlow.IsChecked == true,
                GlowColorHex = TxtGlowColor.Text,
                NamePlateAlwaysVisible = ChkNamePlateVisible.IsChecked == true,
                NamePlatePosition = npPos,
                NamePlateFontSize = SliderNamePlateFontSize.Value
            };

            _tokenCustomizationService.SetCustomization(customization);
            MessageBox.Show($"Customization applied to '{selectedToken.Name}'.",
                "Token Customization", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region UD.6 Measurements Handlers

        private void ChkEnableMeasurements_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableMeasurements = ChkEnableMeasurements.IsChecked == true;
        }

        /// <summary>
        /// Adds a distance or radius measurement based on form inputs.
        /// </summary>
        private void AddMeasurement_Click(object sender, RoutedEventArgs e)
        {
            var label = TxtMeasurementLabel.Text;
            var colorItem = (CmbMeasurementColor.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Blue (#4FC3F7)";

            // Extract hex color from the display string
            var colorHex = "#4FC3F7";
            int parenIdx = colorItem.IndexOf('(');
            if (parenIdx >= 0)
                colorHex = colorItem.Substring(parenIdx + 1).TrimEnd(')');

            var typeText = (CmbMeasurementType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Distance";

            string id;
            if (typeText == "Radius")
            {
                if (!int.TryParse(TxtMeasX1.Text, out int cx) || !int.TryParse(TxtMeasY1.Text, out int cy) ||
                    !int.TryParse(TxtMeasX2.Text, out int radius))
                {
                    MessageBox.Show("For radius: use X1,Y1 as center and X2 as radius.", "Measurement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                id = _measurementService.AddRadiusMeasurement(label, cx, cy, radius, colorHex);
            }
            else
            {
                if (!int.TryParse(TxtMeasX1.Text, out int x1) || !int.TryParse(TxtMeasY1.Text, out int y1) ||
                    !int.TryParse(TxtMeasX2.Text, out int x2) || !int.TryParse(TxtMeasY2.Text, out int y2))
                {
                    MessageBox.Show("Enter valid X1, Y1, X2, Y2 values.", "Measurement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                id = _measurementService.AddDistanceMeasurement(label, x1, y1, x2, y2, colorHex);
            }

            RefreshMeasurementsList();
        }

        private void RemoveMeasurement_Click(object sender, RoutedEventArgs e)
        {
            if (LstMeasurements.SelectedItem is string selected)
            {
                // Extract the ID from the display string (format: "[ID] Label - Distance")
                var idEnd = selected.IndexOf(']');
                if (idEnd > 1)
                {
                    var id = selected.Substring(1, idEnd - 1);
                    _measurementService.RemoveMeasurement(id);
                    RefreshMeasurementsList();
                }
            }
        }

        private void ToggleMeasurement_Click(object sender, RoutedEventArgs e)
        {
            if (LstMeasurements.SelectedItem is string selected)
            {
                var idEnd = selected.IndexOf(']');
                if (idEnd > 1)
                {
                    var id = selected.Substring(1, idEnd - 1);
                    _measurementService.ToggleVisibility(id);
                    RefreshMeasurementsList();
                }
            }
        }

        private void ClearMeasurements_Click(object sender, RoutedEventArgs e)
        {
            _measurementService.ClearAll();
            RefreshMeasurementsList();
        }

        /// <summary>
        /// Refreshes the measurements ListBox with current measurements and their distances.
        /// </summary>
        // VISUAL REFRESH
        private void RefreshMeasurementsList()
        {
            LstMeasurements.Items.Clear();
            foreach (var m in _measurementService.GetAllMeasurements())
            {
                double dist = _measurementService.CalculateDistanceFeet(m);
                var vis = m.IsVisible ? "👁" : "🚫";
                LstMeasurements.Items.Add($"[{m.Id.Substring(0, 8)}] {m.Label} - {dist:F0} ft {vis}");
            }
        }

        #endregion

        #region UD.8 Dice Roller Handlers

        private void ChkEnableDice3D_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableDiceRoller3D = ChkEnableDice3D.IsChecked == true;
        }

        private void ChkEnableDiceStats_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableDiceStatistics = ChkEnableDiceStats.IsChecked == true;
        }

        /// <summary>
        /// Rolls dice using the DicePhysicsService and immediately skips animation to get results.
        /// Records results in DiceStatisticsService for tracking.
        /// </summary>
        private void RollDice_Click(object sender, RoutedEventArgs e)
        {
            var typeText = (CmbDiceType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "D20";
            if (!Enum.TryParse<DiceType>(typeText, out var diceType))
                diceType = DiceType.D20;

            if (!int.TryParse(TxtDiceCount.Text, out int count) || count < 1)
                count = 1;

            _dicePhysicsService.Roll(diceType, count);
            // Skip animation to get immediate results
            _dicePhysicsService.SkipAnimation();
        }

        private void SkipDiceAnimation_Click(object sender, RoutedEventArgs e)
        {
            if (_dicePhysicsService.IsRolling)
                _dicePhysicsService.SkipAnimation();
        }

        /// <summary>
        /// Called when dice have settled; updates the result display and records statistics.
        /// </summary>
        private void OnDiceSettled(System.Collections.Generic.IReadOnlyList<DiceState> dice)
        {
            // Must dispatch to UI thread since events may fire from background
            Dispatcher.Invoke(() =>
            {
                int total = _dicePhysicsService.GetTotal();
                var individual = string.Join(", ", dice.Select(d => $"{d.Type}={d.FinalValue}"));
                TxtDiceResult.Text = $"Total: {total} ({individual})";

                // Record each die result in statistics
                foreach (var d in dice)
                {
                    _diceStatisticsService.RecordRoll(d.FinalValue, d.Sides);
                }

                UpdateDiceStatistics();
            });
        }

        /// <summary>
        /// Updates the dice statistics display with averages, luck score, and critical counts.
        /// </summary>
        private void UpdateDiceStatistics()
        {
            var typeText = (CmbDiceType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "D20";
            Enum.TryParse<DiceType>(typeText, out var diceType);
            int sides = (int)diceType;

            double avg = _diceStatisticsService.GetAverageRoll(sides);
            TxtDiceAverage.Text = $"Average ({typeText}): {avg:F2}";
            TxtDiceLuckScore.Text = $"Luck Score: {_diceStatisticsService.LuckScore:F2}";
            TxtDiceCrits.Text = $"Crits: {_diceStatisticsService.CriticalHitCount} hits, {_diceStatisticsService.CriticalFailCount} fails";
            TxtDiceTotalRolls.Text = $"Total Rolls: {_diceStatisticsService.TotalRolls}";
        }

        #endregion

        #region UD.9 Accessibility Handlers

        private void ChkHighContrast_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableHighContrast = ChkHighContrast.IsChecked == true;
        }

        private void ChkColorblindMode_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableColorblindMode = ChkColorblindMode.IsChecked == true;
        }

        private void ChkDyslexiaFont_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableDyslexiaFont = ChkDyslexiaFont.IsChecked == true;
            UpdateAccessibilityPreview();
        }

        private void ChkKeyboardNav_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableKeyboardNavigation = ChkKeyboardNav.IsChecked == true;
        }

        private void SliderUIScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtUIScale != null)
            {
                Options.AccessibilityUIScale = (int)SliderUIScale.Value;
                TxtUIScale.Text = $"{(int)SliderUIScale.Value}%";
                UpdateAccessibilityPreview();
            }
        }

        /// <summary>
        /// Handles colorblind mode ComboBox changes and applies the selected mode to the service.
        /// </summary>
        private void CmbColorblindMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var modeText = (CmbColorblindMode.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";
            if (_accessibilityService != null && Enum.TryParse<ColorblindMode>(modeText, out var mode))
            {
                _accessibilityService.SetColorblindMode(mode);
            }
        }

        /// <summary>
        /// Updates the accessibility preview section with current font family and scaled font size.
        /// </summary>
        private void UpdateAccessibilityPreview()
        {
            if (TxtAccessibilityFontFamily == null) return;

            var font = _accessibilityService.GetAccessibleFontFamily();
            TxtAccessibilityFontFamily.Text = $"Font: {font.Source}";

            double scaled = _accessibilityService.GetScaledFontSize(14);
            TxtAccessibilityFontSize.Text = $"Scaled Font Size (base 14): {scaled:F1}";
        }

        #endregion
    }
}
