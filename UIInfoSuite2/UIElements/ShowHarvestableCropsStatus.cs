using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using UIInfoSuite.Infrastructure;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

// ReSharper disable once CheckNamespace
namespace UIInfoSuite.UIElements
{
    class ShowHarvestableCropsStatus : IDisposable
    {
        #region Properties
        private readonly PerScreen<Rectangle> _cropTexturePosition = new PerScreen<Rectangle>();
        private readonly PerScreen<string> _cropHoverText = new PerScreen<string>();
        private readonly PerScreen<GameLocation[]> _cropLocations = new PerScreen<GameLocation[]>();
        private readonly PerScreen<ClickableTextureComponent> _cropIcon = new PerScreen<ClickableTextureComponent>();

        private readonly IModHelper _helper;
        #endregion


        #region Life cycle
        public ShowHarvestableCropsStatus(IModHelper helper)
        {
            _helper = helper;

            _cropTexturePosition.Value = new Rectangle {X = 224, Y = 160, Width = 16, Height = 16};
        }

        public void Dispose()
        {
            ToggleOption(false);
            _cropLocations.Value = null;
        }

        public void ToggleOption(bool showToolUpgradeStatus)
        {
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.GameLoop.DayStarted -= OnDayStarted;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showToolUpgradeStatus)
            {
                UpdateCropInfo(true);
                _helper.Events.Display.RenderingHud += OnRenderingHud;
                _helper.Events.Display.RenderedHud += OnRenderedHud;
                _helper.Events.GameLoop.DayStarted += OnDayStarted;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }
        #endregion


        #region Event subscriptions
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(600)) return;

            if (_cropLocations.Value.Any())
                UpdateCropInfo(false);
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            UpdateCropInfo(true);
        }

        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (Game1.eventUp) return;

            // Draw crops icon
            if (_cropLocations.Value.Any())
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                _cropIcon.Value =
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.objectSpriteSheet,
                        _cropTexturePosition.Value,
                        2.5f);
                _cropIcon.Value.draw(Game1.spriteBatch);
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Show text on hover
            if (_cropLocations.Value.Any() && (_cropIcon.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
            {
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    _cropHoverText.Value,
                    Game1.dialogueFont
                );

                if (_helper.Input.IsDown(SButton.MouseRight))
                {
                    _cropLocations.Value = new GameLocation[0];
                }
            }
        }
        #endregion

        private static string GetCropName(int cropId)
        {
            return Game1.objectInformation[cropId].Split('/').First();
        }

        private static Dictionary<string, int> GetHarvestableCrops(GameLocation location)
        {
            return location.terrainFeatures.Values
                .Where(x => x is HoeDirt)
                .Cast<HoeDirt>()
                .Where(x => x.readyForHarvest())
                .GroupBy(x => x.crop.indexOfHarvest.Value)
                .Select(g => new {Id = g.Key, Count = g.Count()})
                .ToDictionary(x => GetCropName(x.Id), x => x.Count);
        }

        private static bool HasHarvestableCrops(GameLocation location)
        {
            return location.terrainFeatures.Values
                .Where(x => x is HoeDirt)
                .Cast<HoeDirt>()
                .Any(x => x.readyForHarvest());
        }

        private static string GetLocationCropDisplayMessage(GameLocation location)
        {
            var info = GetHarvestableCrops(location);

            var message = info.Aggregate(location.Name, (s, pair) => $"{s}\n  - {pair.Key}: {pair.Value}");

            return message;
        }

        #region Logic
        private void UpdateCropInfo(bool reset)
        {
            if (reset)
            {
                _cropLocations.Value = Game1.locations.Where(x => x.IsFarm || x.IsGreenhouse || x.Name.StartsWith("Island")).ToArray();

                //Game1.locations
                //    .Where(x => x is BuildableGameLocation)
                //    .Cast<BuildableGameLocation>()
                //    .Where(x => x.buildings.Any())
                //    .SelectMany(x => x.buildings)
                //    .Where(x => x is FishPond)
                //    .Cast<FishPond>()
                //    .Where(x => x.HasUnresolvedNeeds())
                //    .Select(x => new
                //    {
                //        Fish = GetCropName(x.fishType.Value), NeededItem = x.ItemWanted.DisplayName,
                //        ItemCount = x.neededItemCount.Value
                //    });

            }

            var locations = _cropLocations.Value.Where(HasHarvestableCrops).ToArray();
            
            _cropLocations.Value = locations;

            if (locations.Any())
            {
                var locationsInfo = locations.Aggregate("", (s, location) => s == string.Empty ? GetLocationCropDisplayMessage(location) : $"{s}\n{GetLocationCropDisplayMessage(location)}");
                _cropHoverText.Value = locationsInfo;
            }
            else
            {
                _cropHoverText.Value = string.Empty;
            }
        }
        #endregion
    }
}
