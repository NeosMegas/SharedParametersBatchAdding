#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SharedParametersBatchAdding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;

#endregion

namespace SharedParametersBatchAdding
{
    internal class SharedParametersBatchAddingApp : IExternalApplication
    {
        static AddInId addInId = new AddInId(new Guid("977c5e90-a349-4374-8839-cd5dbaeba952"));
        string helpURL = "https://docs.google.com/document/d/1834huChprxAskfX0RSqEdzMkROZDYWkfjdKOVC0jBF0";
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        public RibbonPanel AutomationPanel(UIControlledApplication application, string tabName = "", RibbonPanel ribbonPanel = null)
        {
            if (ribbonPanel == null)
            {
                if (string.IsNullOrEmpty(tabName))
                    ribbonPanel = application.CreateRibbonPanel("Кулёк параметров");
                else
                    ribbonPanel = application.CreateRibbonPanel(tabName, "Кулёк параметров");
            }
            AddButton(ribbonPanel,
                "Кулёк\nпараметров",
                assemblyPath,
                nameof(SharedParametersBatchAdding) + "." + nameof(SharedParametersBatchAddingCommand),
                "Пакетное добавление общих параметров в семейства",
                "SharedParametersBatchAddingCommand_large.png",
                "SharedParametersBatchAddingCommand_small.png");
            return ribbonPanel;
        }

        private void AddButton(RibbonPanel ribbonPanel, string buttonName, string path, string linkToCommand, string toolTip, string largeIconPath = "", string smallIconPath = "")
        {
            PushButtonData buttonData = new PushButtonData(buttonName,buttonName, path, linkToCommand);
            ContextualHelp contextualHelp = new ContextualHelp(ContextualHelpType.Url, helpURL);
            buttonData.SetContextualHelp(contextualHelp);
            PushButton button = ribbonPanel.AddItem(buttonData) as PushButton;
            button.ToolTip = toolTip;
            //button.AvailabilityClassName = typeof(TrueAvailability).Namespace + "." + nameof(TrueAvailability);
            if (string.IsNullOrEmpty(largeIconPath))
                largeIconPath = "Icon32.png";
            if (string.IsNullOrEmpty(smallIconPath))
                largeIconPath = "Icon16.png";
            button.LargeImage = new BitmapImage(new Uri($@"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/" + largeIconPath, UriKind.RelativeOrAbsolute));
            button.Image = new BitmapImage(new Uri($@"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/" + smallIconPath, UriKind.RelativeOrAbsolute));
        }

        public Result OnStartup(UIControlledApplication a)
        {
            RibbonPanel ribbonPanel = AutomationPanel(a);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }

    //public class TrueAvailability : IExternalCommandAvailability
    //{
    //    public bool IsCommandAvailable(UIApplication a, CategorySet b) => true;
    //}
}
