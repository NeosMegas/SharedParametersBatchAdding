using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SharedParametersBatchAdding
{
    public partial class SharedParametersBatchAddingWPF : Window
    {
        DefinitionGroups SharedParametersGroups;
        public string AddParametersSelectedOption;
        public string FilePath;
        ObservableCollection<KeyValuePair<DefinitionGroup, ObservableCollection<ExternalDefinition>>> DefinitionGroupxternalDefinitionKVPCollection;
        ObservableCollection<KeyValuePair<string, BuiltInParameterGroup>> BuiltInParameterGroupKeyValuePairs;
        public ObservableCollection<SharedParametersBatchAddingItem> SharedParametersBatchAddingItemsList;

        public SharedParametersBatchAddingWPF(DefinitionGroups sharedParametersGroups, ObservableCollection<KeyValuePair<string, BuiltInParameterGroup>> builtInParameterGroupKeyValuePairs)
        {
            InitializeComponent();
            SharedParametersGroups = sharedParametersGroups;
            //Создание коллекции для вывода в комбобокс выбора группирования
            BuiltInParameterGroupKeyValuePairs = builtInParameterGroupKeyValuePairs;
            comboBox_GroupingParameters.ItemsSource = BuiltInParameterGroupKeyValuePairs;
            if (BuiltInParameterGroupKeyValuePairs.Where(kVP => kVP.Key.Equals("Прочее")).Count() != 0)
            {
                comboBox_GroupingParameters.SelectedItem = comboBox_GroupingParameters.Items
                    .GetItemAt(BuiltInParameterGroupKeyValuePairs
                    .IndexOf(BuiltInParameterGroupKeyValuePairs.FirstOrDefault(kVP => kVP.Key == "Прочее")));
            }
            comboBox_GroupingParameters.DisplayMemberPath = "Key";

            //Коллекция групп и общих параметро
            DefinitionGroupxternalDefinitionKVPCollection = new ObservableCollection<KeyValuePair<DefinitionGroup, ObservableCollection<ExternalDefinition>>>();
            foreach (DefinitionGroup definitionGroup in SharedParametersGroups)
            {
                List<ExternalDefinition> tmpExternalDefinitionList = new List<ExternalDefinition>();
                foreach (Definition definition in definitionGroup.Definitions)
                {
                    tmpExternalDefinitionList.Add(definition as ExternalDefinition);
                }
                ObservableCollection<ExternalDefinition> externalDefinitionCollecrion
                    = new ObservableCollection<ExternalDefinition>(tmpExternalDefinitionList
                    .OrderBy(ed => ed.Name, new AlphanumComparatorFastString()));

                KeyValuePair<DefinitionGroup, ObservableCollection<ExternalDefinition>> tmpKeyValuePair
                    = new KeyValuePair<DefinitionGroup, ObservableCollection<ExternalDefinition>>(definitionGroup, externalDefinitionCollecrion);

                DefinitionGroupxternalDefinitionKVPCollection.Add(tmpKeyValuePair);
            }

            //Группа параметров из ФОП
            listBox_SharedParametersGroups.ItemsSource = DefinitionGroupxternalDefinitionKVPCollection;
            listBox_SharedParametersGroups.DisplayMemberPath = "Key.Name";
            listBox_SharedParametersGroups.SelectedItem = listBox_SharedParametersGroups.Items.GetItemAt(0);

            //Создание списка выбранных параметров
            SharedParametersBatchAddingItemsList = new ObservableCollection<SharedParametersBatchAddingItem>();
            dataGrid_SelectedParametersGroup.ItemsSource = SharedParametersBatchAddingItemsList;
            dataGridComboBoxColumnGroup.ItemsSource = BuiltInParameterGroupKeyValuePairs;
            dataGridComboBoxColumnGroup.DisplayMemberPath = "Key";
            dataGridComboBoxColumnGroup.SelectedValuePath = "Value";
        }

        //Показ списка параметров при выборе группы параметров
        private void SharedParametersGroupOnSelected(object sender, SelectionChangedEventArgs args)
        {
            KeyValuePair<DefinitionGroup, ObservableCollection<ExternalDefinition>> selectedKeyValuePair = (KeyValuePair<DefinitionGroup, ObservableCollection<ExternalDefinition>>)(sender as ListBox).SelectedItem;
            DefinitionGroup definitionGroup = selectedKeyValuePair.Key;
            ObservableCollection<ExternalDefinition> sharedParametersSelectedDefinitions = DefinitionGroupxternalDefinitionKVPCollection
                .FirstOrDefault(g => g.Key == definitionGroup).Value;
            listBox_SharedParameters.ItemsSource = sharedParametersSelectedDefinitions;
            listBox_SharedParameters.DisplayMemberPath = "Name";
        }

        //Изменение принципа здания параметров (семейство, семейства, папка)
        private void radioButton_AddParametersGroupChecked(object sender, RoutedEventArgs e)
        {
            string ActionSelectionButtonName = (this.groupBox_AddParameters.Content as System.Windows.Controls.Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;
            if (ActionSelectionButtonName == "radioButton_ActiveFamily")
            {
                btn_FolderBrowserDialog.IsEnabled = false;
                textBox_FamiliesFolderPath.IsEnabled = false;
            }
            else if (ActionSelectionButtonName == "radioButton_AllOpenFamilies")
            {
                btn_FolderBrowserDialog.IsEnabled = false;
                textBox_FamiliesFolderPath.IsEnabled = false;
            }
            else if (ActionSelectionButtonName == "radioButton_FamiliesInSelectedFolder")
            {
                btn_FolderBrowserDialog.IsEnabled = true;
                textBox_FamiliesFolderPath.IsEnabled = true;
            }
        }

        //Получение папки с файлами семейств
        private void btn_FolderBrowserDialog_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if(FilePath != null)
            {
                if (Directory.Exists(FilePath))
                {
                    dialog.SelectedPath = FilePath;
                }
            }
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FilePath = dialog.SelectedPath;
                textBox_FamiliesFolderPath.Text = FilePath;
            }
        }

        private void textBox_FamiliesFolderPath_TextChanged(object sender, TextChangedEventArgs e)
        {
             FilePath = textBox_FamiliesFolderPath.Text;
        }

        //Добавление выбранного параметра
        private void btn_AddParameters_Click(object sender, RoutedEventArgs e)
        {
            List<ExternalDefinition> selectedExternalDefinitionList = listBox_SharedParameters.SelectedItems.Cast<ExternalDefinition>().ToList();
            foreach (ExternalDefinition selectedExternalDefinition in selectedExternalDefinitionList)
            {
                KeyValuePair<DefinitionGroup, ObservableCollection<ExternalDefinition>> sourseKeyValuePair 
                    = DefinitionGroupxternalDefinitionKVPCollection
                    .FirstOrDefault(g => g.Key.Name == selectedExternalDefinition.OwnerGroup.Name);
                sourseKeyValuePair.Value.Remove(selectedExternalDefinition);
            }

            foreach (ExternalDefinition selectedExternalDefinition in selectedExternalDefinitionList)
            {
                //Создание элемента выбора и заполнение свойств
                SharedParametersBatchAddingItem sharedParametersBatchAddingItem = new SharedParametersBatchAddingItem();
                sharedParametersBatchAddingItem.ExternalDefinitionParam = selectedExternalDefinition;
                sharedParametersBatchAddingItem.ExternalDefinitionParamGuid = selectedExternalDefinition.GUID;
                
                string radioButtonParameterIn = (groupBox_ParameterIn.Content as System.Windows.Controls.Grid)
                    .Children.OfType<RadioButton>().FirstOrDefault(rb => rb.IsChecked.Value == true).Name;
                if (radioButtonParameterIn == "radioButton_InstanceParameter")
                {
                    sharedParametersBatchAddingItem.AddParameterSelectedOptionParam = true;
                }
                else
                {
                    sharedParametersBatchAddingItem.AddParameterSelectedOptionParam = false;
                }
                sharedParametersBatchAddingItem.BuiltInParameterGroupParam = (KeyValuePair<string, BuiltInParameterGroup>)comboBox_GroupingParameters.SelectedValue;
                sharedParametersBatchAddingItem.FormulaParam = null;

                //Добавление элемента выбора в список выбранных параметров
                List<SharedParametersBatchAddingItem> sharedParametersItemsTmp = SharedParametersBatchAddingItemsList.Where(p => p.ExternalDefinitionParam
                == sharedParametersBatchAddingItem.ExternalDefinitionParam).ToList();
                if (sharedParametersItemsTmp.Count == 0)
                {
                    SharedParametersBatchAddingItemsList.Add(sharedParametersBatchAddingItem);
                }
            }
        }

        //Удаление выбранного параметра
        private void btn_DeleteParameters_Click(object sender, RoutedEventArgs e)
        {
            if (SharedParametersBatchAddingItemsList.Count != 0)
            {
                List<SharedParametersBatchAddingItem> selectedSharedParametersBatchAddingItemList 
                    = dataGrid_SelectedParametersGroup.SelectedItems.Cast<SharedParametersBatchAddingItem>().ToList();
                foreach(SharedParametersBatchAddingItem selectedSharedParametersBatchAddingItem in selectedSharedParametersBatchAddingItemList)
                {
                    SharedParametersBatchAddingItemsList.Remove(selectedSharedParametersBatchAddingItem);
                }
                foreach (SharedParametersBatchAddingItem selectedSharedParametersBatchAddingItem in selectedSharedParametersBatchAddingItemList)
                {
                    DefinitionGroupxternalDefinitionKVPCollection.FirstOrDefault(g => g.Key.Name == selectedSharedParametersBatchAddingItem
                    .ExternalDefinitionParam.OwnerGroup.Name).Value.Add(selectedSharedParametersBatchAddingItem.ExternalDefinitionParam);
                    
                    List<ExternalDefinition> tmpExternalDefinitionListForSorting = DefinitionGroupxternalDefinitionKVPCollection
                        .FirstOrDefault(g => g.Key.Name == selectedSharedParametersBatchAddingItem.ExternalDefinitionParam.OwnerGroup.Name).Value.Cast<ExternalDefinition>().ToList();
                    tmpExternalDefinitionListForSorting = tmpExternalDefinitionListForSorting.OrderBy(ed => ed.Name).ToList();
                    
                    DefinitionGroupxternalDefinitionKVPCollection.FirstOrDefault(g => g.Key.Name == selectedSharedParametersBatchAddingItem.ExternalDefinitionParam.OwnerGroup.Name).Value.Clear();
                    foreach(ExternalDefinition sortedExternalDefinition in tmpExternalDefinitionListForSorting)
                    {
                        DefinitionGroupxternalDefinitionKVPCollection.FirstOrDefault(g => g.Key.Name == selectedSharedParametersBatchAddingItem.ExternalDefinitionParam.OwnerGroup.Name)
                            .Value.Add(sortedExternalDefinition);
                    }
                }
            }
        }

        //Открыть 
        private void btn_Open_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.Filter = "json files (*.json)|*.json";
            System.Windows.Forms.DialogResult result = openDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                List<SharedParametersBatchAddingItem> sharedParametersBatchAddingItemList = dataGrid_SelectedParametersGroup.Items.Cast<SharedParametersBatchAddingItem>().ToList();
                if (sharedParametersBatchAddingItemList.Count != 0)
                {
                    
                    foreach (SharedParametersBatchAddingItem itemInDataGread in sharedParametersBatchAddingItemList)
                    {
                        SharedParametersBatchAddingItemsList.Remove(itemInDataGread);
                    }
                    foreach (SharedParametersBatchAddingItem itemInDataGread in sharedParametersBatchAddingItemList)
                    {
                        DefinitionGroupxternalDefinitionKVPCollection.FirstOrDefault(g => g.Key.Name == itemInDataGread.ExternalDefinitionParam.OwnerGroup.Name)
                            .Value.Add(itemInDataGread.ExternalDefinitionParam);

                        List<ExternalDefinition> tmpExternalDefinitionListForSorting = DefinitionGroupxternalDefinitionKVPCollection
                            .FirstOrDefault(g => g.Key.Name == itemInDataGread.ExternalDefinitionParam.OwnerGroup.Name).Value.Cast<ExternalDefinition>().ToList();
                        
                        tmpExternalDefinitionListForSorting = tmpExternalDefinitionListForSorting.OrderBy(ed => ed.Name).ToList();
                        DefinitionGroupxternalDefinitionKVPCollection.FirstOrDefault(g => g.Key.Name == itemInDataGread.ExternalDefinitionParam.OwnerGroup.Name).Value.Clear();
                        foreach (ExternalDefinition sortedExternalDefinition in tmpExternalDefinitionListForSorting)
                        {
                            DefinitionGroupxternalDefinitionKVPCollection.FirstOrDefault(g => g.Key.Name == itemInDataGread.ExternalDefinitionParam.OwnerGroup.Name).Value.Add(sortedExternalDefinition);
                        }
                    }
                }

                string jsonFilePath = openDialog.FileName;
                SharedParametersBatchAddingSettings sharedParametersBatchAddingSettings = new SharedParametersBatchAddingSettings();
                SharedParametersBatchAddingItemsList = sharedParametersBatchAddingSettings.GetSettings(SharedParametersGroups, jsonFilePath);
                
                ObservableCollection<SharedParametersBatchAddingItem> tmpSharedParametersBatchAddingItemsList = new ObservableCollection<SharedParametersBatchAddingItem>(SharedParametersBatchAddingItemsList);
                foreach (SharedParametersBatchAddingItem item in tmpSharedParametersBatchAddingItemsList)
                {
                    if(item.ExternalDefinitionParam == null)
                    {
                        SharedParametersBatchAddingItemsList.Remove(item);
                    }
                }

                dataGrid_SelectedParametersGroup.ItemsSource = SharedParametersBatchAddingItemsList;
                foreach (SharedParametersBatchAddingItem item in SharedParametersBatchAddingItemsList)
                {
                    KeyValuePair<DefinitionGroup, ObservableCollection<ExternalDefinition>> sourseKeyValuePair
                        = DefinitionGroupxternalDefinitionKVPCollection.FirstOrDefault(g => g.Key.Name == item.ExternalDefinitionParam.OwnerGroup.Name);
                    sourseKeyValuePair.Value.Remove(item.ExternalDefinitionParam);
                }
            }
        }

        //Сохранить
        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new System.Windows.Forms.SaveFileDialog();
            saveDialog.Filter = "json files (*.json)|*.json";
            System.Windows.Forms.DialogResult result = saveDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string jsonFilePath = saveDialog.FileName;
                SharedParametersBatchAddingSettings sharedParametersBatchAddingSettings = new SharedParametersBatchAddingSettings();
                sharedParametersBatchAddingSettings.Save(SharedParametersBatchAddingItemsList, jsonFilePath);
            }
        }

        private void dataGrid_SelectedParametersGroup_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (SharedParametersBatchAddingItemsList.Count != 0)
                {
                    List<SharedParametersBatchAddingItem> selectedSharedParametersBatchAddingItemList
                        = dataGrid_SelectedParametersGroup.SelectedItems.Cast<SharedParametersBatchAddingItem>().ToList();
                    foreach (SharedParametersBatchAddingItem selectedSharedParametersBatchAddingItem in selectedSharedParametersBatchAddingItemList)
                    {
                        SharedParametersBatchAddingItemsList.Remove(selectedSharedParametersBatchAddingItem);
                    }
                    foreach (SharedParametersBatchAddingItem selectedSharedParametersBatchAddingItem in selectedSharedParametersBatchAddingItemList)
                    {
                        DefinitionGroupxternalDefinitionKVPCollection.FirstOrDefault(g => g.Key.Name == selectedSharedParametersBatchAddingItem
                        .ExternalDefinitionParam.OwnerGroup.Name).Value.Add(selectedSharedParametersBatchAddingItem.ExternalDefinitionParam);

                        List<ExternalDefinition> tmpExternalDefinitionListForSorting = DefinitionGroupxternalDefinitionKVPCollection
                            .FirstOrDefault(g => g.Key.Name == selectedSharedParametersBatchAddingItem.ExternalDefinitionParam.OwnerGroup.Name).Value.Cast<ExternalDefinition>().ToList();
                        tmpExternalDefinitionListForSorting = tmpExternalDefinitionListForSorting.OrderBy(ed => ed.Name).ToList();

                        DefinitionGroupxternalDefinitionKVPCollection.FirstOrDefault(g => g.Key.Name == selectedSharedParametersBatchAddingItem.ExternalDefinitionParam.OwnerGroup.Name).Value.Clear();
                        foreach (ExternalDefinition sortedExternalDefinition in tmpExternalDefinitionListForSorting)
                        {
                            DefinitionGroupxternalDefinitionKVPCollection.FirstOrDefault(g => g.Key.Name == selectedSharedParametersBatchAddingItem.ExternalDefinitionParam.OwnerGroup.Name)
                                .Value.Add(sortedExternalDefinition);
                        }
                    }
                }
            }
        }

        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            AddParametersSelectedOption = (groupBox_AddParameters.Content as System.Windows.Controls.Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;

            this.DialogResult = true;
            this.Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void SharedParametersBatchAddingFormWPF_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                AddParametersSelectedOption = (groupBox_AddParameters.Content as System.Windows.Controls.Grid)
                    .Children.OfType<RadioButton>()
                    .FirstOrDefault(rb => rb.IsChecked.Value == true)
                    .Name;

                this.DialogResult = true;
                this.Close();
            }

            else if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }
    }
}
