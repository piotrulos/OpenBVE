﻿using System;
using System.Xml;

namespace OpenBve
{
    internal static class Ls3DGrpParser
    {

        internal class GruppenObject
        {
            //A gruppenobject holds a list of ls3dobjs, which appear to be roughly equivilant to meshbuilders
            internal string Name;
            internal World.Vector3D Position;
            //Change??
            internal World.Vector3D Rotation;
        }

        internal static ObjectManager.AnimatedObjectCollection ReadObject(string FileName, System.Text.Encoding Encoding,
            ObjectManager.ObjectLoadMode LoadMode)
        {
            XmlDocument currentXML = new XmlDocument();
            //May need to be changed to use de-DE
            System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;
            ObjectManager.AnimatedObjectCollection Result = new ObjectManager.AnimatedObjectCollection();
            Result.Objects = new ObjectManager.AnimatedObject[0];
            int ObjectCount = 0;
            currentXML.Load(FileName);
            string BaseDir = System.IO.Path.GetDirectoryName(FileName);

            GruppenObject[] CurrentObjects = new GruppenObject[0];
            //Check for null
            if (currentXML.DocumentElement != null)
            {
                ObjectManager.UnifiedObject[] obj = new OpenBve.ObjectManager.UnifiedObject[0];
                XmlNodeList DocumentNodes = currentXML.DocumentElement.SelectNodes("/GRUPPENOBJECT");
                if (DocumentNodes != null)
                {
                    foreach (XmlNode outerNode in DocumentNodes)
                    {
                        if (outerNode.HasChildNodes)
                        {
                            foreach (XmlNode node in outerNode.ChildNodes)
                            {
                                if (node.Name == "Object" && node.HasChildNodes)
                                {
                                    foreach (XmlNode childNode in node.ChildNodes)
                                    {
                                        if (childNode.Name == "Props" && childNode.Attributes != null)
                                        {
                                            Array.Resize<GruppenObject>(ref CurrentObjects, CurrentObjects.Length + 1);
                                            GruppenObject Object = new GruppenObject();
                                            foreach (XmlAttribute attribute in childNode.Attributes)
                                            {
                                                switch (attribute.Name)
                                                {
                                                    case "Name":
                                                        string ObjectFile = OpenBveApi.Path.CombineFile(BaseDir,attribute.Value);
                                                        
                                                        Object.Name = ObjectFile;
                                                        //obj[obj.Length -1] = ObjectManager.LoadObject(ObjectFile, Encoding, LoadMode, false, false, false);
                                                        ObjectCount++;
                                                        break;
                                                    case "Position":
                                                        string[] SplitPosition = attribute.Value.Split(';');
                                                        double.TryParse(SplitPosition[0], out Object.Position.X);
                                                        double.TryParse(SplitPosition[1], out Object.Position.Y);
                                                        double.TryParse(SplitPosition[2], out Object.Position.Z);
                                                        break;
                                                    case "Rotation":
                                                        //This will require passing a paramater to the LS3D object reader I think
                                                        //Dynamic rotation doesn't seem to be possible
                                                        break;
                                                }
                                            }
                                            CurrentObjects[CurrentObjects.Length - 1] = Object;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    Array.Resize<ObjectManager.UnifiedObject>(ref obj, CurrentObjects.Length);
                    Array.Resize<ObjectManager.AnimatedObject>(ref Result.Objects, CurrentObjects.Length);
                    //We've loaded the XML references, now load the objects into memory
                    for (int i = 0; i < CurrentObjects.Length; i++)
                    {
                        obj[i] = ObjectManager.LoadObject(CurrentObjects[i].Name, Encoding, LoadMode, false, false, false);
                    }
                    for (int j = 0; j < obj.Length; j++)
                    {
                        if (obj[j] != null)
                        {
                            if (obj[j] is ObjectManager.StaticObject)
                            {
                                ObjectManager.StaticObject s = (ObjectManager.StaticObject) obj[j];
                                s.Dynamic = true;
                                ObjectManager.AnimatedObject a = new ObjectManager.AnimatedObject();
                                ObjectManager.AnimatedObjectState aos = new ObjectManager.AnimatedObjectState
                                {
                                    Object = s,
                                    Position = CurrentObjects[j].Position
                                };
                                a.States = new ObjectManager.AnimatedObjectState[] {aos};
                                Result.Objects[j] = a;
                                ObjectCount++;
                            }
                            else if (obj[j] is ObjectManager.AnimatedObjectCollection)
                            {
                                ObjectManager.AnimatedObjectCollection a =
                                    (ObjectManager.AnimatedObjectCollection) obj[j];
                                for (int k = 0; k < a.Objects.Length; k++)
                                {
                                    for (int h = 0; h < a.Objects[k].States.Length; h++)
                                    {
                                        a.Objects[k].States[h].Position.X += CurrentObjects[j].Position.X;
                                        a.Objects[k].States[h].Position.Y += CurrentObjects[j].Position.Y;
                                        a.Objects[k].States[h].Position.Z += CurrentObjects[j].Position.Z;
                                    }
                                    Result.Objects[j] = a.Objects[k];
                                    ObjectCount++;
                                }
                            }
                        }
                    }
                }
                Array.Resize<ObjectManager.AnimatedObject>(ref Result.Objects, CurrentObjects.Length);
                return Result;
            }
            //Didn't find an acceptable XML object
            //Probably will cause things to throw an absolute wobbly somewhere....
            return null;
        }
    }
}