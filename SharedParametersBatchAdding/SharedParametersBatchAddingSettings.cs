/*
 * Copyright (c) <2023> <Misharev Evgeny>
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer 
 *    in the documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the <organization> nor the names of its contributors may be used to endorse or promote products derived 
 *    from this software without specific prior written permission.
 * 4. Redistributions are not allowed to be sold, in whole or in part, for any compensation of any kind.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, 
 * BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
 * IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, 
 * OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; 
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * Contact: <citrusbim@gmail.com> or <https://web.telegram.org/k/#@MisharevEvgeny>
 */
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;

namespace SharedParametersBatchAdding
{
    class SharedParametersBatchAddingSettings
    {
        public void Save(ObservableCollection<SharedParametersBatchAddingItem> sharedParametersBatchAddingItems, string jsonFilePath)
        {
            if (File.Exists(jsonFilePath))
            {
                File.Delete(jsonFilePath);
            }

            using (FileStream fs = new FileStream(jsonFilePath, FileMode.Create))
            {
                fs.Close();
            }
            JsonSerializer serializer = new JsonSerializer();
            using (StreamWriter sw = new StreamWriter(jsonFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(writer, sharedParametersBatchAddingItems);
            }
        }

        public ObservableCollection<SharedParametersBatchAddingItem> GetSettings(DefinitionGroups sharedParametersGroups, string jsonFilePath)
        {
            ObservableCollection<SharedParametersBatchAddingItem> sharedParametersBatchAddingItemsTmp = new ObservableCollection<SharedParametersBatchAddingItem>();
            if (File.Exists(jsonFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                using (StreamReader sr = new StreamReader(jsonFilePath))
                using (JsonTextReader reader = new JsonTextReader(sr))
                {
                    ObservableCollection<SharedParametersBatchAddingItem> tmp = (ObservableCollection<SharedParametersBatchAddingItem>)serializer
                        .Deserialize(reader, typeof(ObservableCollection<SharedParametersBatchAddingItem>));
                    if (tmp != null)
                    {
                        foreach (SharedParametersBatchAddingItem itm in tmp)
                        {
                            foreach (DefinitionGroup spg in sharedParametersGroups)
                            {
                                foreach (ExternalDefinition d in spg.Definitions)
                                {
                                    if (itm.ExternalDefinitionParamGuid == d.GUID)
                                    {
                                        itm.ExternalDefinitionParam = d;
                                        break;
                                    }
                                }
                                if (itm.ExternalDefinitionParam != null) break;
                            }
                        }
                        sharedParametersBatchAddingItemsTmp = tmp;
                    }
                }
            }
            else
            {
                sharedParametersBatchAddingItemsTmp = new ObservableCollection<SharedParametersBatchAddingItem>();
            }
            return sharedParametersBatchAddingItemsTmp;
        }
    }
}
