using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Blish_HUD.Settings.UI.Views;
using Blish_HUD.Graphics.UI;

namespace lk0001.CurrentTemplates.Views
{
    public class SettingsView : View
    {
        private Label settingBuildPadPath_Warning;

        protected override void Build(Container buildPanel)
        {
            Panel parentPanel = new Panel()
            {
                CanScroll = false,
                Parent = buildPanel,
                Height = buildPanel.Height,
                HeightSizingMode = SizingMode.AutoSize,
                Width = 700,
            };

            Label settingFontSize_Label = new Label()
            {
                Location = new Point(10, 10),
                Width = 75,
                AutoSizeHeight = false,
                WrapText = false,
                Parent = parentPanel,
                Text = "Font Size: ",
            };
            Dropdown settingFontSize_Select = new Dropdown()
            {
                Location = new Point(settingFontSize_Label.Right + 8, settingFontSize_Label.Top - 4),
                Width = 50,
                Parent = parentPanel,
            };
            foreach (var s in Module._fontSizes)
            {
                settingFontSize_Select.Items.Add(s);
            }
            settingFontSize_Select.SelectedItem = Module._settingFontSize.Value;
            settingFontSize_Select.ValueChanged += delegate {
                Module._settingFontSize.Value = settingFontSize_Select.SelectedItem;
            };

            Label settingAlign_Label = new Label()
            {
                Location = new Point(10, settingFontSize_Label.Bottom + 10),
                Width = 75,
                AutoSizeHeight = false,
                WrapText = false,
                Parent = parentPanel,
                Text = "Align: ",
            };
            Dropdown settingAlign_Select = new Dropdown()
            {
                Location = new Point(settingAlign_Label.Right + 8, settingAlign_Label.Top - 4),
                Width = 75,
                Parent = parentPanel,
            };
            foreach (var s in Module._fontAlign)
            {
                settingAlign_Select.Items.Add(s);
            }
            settingAlign_Select.SelectedItem = Module._settingAlign.Value;
            settingAlign_Select.ValueChanged += delegate {
                Module._settingAlign.Value = settingAlign_Select.SelectedItem;
            };

            IView settingDrag_View = SettingView.FromType(Module._settingDrag, buildPanel.Width);
            ViewContainer settingDrag_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingAlign_Label.Bottom + 6),
                Parent = parentPanel
            };
            settingDrag_Container.Show(settingDrag_View);

            IView settingBuildPad_View = SettingView.FromType(Module._settingBuildPad, buildPanel.Width);
            ViewContainer settingBuildPad_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingDrag_Container.Bottom + 6),
                Parent = parentPanel
            };
            settingBuildPad_Container.Show(settingBuildPad_View);

            IView settingBuildPadPath_View = SettingView.FromType(Module._settingBuildPadPath, buildPanel.Width);
            ViewContainer settingBuildPadPath_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingBuildPad_Container.Bottom + 6),
                Parent = parentPanel
            };
            settingBuildPadPath_Container.Show(settingBuildPadPath_View);

            settingBuildPadPath_Warning = new Label()
            {
                Location = new Point(10, settingBuildPadPath_Container.Bottom + 10),
                Width = 100,
                AutoSizeHeight = false,
                WrapText = false,
                Parent = parentPanel,
                Text = "Incorrect path",
                TextColor = Color.Red,
            };

            ToggleIncorrectPathWarning();
        }

        public void ToggleIncorrectPathWarning()
        {
            if (Module._hasBuildPad)
            {
                settingBuildPadPath_Warning?.Hide();
            }
            else
            {
                settingBuildPadPath_Warning?.Show();
            }
        }
    }
}
