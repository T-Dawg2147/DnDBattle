using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using DnDBattle.Models.Tiles;
using Action = DnDBattle.Models.Combat.Action;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing Save/Load encounter functionality and Path Animation
    /// </summary>
    public partial class BattleGridControl
    {
        #region Save/Load Encounter DTO Helpers

        /// <summary>
        /// Creates an EncounterDto for saving the current state
        /// </summary>
        public EncounterDto GetEncounterDto()
        {
            var dto = new EncounterDto();

            // Map image
            if (MapImage?.Source is BitmapImage bi && bi.UriSource != null)
                dto.MapImagePath = bi.UriSource.LocalPath;
            else
                dto.MapImagePath = null;

            // Tokens
            if (Tokens != null)
            {
                foreach (var t in Tokens)
                {
                    dto.Tokens.Add(new TokenDto
                    {
                        Id = t.Id.ToString(),
                        Name = t.Name,
                        GridX = t.GridX,
                        GridY = t.GridY,
                        MaxHP = t.MaxHP,
                        AC = t.ArmorClass,
                        Initiative = t.Initiative,
                        InitiativeMod = t.InitiativeModifier,
                        Speed = t.Speed,
                        IsPlayer = t.IsPlayer,
                        SizeInSquares = t.SizeInSquares,
                        IconPath = (t.Image is BitmapImage b && b.UriSource != null) ? b.UriSource.LocalPath : null
                    });
                }
            }

            // Walls
            foreach (var wall in _wallService.Walls)
            {
                dto.Walls.Add(new WallDto
                {
                    StartX = wall.StartPoint.X,
                    StartY = wall.StartPoint.Y,
                    EndX = wall.EndPoint.X,
                    EndY = wall.EndPoint.Y,
                    WallType = wall.WallType.ToString(),
                    IsOpen = wall.IsOpen,
                    Label = wall.Label
                });
            }

            // Lights
            dto.Lights = _lights.Select(l => new LightDto
            {
                X = l.CenterGrid.X,
                Y = l.CenterGrid.Y,
                RadiusSquares = l.RadiusSquares,
                Intensity = l.Intensity,
                BrightRadius = l.BrightRadius,
                DimRadius = l.DimRadius,
                ColorR = l.LightColor.R,
                ColorG = l.LightColor.G,
                ColorB = l.LightColor.B,
                IsEnabled = l.IsEnabled,
                LightType = l.Type.ToString(),
                Direction = l.Direction,
                ConeWidth = l.ConeWidth,
                Label = l.Label
            }).ToList();

            return dto;
        }

        /// <summary>
        /// Loads an encounter from a DTO
        /// </summary>
        public void LoadEncounterDto(EncounterDto dto)
        {
            // Map image
            if (!string.IsNullOrEmpty(dto.MapImagePath) && System.IO.File.Exists(dto.MapImagePath))
            {
                MapImage.Source = new BitmapImage(new Uri(dto.MapImagePath));
                MapImage.SetValue(Canvas.ZIndexProperty, -100);
            }

            // Tokens: clear and add
            Tokens?.Clear();
            foreach (var td in dto.Tokens)
            {
                var token = new Token
                {
                    Name = td.Name,
                    HP = td.MaxHP,
                    MaxHP = td.MaxHP,
                    ArmorClass = td.AC,
                    Initiative = td.Initiative,
                    InitiativeModifier = td.InitiativeMod,
                    Speed = td.Speed,
                    IsPlayer = td.IsPlayer,
                    GridX = td.GridX,
                    GridY = td.GridY,
                    SizeInSquares = td.SizeInSquares
                };

                if (!string.IsNullOrEmpty(td.IconPath) && System.IO.File.Exists(td.IconPath))
                {
                    token.Image = new BitmapImage(new Uri(td.IconPath));
                }
                Tokens?.Add(token);
            }

            // Walls: clear and add
            _wallService.Clear();
            if (dto.Walls != null)
            {
                foreach (var wd in dto.Walls)
                {
                    var wall = new Wall
                    {
                        StartPoint = new Point(wd.StartX, wd.StartY),
                        EndPoint = new Point(wd.EndX, wd.EndY),
                        IsOpen = wd.IsOpen,
                        Label = wd.Label
                    };

                    // Parse wall type
                    if (Enum.TryParse<WallType>(wd.WallType, out var wallType))
                    {
                        wall.WallType = wallType;
                    }

                    _wallService.AddWall(wall);
                }
            }

            // Lights: clear and add
            _lights.Clear();
            if (dto.Lights != null)
            {
                foreach (var ld in dto.Lights)
                {
                    var light = new LightSource
                    {
                        CenterGrid = new Point(ld.X, ld.Y),
                        BrightRadius = ld.BrightRadius,
                        DimRadius = ld.DimRadius,
                        Intensity = ld.Intensity,
                        LightColor = Color.FromRgb(ld.ColorR, ld.ColorG, ld.ColorB),
                        IsEnabled = ld.IsEnabled,
                        Direction = ld.Direction,
                        ConeWidth = ld.ConeWidth,
                        Label = ld.Label
                    };

                    if (Enum.TryParse<LightType>(ld.LightType, out var lt))
                        light.Type = lt;

                    _lights.Add(light);
                }
            }

            RebuildTokenVisuals();
            RedrawWalls();
            RedrawMovementOverlay();
            RedrawLighting();
        }

        #endregion

        #region Path Animation and AOO Resolution

        /// <summary>
        /// Commits the previewed path with animation
        /// </summary>
        public async Task CommitPreviewedPathAsync()
        {
            if (_lastPreviewPath == null || _lastPreviewPath.Count == 0 || SelectedToken == null) return;

            var tokenVis = RenderCanvas.Children.OfType<FrameworkElement>().FirstOrDefault(c => c.Tag is Token t && t.Id == SelectedToken.Id);
            if (tokenVis == null) return;
            if (_isDraggingToken) return;

            double secondsPerSquare = 1.0 / Math.Max(0.001, Options.PathSpeedSquaresPerSecond);

            int prevX = SelectedToken.GridX;
            int prevY = SelectedToken.GridY;

            for (int i = 0; i < _lastPreviewPath.Count; i++)
            {
                var step = _lastPreviewPath[i];
                double targetLeft = step.x * GridCellSize;
                double targetTop = step.y * GridCellSize;

                await AnimateTokenTo(tokenVis, targetLeft, targetTop, secondsPerSquare);

                // Resolve AOOs between prev cell and this step
                await ResolveAOOsForStep((prevX, prevY), (step.x, step.y), SelectedToken);

                // Update logical position after AOOs resolved
                SelectedToken.GridX = step.x;
                SelectedToken.GridY = step.y;

                prevX = step.x;
                prevY = step.y;
            }

            ClearPathVisual();
            RedrawMovementOverlay();
        }

        private Task AnimateTokenTo(FrameworkElement tokenVis, double targetLeft, double targetTop, double seconds)
        {
            var tcs = new TaskCompletionSource<object>();

            var leftAnim = new DoubleAnimation()
            {
                To = targetLeft,
                Duration = TimeSpan.FromSeconds(seconds),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            var topAnim = new DoubleAnimation()
            {
                To = targetTop,
                Duration = TimeSpan.FromSeconds(seconds),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            leftAnim.FillBehavior = FillBehavior.Stop;
            topAnim.FillBehavior = FillBehavior.Stop;

            int completedCount = 0;
            void OnCompleted(object s, EventArgs e)
            {
                completedCount++;
                if (completedCount >= 2)
                {
                    Canvas.SetLeft(tokenVis, targetLeft);
                    Canvas.SetTop(tokenVis, targetTop);
                    leftAnim.Completed -= OnCompleted;
                    topAnim.Completed -= OnCompleted;
                    tcs.SetResult(null);
                }
            }

            leftAnim.Completed += OnCompleted;
            topAnim.Completed += OnCompleted;

            tokenVis.BeginAnimation(Canvas.LeftProperty, leftAnim);
            tokenVis.BeginAnimation(Canvas.TopProperty, topAnim);

            return tcs.Task;
        }

        private async Task ResolveAOOsForStep((int x, int y) prevCell, (int x, int y) curCell, Token defender)
        {
            if (defender == null || Tokens == null) return;

            // Find enemies (opposite team) that were adjacent to prevCell and are NOT adjacent to curCell
            var enemies = Tokens.Where(t => t.Id != defender.Id && t.IsPlayer != defender.IsPlayer).ToList();
            var provokingEnemies = new List<Token>();

            foreach (var eToken in enemies)
            {
                bool prevAdj = AreCellsAdjacent(prevCell, (eToken.GridX, eToken.GridY));
                bool curAdj = AreCellsAdjacent(curCell, (eToken.GridX, eToken.GridY));
                if (prevAdj && !curAdj)
                    provokingEnemies.Add(eToken);
            }

            if (provokingEnemies.Count == 0) return;

            foreach (var attacker in provokingEnemies)
            {
                (string Name, int AttackBonus, string DamageExpression, string Range)? action = PickAttackAction(attacker);
                int attackBonus = action?.AttackBonus ?? attacker.InitiativeModifier;

                // roll d20
                var rollRes = DnDBattle.Utils.DiceRoller.RollExpression("d20");
                int d20 = rollRes.Individual.Count > 0 ? rollRes.Individual[0] : rollRes.Total;
                int attackTotal = d20 + attackBonus;

                bool isCritical = (d20 == 20);
                bool hit = isCritical || (attackTotal >= defender.ArmorClass);

                int damage = 0;
                string damageDetails = string.Empty;
                if (hit)
                {
                    string dmgExpr = action?.DamageExpression ?? "1d4";
                    var dmgRoll = DnDBattle.Utils.DiceRoller.RollExpression(dmgExpr);
                    damage = dmgRoll.Total;
                    if (isCritical)
                    {
                        var extra = DnDBattle.Utils.DiceRoller.RollExpression(dmgExpr);
                        damage += extra.Total;
                        damageDetails = $"{dmgRoll.Total} + {extra.Total} (critical)";
                    }
                    else damageDetails = $"{dmgRoll.Total}";
                }

                // Apply damage to defender (on UI thread)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    defender.HP = Math.Max(0, defender.HP - damage);
                });

                // Log result
                var msg = $"{attacker.Name} makes an opportunity attack on {defender.Name}: d20={d20} + {attackBonus} => {attackTotal}. ";
                msg += hit ? $"Hit for {damage} ({damageDetails}). {defender.Name} HP now {defender.HP}." : "Missed.";
                AddToActionLog("AOO", msg);

                // small delay to stagger logs/animation feel
                await Task.Delay(250);
                // If defender dies, break
                if (defender.HP <= 0)
                {
                    AddToActionLog("AOO", $"{defender.Name} has been reduced to 0 HP.");
                    break;
                }
            }
        }

        private bool AreCellsAdjacent((int x, int y) a, (int x, int y) b)
        {
            int dx = Math.Abs(a.x - b.x), dy = Math.Abs(a.y - b.y);
            return (dx + dy) == 1;
        }

        private (string name, int AttackBonus, string DamageExpression, string Range) PickAttackAction(Token attacker)
        {
            try
            {
                var prop = attacker.GetType().GetProperty("Action");
                if (prop != null)
                {
                    var actions = prop.GetValue(attacker) as System.Collections.IEnumerable;
                    if (actions != null)
                    {
                        foreach (var a in actions)
                        {
                            var rangeProp = a.GetType().GetProperty("Range");
                            var nameProp = a.GetType().GetProperty("Name");
                            var atkProp = a.GetType().GetProperty("AttackBonus");
                            var dmgProp = a.GetType().GetProperty("Damage");
                            var range = rangeProp?.GetValue(a)?.ToString() ?? "";
                            if (range.ToLower().Contains("melee") || string.IsNullOrWhiteSpace(range))
                            {
                                var nm = nameProp?.GetValue(a)?.ToString() ?? "Attack";
                                int atk = 0;
                                int.TryParse(atkProp?.GetValue(a)?.ToString() ?? "0", out atk);
                                var dmg = dmgProp?.GetValue(a)?.ToString() ?? "1d4";
                                return (nm, atk, dmg, range);
                            }
                        }

                    }
                }
            }
            catch { }
            return ("Melee Attack", attacker.InitiativeModifier, "1d4", "Melee");
        }

        #endregion
    }
}
