using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace SharedParametersBatchAdding
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class SharedParametersBatchAddingCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Получение текущего документа
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            DefinitionFile defFile = uidoc.Application.Application.OpenSharedParameterFile();
            if (defFile == null)
            {
                TaskDialog.Show("Revit", "Перед началом работы подключите Файл Общих Параметров!");
                return Result.Cancelled;
            }
            DefinitionGroups sharedParametersGroups = defFile.Groups;
            List<BuiltInParameterGroup> builtInParameterGroup = GetBuiltInParameterGroup();

            ObservableCollection<KeyValuePair<string, BuiltInParameterGroup>> builtInParameterGroupKeyValuePairs = new ObservableCollection<KeyValuePair<string, BuiltInParameterGroup>>();
            foreach (BuiltInParameterGroup item in builtInParameterGroup)
            {
                try
                {
                    builtInParameterGroupKeyValuePairs.Add(new KeyValuePair<string, BuiltInParameterGroup>(LabelUtils.GetLabelFor(item), item));
                }
                catch { }
            }
            builtInParameterGroupKeyValuePairs = new ObservableCollection<KeyValuePair<string, BuiltInParameterGroup>>(builtInParameterGroupKeyValuePairs.OrderBy(i => i.Key));

            SharedParametersBatchAddingWPF sharedParametersBatchAddingFormWPF = new SharedParametersBatchAddingWPF(sharedParametersGroups, builtInParameterGroupKeyValuePairs);
            sharedParametersBatchAddingFormWPF.ShowDialog();
            if (sharedParametersBatchAddingFormWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }
            string addParametersSelectedOption = sharedParametersBatchAddingFormWPF.AddParametersSelectedOption;
            IList<SharedParametersBatchAddingItem> sharedParametersBatchAddingItemsList = sharedParametersBatchAddingFormWPF.SharedParametersBatchAddingItemsList;
            if (addParametersSelectedOption == "radioButton_ActiveFamily")
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;
                if (doc.IsFamilyDocument)
                {
                    using (Transaction t = new Transaction(doc))
                    {
                        t.Start("Добавление параметров в семейство");
                        IList<FamilyParameter> fPars = doc.FamilyManager.GetParameters().Where(p => p.IsShared).ToList();

                        foreach (SharedParametersBatchAddingItem sharedParameterItem in sharedParametersBatchAddingItemsList)
                        {
                            IList<FamilyParameter> inFamily = fPars.Where(p => p.GUID == sharedParameterItem.ExternalDefinitionParam.GUID).ToList();
                            if (inFamily.Count == 0)
                            {
                                FamilyParameter fp = doc.FamilyManager.AddParameter(sharedParameterItem.ExternalDefinitionParam, sharedParameterItem.BuiltInParameterGroupParam.Value, sharedParameterItem.AddParameterSelectedOptionParam) as FamilyParameter;
                                if (sharedParameterItem.FormulaParam != null && sharedParameterItem.FormulaParam != "")
                                {
                                    try
                                    {
                                        doc.FamilyManager.SetFormula(fp, sharedParameterItem.FormulaParam);
                                    }
                                    catch
                                    {
                                        TaskDialog.Show("Revit", "Проверьте формат формулы в параметре \"" + sharedParameterItem.ExternalDefinitionParam.Name + "\"!");
                                    }
                                }
                            }
                            else
                            {
                                if (sharedParameterItem.FormulaParam != null && sharedParameterItem.FormulaParam != "")
                                {
                                    try
                                    {
                                        doc.FamilyManager.SetFormula(fPars.Where(p => p.GUID == sharedParameterItem.ExternalDefinitionParam.GUID).ToList().First(), sharedParameterItem.FormulaParam);
                                    }
                                    catch
                                    {
                                        TaskDialog.Show("Revit", "Проверьте формат формулы в параметре \"" + sharedParameterItem.ExternalDefinitionParam.Name + "\"!");
                                    }
                                }
                            }

                        }
                        t.Commit();
                    }
                }
                else
                {
                    TaskDialog.Show("Revit", "Текущий документ не является семейством!");
                    return Result.Cancelled;
                }
            }
            else if (addParametersSelectedOption == "radioButton_AllOpenFamilies")
            {
                DocumentSet documentSet = commandData.Application.Application.Documents;
                foreach (Document doc in documentSet)
                {
                    if (doc.IsFamilyDocument)
                    {
                        using (Transaction t = new Transaction(doc))
                        {
                            t.Start("Добавление параметров в семейство");
                            IList<FamilyParameter> fPars = doc.FamilyManager.GetParameters().Where(p => p.IsShared).ToList();

                            foreach (SharedParametersBatchAddingItem sharedParameterItem in sharedParametersBatchAddingItemsList)
                            {
                                IList<FamilyParameter> inFamily = fPars.Where(p => p.GUID == sharedParameterItem.ExternalDefinitionParam.GUID).ToList();
                                if (inFamily.Count == 0)
                                {
                                    FamilyParameter fp = doc.FamilyManager.AddParameter(sharedParameterItem.ExternalDefinitionParam, sharedParameterItem.BuiltInParameterGroupParam.Value, sharedParameterItem.AddParameterSelectedOptionParam) as FamilyParameter;
                                    if (sharedParameterItem.FormulaParam != null && sharedParameterItem.FormulaParam != "")
                                    {
                                        try
                                        {
                                            doc.FamilyManager.SetFormula(fp, sharedParameterItem.FormulaParam);
                                        }
                                        catch
                                        {
                                            TaskDialog.Show("Revit", "Проверьте формат формулы в параметре \"" + sharedParameterItem.ExternalDefinitionParam.Name + "\"!");
                                        }
                                    }
                                }
                                else
                                {
                                    if (sharedParameterItem.FormulaParam != null && sharedParameterItem.FormulaParam != "")
                                    {
                                        try
                                        {
                                            doc.FamilyManager.SetFormula(fPars.Where(p => p.GUID == sharedParameterItem.ExternalDefinitionParam.GUID).ToList().First(), sharedParameterItem.FormulaParam);
                                        }
                                        catch
                                        {
                                            TaskDialog.Show("Revit", "Проверьте формат формулы в параметре \"" + sharedParameterItem.ExternalDefinitionParam.Name + "\"!");
                                        }
                                    }
                                }
                            }
                            t.Commit();
                        }
                    }
                }
            }
            else if (addParametersSelectedOption == "radioButton_FamiliesInSelectedFolder")
            {
                if (sharedParametersBatchAddingFormWPF.FilePath == null)
                {
                    TaskDialog.Show("Revit", "Не выбран путь к папке с семействами!");
                    return Result.Cancelled;
                }
                string[] files = Directory.GetFiles(sharedParametersBatchAddingFormWPF.FilePath).Where(p => p.Split('.').Last() == "rfa").ToArray();
                foreach (string file in files)
                {
                    Document familyDoc = commandData.Application.Application.OpenDocumentFile(file);
                    using (Transaction t = new Transaction(familyDoc))
                    {
                        t.Start("Добавление параметров в семейство");
                        IList<FamilyParameter> fPars = familyDoc.FamilyManager.GetParameters().Where(p => p.IsShared).ToList();

                        foreach (SharedParametersBatchAddingItem sharedParameterItem in sharedParametersBatchAddingItemsList)
                        {
                            IList<FamilyParameter> inFamily = fPars.Where(p => p.GUID == sharedParameterItem.ExternalDefinitionParam.GUID).ToList();
                            if (inFamily.Count == 0)
                            {
                                FamilyParameter fp = familyDoc.FamilyManager.AddParameter(sharedParameterItem.ExternalDefinitionParam, sharedParameterItem.BuiltInParameterGroupParam.Value, sharedParameterItem.AddParameterSelectedOptionParam) as FamilyParameter;
                                if (sharedParameterItem.FormulaParam != null && sharedParameterItem.FormulaParam != "")
                                {
                                    try
                                    {
                                        familyDoc.FamilyManager.SetFormula(fp, sharedParameterItem.FormulaParam);
                                    }
                                    catch
                                    {
                                        TaskDialog.Show("Revit", "Проверьте формат формулы в параметре \"" + sharedParameterItem.ExternalDefinitionParam.Name + "\"!");
                                    }
                                }
                            }
                            else
                            {
                                if (sharedParameterItem.FormulaParam != null && sharedParameterItem.FormulaParam != "")
                                {
                                    try
                                    {
                                        familyDoc.FamilyManager.SetFormula(fPars.Where(p => p.GUID == sharedParameterItem.ExternalDefinitionParam.GUID).ToList().First(), sharedParameterItem.FormulaParam);
                                    }
                                    catch
                                    {
                                        TaskDialog.Show("Revit", "Проверьте формат формулы в параметре \"" + sharedParameterItem.ExternalDefinitionParam.Name + "\"!");
                                    }
                                }
                            }
                        }
                        t.Commit();
                    }
                    familyDoc.Close();
                }
            }
            return Result.Succeeded;
        }

        private static List<BuiltInParameterGroup> GetBuiltInParameterGroup()
        {
            List<BuiltInParameterGroup> tmpBuiltInParameterGroup = new List<BuiltInParameterGroup>();
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_ANALYSIS_RESULTS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_ANALYTICAL_ALIGNMENT);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_ANALYTICAL_MODEL);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_CONSTRAINTS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_CONSTRUCTION);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_DATA);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_GEOMETRY);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_DIVISION_GEOMETRY);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_AELECTRICAL);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_ELECTRICAL_CIRCUITING);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_ELECTRICAL_LIGHTING);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_ELECTRICAL_LOADS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_ELECTRICAL);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_ENERGY_ANALYSIS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_FIRE_PROTECTION);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_FORCES);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_GENERAL);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_GRAPHICS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_GREEN_BUILDING);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_IDENTITY_DATA);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_IFC);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_REBAR_SYSTEM_LAYERS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_MATERIALS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_MECHANICAL);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_MECHANICAL_AIRFLOW);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_MECHANICAL_LOADS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_ADSK_MODEL_PROPERTIES);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_MOMENTS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.INVALID);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_OVERALL_LEGEND);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_PHASING);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_LIGHT_PHOTOMETRICS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_PLUMBING);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_PRIMARY_END);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_REBAR_ARRAY);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_RELEASES_MEMBER_FORCES);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_SECONDARY_END);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_SECONDARY_END);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_SEGMENTS_FITTINGS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_SLAB_SHAPE_EDIT);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_STRUCTURAL);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_STRUCTURAL_ANALYSIS);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_TEXT);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_TITLE);
            tmpBuiltInParameterGroup.Add(BuiltInParameterGroup.PG_VISIBILITY);
            return tmpBuiltInParameterGroup;
        }
    }
}
