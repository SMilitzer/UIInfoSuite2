using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using System;
using System.Linq;
using UIInfoSuite.Infrastructure;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

// ReSharper disable once CheckNamespace
namespace UIInfoSuite.UIElements
{
    class ShowPondsWithNeedsStatus : IDisposable
    {
        #region Properties
        private readonly PerScreen<Rectangle> _fishTexturePosition = new PerScreen<Rectangle>();
        private readonly PerScreen<string> _fishHoverText = new PerScreen<string>();
        private readonly PerScreen<FishPond[]> _fishPonds = new PerScreen<FishPond[]>();
        private readonly PerScreen<FishPond[]> _fishPondsWithNeeds = new PerScreen<FishPond[]>();
        private readonly PerScreen<ClickableTextureComponent> _fishIcon = new PerScreen<ClickableTextureComponent>();

        private readonly IModHelper _helper;
        #endregion

        #region Life cycle
        public ShowPondsWithNeedsStatus(IModHelper helper)
        {
            _helper = helper;

            _fishTexturePosition.Value = new Rectangle {X = 256, Y = 80, Width = 16, Height = 16};
            _fishPonds.Value = new FishPond[0];
            _fishPondsWithNeeds.Value = new FishPond[0];
        }

        public void Dispose()
        {
            ToggleOption(false);
            _fishPonds.Value = null;
            _fishPondsWithNeeds.Value = null;
        }

        public void ToggleOption(bool showToolUpgradeStatus)
        {
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.GameLoop.DayStarted -= OnDayStarted;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showToolUpgradeStatus)
            {
                UpdatePondsInfo(true);

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

            if (_fishPonds.Value.Any())
                UpdatePondsInfo(false);
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            UpdatePondsInfo(true);
        }

        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (Game1.eventUp) return;

            // Draw crops icon
            if (_fishPondsWithNeeds.Value.Any())
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                _fishIcon.Value =
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.objectSpriteSheet,
                        _fishTexturePosition.Value,
                        2.5f);
                _fishIcon.Value.draw(Game1.spriteBatch);
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Show text on hover
            if (_fishPondsWithNeeds.Value.Any() && (_fishIcon.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
            {
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    _fishHoverText.Value,
                    Game1.dialogueFont
                );

                if (_helper.Input.IsDown(SButton.MouseRight))
                {
                    _fishPonds.Value = new FishPond[0];
                    _fishPondsWithNeeds.Value = new FishPond[0];
                }
            }
        }
        #endregion

        private static string GetFishName(int fishId)
        {
            return Game1.objectInformation[fishId].Split('/').First();
        }

        private static string GetFishPondDisplayMessage(FishPond fishpond)
        {
            var fishId = fishpond.fishType.Value;
            var neededItemCount = fishpond.neededItemCount.Value;
            var itemWanted = fishpond.neededItem.Value.displayName;

            var fishName = GetFishName(fishId);

            var message = $"{fishName}: {neededItemCount} {itemWanted}";

            return message;
        }

        #region Logic
        private void UpdatePondsInfo(bool reset)
        {
            if (reset)
            {
                _fishPonds.Value = Game1.locations
                    .Where(x => x is BuildableGameLocation)
                    .Cast<BuildableGameLocation>()
                    .Where(x => x.buildings.Any())
                    .SelectMany(x => x.buildings)
                    .Where(x => x is FishPond)
                    .Cast<FishPond>()
                    .ToArray();
            }

            var fishPondsWithNeeds = _fishPonds.Value.Where(x => x.HasUnresolvedNeeds()).ToArray();

            _fishPondsWithNeeds.Value = fishPondsWithNeeds;

            if (fishPondsWithNeeds.Any())
            {
                var fishInfo = fishPondsWithNeeds.Aggregate("", (s, fishpond) => s == string.Empty ? GetFishPondDisplayMessage(fishpond) : $"{s}\n{GetFishPondDisplayMessage(fishpond)}");
                _fishHoverText.Value = fishInfo;
            }
            else
            {
                _fishHoverText.Value = string.Empty;
            }
        }
        #endregion
    }
}
