﻿using DGP.Genshin.DataViewer.Controls.Dialogs;
using DGP.Genshin.DataViewer.Helpers;
using DGP.Genshin.DataViewer.Services;
using DGP.Snap.Framework.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DGP.Genshin.DataViewer.Views
{
    /// <summary>
    /// DirectorySelectView.xaml 的交互逻辑
    /// </summary>
    public partial class DirectorySelectView : UserControl
    {
        public DirectorySelectView()
        {
            InitializeComponent();
            Container.GoToElementState("PickingFolder", true);
        }

        public ExcelSplitView ExcelSplitView {get; set; }

        private void OpenFolderRequested(object sender, RoutedEventArgs e)
        {
            WorkingFolderService.SelectWorkingFolder();
            string path = WorkingFolderService.WorkingFolderPath;
            if (path == null)
                return;
            if (!Directory.Exists(path + @"\TextMap\") || !Directory.Exists(path + @"\Excel\"))
                new SelectionSuggestDialog().ShowAsync();
            else
            {
                ExcelSplitView.TextMapCollection = DirectoryEx.GetFileExs(path + @"\TextMap\");
                ExcelSplitView.ExcelConfigDataCollection = DirectoryEx.GetFileExs(path + @"\Excel\");
                if (ExcelSplitView.ExcelConfigDataCollection.Count() == 0)
                    new SelectionSuggestDialog().ShowAsync();
                else
                {
                    Container.GoToElementState("SelectingMap", true);
                    //npcid
                    MapService.NPCMap = new Lazy<Dictionary<string, string>>(() =>
                    Json.ToObject<JArray>(
                        ExcelSplitView.ExcelConfigDataCollection
                        .First(f => f.FileName == "Npc").Read())
                    .ToDictionary(t => t["Id"].ToString(), v => v["NameTextMapHash"].ToString()));
                }
            }
        }

        private void OnMapSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Container.GoToElementState("Confirming", true);
        }

        private void OnConfirmed(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = ExcelSplitView;
        }
    }
}
