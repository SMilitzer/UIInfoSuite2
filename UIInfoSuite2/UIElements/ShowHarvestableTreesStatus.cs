using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.TerrainFeatures;
using UIInfoSuite.Infrastructure;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

// ReSharper disable once CheckNamespace
namespace UIInfoSuite.UIElements
{
    class ShowHarvestableTreesStatus : IDisposable
    {
        #region Properties
        private readonly PerScreen<GameLocation[]> _treeLocations = new PerScreen<GameLocation[]>();
        private readonly PerScreen<Rectangle> _treeTexturePosition = new PerScreen<Rectangle>();
        private readonly PerScreen<string> _treeHoverText = new PerScreen<string>();
        private readonly PerScreen<ClickableTextureComponent> _treeIcon = new PerScreen<ClickableTextureComponent>();

        private readonly IModHelper _helper;
        #endregion


        #region Life cycle
        public ShowHarvestableTreesStatus(IModHelper helper)
        {
            _helper = helper;

            _treeTexturePosition.Value = new Rectangle {X = 208, Y = 400, Width = 16, Height = 16};
        }

        public void Dispose()
        {
            ToggleOption(false);
            _treeLocations.Value = null;
        }

        public void ToggleOption(bool showToolUpgradeStatus)
        {
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.GameLoop.DayStarted -= OnDayStarted;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showToolUpgradeStatus)
            {
                UpdateTreeInfo(true);

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

            if (_treeLocations.Value.Any())
                UpdateTreeInfo(false);
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            UpdateTreeInfo(true);
        }

        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (Game1.eventUp) return;

            // Draw tree icon
            if (_treeLocations.Value.Any())
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                _treeIcon.Value =
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.objectSpriteSheet,
                        _treeTexturePosition.Value,
                        2.5f);
                _treeIcon.Value.draw(Game1.spriteBatch);
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Show text on hover
            if (_treeLocations.Value.Any() && (_treeIcon.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
            {
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    _treeHoverText.Value,
                    Game1.dialogueFont
                );

                if (_helper.Input.IsDown(SButton.MouseRight))
                {
                    _treeLocations.Value = new GameLocation[0];
                }
            }
        }
        #endregion

        private static string GetTreeName(int cropId)
        {
            return Game1.objectInformation[cropId].Split('/').First();
        }

        private static bool HasHarvestableTrees(GameLocation location)
        {
            return location.terrainFeatures.Values
                .Where(x => x is FruitTree)
                .Cast<FruitTree>()
                .Any(x => x.fruitsOnTree.Value == FruitTree.maxFruitsOnTrees);
        }

        private static Dictionary<string, int> GetHarvestableTrees(GameLocation location)
        {
            return location.terrainFeatures.Values
                .Where(x => x is FruitTree)
                .Cast<FruitTree>()
                .Where(x => x.fruitsOnTree.Value == FruitTree.maxFruitsOnTrees)
                .GroupBy(x => x.indexOfFruit.Value)
                .Select(g => new {Id = g.Key, Count = g.Count()})
                .ToDictionary(x => GetTreeName(x.Id), x => x.Count);
        }
        private static string GetLocationTreeDisplayMessage(GameLocation location)
        {
            var info = GetHarvestableTrees(location);

            var message = info.Aggregate(location.Name, (s, pair) => $"{s}\n  - {pair.Key}: {pair.Value}");

            return message;
        }

        #region Logic
        private void UpdateTreeInfo(bool reset)
        {
            if (reset)
            {
                _treeLocations.Value = Game1.locations.Where(x => x.IsFarm || x.IsGreenhouse || x.Name.StartsWith("Island")).ToArray();
            }

            var locations = _treeLocations.Value.Where(HasHarvestableTrees).ToArray();
            
            _treeLocations.Value = locations;

            if (locations.Any())
            {
                var locationsInfo = locations.Aggregate("", (s, location) => s == string.Empty ? GetLocationTreeDisplayMessage(location) : $"{s}\n{GetLocationTreeDisplayMessage(location)}");
                _treeHoverText.Value = locationsInfo;
            }
            else
            {
                _treeHoverText.Value = string.Empty;
            }
        }
        #endregion
    }
}
