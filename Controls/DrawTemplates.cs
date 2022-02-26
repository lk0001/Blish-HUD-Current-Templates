using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace lk0001.CurrentTemplates.Controls
{
    class DrawTemplates: Container
    {
        public ContentService.FontSize Font_Size = ContentService.FontSize.Size16;
        public HorizontalAlignment Align = HorizontalAlignment.Left;
        public bool Drag = false;
        public bool BuildPad = false;
        public string BuildPadConfigPath = "";

        public string buildName = "";
        public string equipmentName = "";

        private static BitmapFont _font;
        private Point _dragStart = Point.Zero;
        private bool _dragging;

        private LoadingSpinner spinner;

        public DrawTemplates()
        {
            this.Location = new Point(0, 0);
            this.Size = new Point(0, 0);
            this.Visible = true;
            this.Padding = Thickness.Zero;
            spinner = new LoadingSpinner()
            {
                Location = new Point(1, 1),
                Visible = true,
                Parent = this,
            };
            AddChild(spinner);
        }

        protected override CaptureType CapturesInput()
        {
            if (this.Drag)
                return CaptureType.Mouse;
            else
                return CaptureType.Filter;
        }
        protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
        {
            if (Drag)
            {
                _dragging = true;
                _dragStart = Input.Mouse.Position;
            }
            base.OnLeftMouseButtonPressed(e);
        }
        protected override void OnLeftMouseButtonReleased(MouseEventArgs e)
        {
            if (Drag)
            {
                _dragging = false;
                Module._settingLoc.Value = this.Location;
            }
            base.OnLeftMouseButtonPressed(e);
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            if (_dragging)
            {
                var nOffset = Input.Mouse.Position - _dragStart;
                Location += nOffset;

                _dragStart = Input.Mouse.Position;
            }
        }

        public void ShowSpinner()
        {
            spinner.Show();
        }

        public void HideSpinner()
        {
            spinner.Hide();
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            string templates = "";
            _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, Font_Size, ContentService.FontStyle.Regular);

            templates += " " + buildName + " \n";
            templates += " " + equipmentName + " \n";

            int templatesWidth = (int)_font.MeasureString(templates).Width;
            int spinnerSize = (int)_font.MeasureString("C").Height * 2;
            this.Size = new Point(
                templatesWidth + spinnerSize,
                (int)_font.MeasureString(templates).Height
            );
            spinner.Location = new Point(templatesWidth, 1);
            spinner.Size = new Point(spinnerSize, spinnerSize);

            spriteBatch.DrawStringOnCtrl(this,
                templates,
                _font,
                new Rectangle(0, 0, this.Size.X, this.Size.Y),
                Color.White,
                false,
                true,
                1,
                Align,
                VerticalAlignment.Top
            );
        }
    }
}
