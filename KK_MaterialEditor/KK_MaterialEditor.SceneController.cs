﻿using ExtensibleSaveFormat;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KK_MaterialEditor
{
    public partial class KK_MaterialEditor
    {
        public class MaterialEditorSceneController : SceneCustomFunctionController
        {
            private readonly List<RendererProperty> RendererPropertyList = new List<RendererProperty>();
            private readonly List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
            private readonly List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
            private readonly List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();

            private static Dictionary<int, byte[]> TextureDictionary = new Dictionary<int, byte[]>();

            private static byte[] TexBytes = null;
            private static string PropertyToSet = "";
            private static string MatToSet;
            private static int IDToSet = 0;
            private static GameObject GameObjectToSet;

            protected override void OnSceneSave()
            {
                var data = new PluginData();
                if (data == null)
                    return;

                List<int> IDsToPurge = new List<int>();
                foreach (int texID in TextureDictionary.Keys)
                    if (!MaterialTexturePropertyList.Any(x => x.TexID == texID))
                        IDsToPurge.Add(texID);

                foreach (int texID in IDsToPurge)
                    TextureDictionary.Remove(texID);

                if (TextureDictionary.Count > 0)
                    data.data.Add(nameof(TextureDictionary), MessagePackSerializer.Serialize(TextureDictionary));
                else
                    data.data.Add(nameof(TextureDictionary), null);

                if (RendererPropertyList.Count > 0)
                    data.data.Add(nameof(RendererPropertyList), MessagePackSerializer.Serialize(RendererPropertyList));
                else
                    data.data.Add(nameof(RendererPropertyList), null);

                if (MaterialFloatPropertyList.Count > 0)
                    data.data.Add(nameof(MaterialFloatPropertyList), MessagePackSerializer.Serialize(MaterialFloatPropertyList));
                else
                    data.data.Add(nameof(MaterialFloatPropertyList), null);

                if (MaterialColorPropertyList.Count > 0)
                    data.data.Add(nameof(MaterialColorPropertyList), MessagePackSerializer.Serialize(MaterialColorPropertyList));
                else
                    data.data.Add(nameof(MaterialColorPropertyList), null);

                if (MaterialTexturePropertyList.Count > 0)
                    data.data.Add(nameof(MaterialTexturePropertyList), MessagePackSerializer.Serialize(MaterialTexturePropertyList));
                else
                    data.data.Add(nameof(MaterialTexturePropertyList), null);

                SetExtendedData(data);
            }

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                var data = GetExtendedData();

                if (operation == SceneOperationKind.Clear || operation == SceneOperationKind.Load)
                {
                    RendererPropertyList.Clear();
                    MaterialFloatPropertyList.Clear();
                    MaterialColorPropertyList.Clear();
                    MaterialTexturePropertyList.Clear();
                    TextureDictionary.Clear();
                }

                if (data == null)
                    return;

                var importDictionary = new Dictionary<int, int>();

                if (operation == SceneOperationKind.Load)
                    if (data.data.TryGetValue(nameof(TextureDictionary), out var texDic) && texDic != null)
                        TextureDictionary = MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic);
                    else if (operation == SceneOperationKind.Import)
                        if (data.data.TryGetValue(nameof(TextureDictionary), out texDic) && texDic != null)
                            foreach (var x in MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic))
                                importDictionary[x.Key] = SetAndGetTextureID(x.Value);
                        else if (operation == SceneOperationKind.Clear)
                            return;

                if (data.data.TryGetValue(nameof(RendererPropertyList), out var rendererProperties) && rendererProperties != null)
                    foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties))
                        if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                            if (SetRendererProperty(ociItem.objectItem, loadedProperty.RendererName, loadedProperty.Property, int.Parse(loadedProperty.Value), ObjectType.StudioItem))
                                RendererPropertyList.Add(new RendererProperty(GetObjectID(objectCtrlInfo), loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                if (data.data.TryGetValue(nameof(MaterialFloatPropertyList), out var materialFloatProperties) && materialFloatProperties != null)
                    foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties))
                        if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                            if (SetFloatProperty(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, ObjectType.StudioItem))
                                MaterialFloatPropertyList.Add(new MaterialFloatProperty(GetObjectID(objectCtrlInfo), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                if (data.data.TryGetValue(nameof(MaterialColorPropertyList), out var materialColorProperties) && materialColorProperties != null)
                    foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties))
                        if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                            if (SetColorProperty(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, ObjectType.StudioItem))
                                MaterialColorPropertyList.Add(new MaterialColorProperty(GetObjectID(objectCtrlInfo), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                if (data.data.TryGetValue(nameof(MaterialTexturePropertyList), out var materialTextureProperties) && materialTextureProperties != null)
                    foreach (var loadedProperty in MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties))
                        if (loadedItems.TryGetValue(loadedProperty.ID, out ObjectCtrlInfo objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                        {
                            int texID = operation == SceneOperationKind.Import ? importDictionary[loadedProperty.TexID] : loadedProperty.TexID;
                            MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(GetObjectID(objectCtrlInfo), loadedProperty.MaterialName, loadedProperty.Property, texID);
                            if (SetTextureProperty(ociItem.objectItem, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Texture, ObjectType.StudioItem))
                                MaterialTexturePropertyList.Add(newTextureProperty);
                        }
            }

            protected override void OnObjectsCopied(ReadOnlyDictionary<int, ObjectCtrlInfo> copiedItems)
            {
                List<RendererProperty> rendererPropertyListNew = new List<RendererProperty>();
                List<MaterialFloatProperty> materialFloatPropertyListNew = new List<MaterialFloatProperty>();
                List<MaterialColorProperty> materialColorPropertyListNew = new List<MaterialColorProperty>();
                List<MaterialTextureProperty> materialTexturePropertyListNew = new List<MaterialTextureProperty>();

                foreach (var copiedItem in copiedItems)
                    if (copiedItem.Value is OCIItem ociItem)
                        foreach (var loadedProperty in RendererPropertyList.Where(x => x.ID == copiedItem.Key))
                            if (SetRendererProperty(ociItem.objectItem, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, ObjectType.StudioItem))
                                rendererPropertyListNew.Add(new RendererProperty(copiedItem.Value.GetSceneId(), loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                foreach (var copiedItem in copiedItems)
                    if (copiedItem.Value is OCIItem ociItem)
                        foreach (var loadedProperty in MaterialFloatPropertyList.Where(x => x.ID == copiedItem.Key))
                            if (SetFloatProperty(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, ObjectType.StudioItem))
                                materialFloatPropertyListNew.Add(new MaterialFloatProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                foreach (var copiedItem in copiedItems)
                    if (copiedItem.Value is OCIItem ociItem)
                        foreach (var loadedProperty in MaterialColorPropertyList.Where(x => x.ID == copiedItem.Key))
                            if (SetColorProperty(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, ObjectType.StudioItem))
                                materialColorPropertyListNew.Add(new MaterialColorProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));

                foreach (var copiedItem in copiedItems)
                    if (copiedItem.Value is OCIItem ociItem)
                        foreach (var loadedProperty in MaterialTexturePropertyList.Where(x => x.ID == copiedItem.Key))
                        {
                            MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(copiedItem.Value.GetSceneId(), loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.TexID);
                            if (SetTextureProperty(ociItem.objectItem, loadedProperty.MaterialName, loadedProperty.Property, newTextureProperty.Texture, ObjectType.StudioItem))
                                materialTexturePropertyListNew.Add(newTextureProperty);
                        }

                RendererPropertyList.AddRange(rendererPropertyListNew);
                MaterialFloatPropertyList.AddRange(materialFloatPropertyListNew);
                MaterialColorPropertyList.AddRange(materialColorPropertyListNew);
                MaterialTexturePropertyList.AddRange(materialTexturePropertyListNew);
            }

            private void Update()
            {
                try
                {
                    if (TexBytes != null)
                    {
                        Texture2D tex = new Texture2D(2, 2);
                        tex.LoadImage(TexBytes);

                        SetTextureProperty(GameObjectToSet, MatToSet, PropertyToSet, tex, ObjectType.StudioItem);

                        var textureProperty = MaterialTexturePropertyList.FirstOrDefault(x => x.ID == IDToSet && x.Property == PropertyToSet && x.MaterialName == MatToSet);
                        if (textureProperty == null)
                            MaterialTexturePropertyList.Add(new MaterialTextureProperty(IDToSet, MatToSet, PropertyToSet, SetAndGetTextureID(TexBytes)));
                        else
                            textureProperty.Data = TexBytes;
                    }
                }
                catch
                {
                    BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, "Failed to load texture.");
                }
                finally
                {
                    TexBytes = null;
                    PropertyToSet = "";
                    MatToSet = null;
                    GameObjectToSet = null;
                }
            }

            internal void ItemDeleteEvent(int ID)
            {
                RendererPropertyList.RemoveAll(x => x.ID == ID);
                MaterialFloatPropertyList.RemoveAll(x => x.ID == ID);
                MaterialColorPropertyList.RemoveAll(x => x.ID == ID);
                MaterialTexturePropertyList.RemoveAll(x => x.ID == ID);
            }
            /// <summary>
            /// Finds the texture bytes in the dictionary of textures and returns its ID. If not found, adds the texture to the dictionary and returns the ID of the added texture.
            /// </summary>
            private static int SetAndGetTextureID(byte[] textureBytes)
            {
                int highestID = 0;
                foreach (var tex in TextureDictionary)
                    if (tex.Value.SequenceEqual(textureBytes))
                        return tex.Key;
                    else if (tex.Key > highestID)
                        highestID = tex.Key;

                highestID++;
                TextureDictionary.Add(highestID, textureBytes);
                return highestID;
            }

            public void AddRendererProperty(int id, string rendererName, RendererProperties property, string value, string valueOriginal)
            {
                var rendererProperty = RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == rendererName);
                if (rendererProperty == null)
                    RendererPropertyList.Add(new RendererProperty(id, rendererName, property, value, valueOriginal));
                else
                {
                    if (value == rendererProperty.ValueOriginal)
                        RendererPropertyList.Remove(rendererProperty);
                    else
                        rendererProperty.Value = value;
                }
            }
            public string GetRendererPropertyValue(int id, string rendererName, RendererProperties property) =>
                RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == rendererName)?.Value;
            public string GetRendererPropertyValueOriginal(int id, string rendererName, RendererProperties property) =>
                RendererPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.RendererName == rendererName)?.ValueOriginal;
            public void RemoveRendererProperty(int id, string rendererName, RendererProperties property) =>
                RendererPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.RendererName == rendererName);

            public void AddMaterialFloatProperty(int id, string materialName, string property, string value, string valueOriginal)
            {
                var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.MaterialName == materialName);
                if (materialProperty == null)
                    MaterialFloatPropertyList.Add(new MaterialFloatProperty(id, materialName, property, value, valueOriginal));
                else
                {
                    if (value == materialProperty.ValueOriginal)
                        MaterialFloatPropertyList.Remove(materialProperty);
                    else
                        materialProperty.Value = value;
                }
            }
            public string GetMaterialFloatPropertyValue(int id, string materialName, string property) =>
                MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.MaterialName == materialName)?.Value;
            public string GetMaterialFloatPropertyValueOriginal(int id, string materialName, string property) =>
                MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.MaterialName == materialName)?.ValueOriginal;
            public void RemoveMaterialFloatProperty(int id, string materialName, string property) =>
                MaterialFloatPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.MaterialName == materialName);

            public void AddMaterialColorProperty(int id, string materialName, string property, Color value, Color valueOriginal)
            {
                var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == property && x.MaterialName == materialName);
                if (colorProperty == null)
                    MaterialColorPropertyList.Add(new MaterialColorProperty(id, materialName, property, value, valueOriginal));
                else
                {
                    if (value == colorProperty.ValueOriginal)
                        MaterialColorPropertyList.Remove(colorProperty);
                    else
                        colorProperty.Value = value;
                }
            }
            public Color GetMaterialColorPropertyValue(int id, string materialName, string property)
            {
                if (MaterialColorPropertyList.Where(x => x.ID == id && x.Property == property && x.MaterialName == materialName).Count() == 0)
                    return new Color(-1, -1, -1, -1);
                return MaterialColorPropertyList.First(x => x.ID == id && x.Property == property && x.MaterialName == materialName).Value;
            }
            public Color GetMaterialColorPropertyValueOriginal(int id, string materialName, string property)
            {
                if (MaterialColorPropertyList.Where(x => x.ID == id && x.Property == property && x.MaterialName == materialName).Count() == 0)
                    return new Color(-1, -1, -1, -1);
                return MaterialColorPropertyList.First(x => x.ID == id && x.Property == property && x.MaterialName == materialName).ValueOriginal;
            }
            public void RemoveMaterialColorProperty(int id, string materialName, string property) =>
                MaterialColorPropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.MaterialName == materialName);
            public void AddMaterialTextureProperty(int id, string materialName, string property, GameObject go)
            {
                OpenFileDialog.Show(strings => OnFileAccept(strings), "Open image", Application.dataPath, FileFilter, FileExt);

                void OnFileAccept(string[] strings)
                {
                    if (strings == null || strings.Length == 0)
                        return;

                    if (strings[0].IsNullOrEmpty())
                        return;

                    TexBytes = File.ReadAllBytes(strings[0]);
                    PropertyToSet = property;
                    MatToSet = materialName;
                    GameObjectToSet = go;
                    IDToSet = id;
                }
            }
            public void RemoveMaterialTextureProperty(int id, string materialName, string property)
            {
                BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Message, "Save and reload scene to refresh textures.");
                MaterialTexturePropertyList.RemoveAll(x => x.ID == id && x.Property == property && x.MaterialName == materialName);
            }

            [Serializable]
            [MessagePackObject]
            private class RendererProperty
            {
                [Key("ID")]
                public int ID;
                [Key("RendererName")]
                public string RendererName;
                [Key("Property")]
                public RendererProperties Property;
                [Key("Value")]
                public string Value;
                [Key("ValueOriginal")]
                public string ValueOriginal;

                public RendererProperty(int id, string rendererName, RendererProperties property, string value, string valueOriginal)
                {
                    ID = id;
                    RendererName = rendererName.Replace("(Instance)", "").Trim();
                    Property = property;
                    Value = value;
                    ValueOriginal = valueOriginal;
                }
            }

            [Serializable]
            [MessagePackObject]
            private class MaterialFloatProperty
            {
                [Key("ID")]
                public int ID;
                [Key("MaterialName")]
                public string MaterialName;
                [Key("Property")]
                public string Property;
                [Key("Value")]
                public string Value;
                [Key("ValueOriginal")]
                public string ValueOriginal;

                public MaterialFloatProperty(int id, string materialName, string property, string value, string valueOriginal)
                {
                    ID = id;
                    MaterialName = materialName.Replace("(Instance)", "").Trim();
                    Property = property;
                    Value = value;
                    ValueOriginal = valueOriginal;
                }
            }

            [Serializable]
            [MessagePackObject]
            private class MaterialColorProperty
            {
                [Key("ID")]
                public int ID;
                [Key("MaterialName")]
                public string MaterialName;
                [Key("Property")]
                public string Property;
                [Key("Value")]
                public Color Value;
                [Key("ValueOriginal")]
                public Color ValueOriginal;

                public MaterialColorProperty(int id, string materialName, string property, Color value, Color valueOriginal)
                {
                    ID = id;
                    MaterialName = materialName.Replace("(Instance)", "").Trim();
                    Property = property;
                    Value = value;
                    ValueOriginal = valueOriginal;
                }
            }
            [Serializable]
            [MessagePackObject]
            public class MaterialTextureProperty
            {
                [Key("ID")]
                public int ID;
                [Key("MaterialName")]
                public string MaterialName;
                [Key("Property")]
                public string Property;
                [Key("TexID")]
                public int TexID;

                [IgnoreMember]
                private byte[] _data;
                [IgnoreMember]
                public byte[] Data
                {
                    get => _data;
                    set
                    {
                        Dispose();
                        _data = value;
                        TexID = SetAndGetTextureID(value);
                    }
                }
                [IgnoreMember]
                private Texture2D _texture;
                [IgnoreMember]
                public Texture2D Texture
                {
                    get
                    {
                        if (_texture == null)
                        {
                            if (_data != null)
                                _texture = TextureFromBytes(_data, TextureFormat.ARGB32);
                        }
                        return _texture;
                    }
                }

                public MaterialTextureProperty(int id, string materialName, string property, int texID)
                {
                    ID = id;
                    MaterialName = materialName.Replace("(Instance)", "").Trim();
                    Property = property;
                    TexID = texID;
                    Data = TextureDictionary[texID];
                }

                public void Dispose()
                {
                    if (_texture != null)
                    {
                        UnityEngine.Object.Destroy(_texture);
                        _texture = null;
                    }
                }

                public bool IsEmpty() => Data == null;
            }
        }
    }
}
