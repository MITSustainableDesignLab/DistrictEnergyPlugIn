using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;

namespace DistrictEnergy
{
    [System.Runtime.InteropServices.Guid("f7e799d4-6531-47a4-8e7e-f469b280a9b2")]
    public class CreateNetworkLayer : Command

    {
        static CreateNetworkLayer _instance;
        public CreateNetworkLayer()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CreateNetworkLayer command.</summary>
        public static CreateNetworkLayer Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "CreateNetworkLayer"; }
        }

        protected override Result RunCommand(RhinoDoc RhinoDocument, RunMode mode)
        {
            try
            {
                string layerPath_h = "umi::District Energy::Heating Network";
                string layerPath_c = "umi::District Energy::Cooling Nerwork";
                string layerPath_p = "umi::District Energy::Thermal Plant";
                Color color_h = Color.FromArgb(0, 255, 79, 0);
                Color color_c = Color.FromArgb(0, 23, 85, 153);
                Color color_p = Color.FromArgb(0, 77, 59, 232);

                Layer target_h = EnsureFullPath(layerPath_h, RhinoDocument);
                Layer target_c = EnsureFullPath(layerPath_c, RhinoDocument);
                Layer target_p = EnsureFullPath(layerPath_p, RhinoDocument);
                target_h.Color = color_h;
                target_h.CommitChanges();
                target_c.Color = color_c;
                target_c.CommitChanges();
                target_p.Color = color_p;
                target_p.CommitChanges();
                var Index_h = target_h.LayerIndex;
                var Index_c = target_c.LayerIndex;
                var Index_p = target_p.LayerIndex;
                RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Index_h, true);


                //return Result.Success;
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"Error: {e.Message}");
                return Result.Failure;
            }

            RhinoApp.WriteLine("Successfully created district energy layers.");
            return Result.Success;

        }

        // <Custom additional code>
        /// <summary>Copy properties from layer From to Layer To</summary>
        /// <param name="From"></param>
        /// <param name="To"></param>
        void MatchProperties(Layer From, Layer To)
        {
            To.Color = From.Color;
            To.RenderMaterialIndex = From.RenderMaterialIndex;
            To.IsExpanded = false;
            To.CommitChanges();
        }

        /// <summary>
        /// /// <summary>
        /// Find a layer by name
        /// </summary>
        /// </summary>
        /// <param name="RhinoDocument"></param>
        /// <param name="name"></param>
        /// <param name="isRoot"></param>
        /// <returns></returns>
        Layer FindLayer(RhinoDoc RhinoDocument, string name, bool isRoot)
        {
            int id = RhinoDocument.Layers.Find(name, true);
            if (id >= 0 && (!isRoot || RhinoDocument.Layers[id].ParentLayerId.Equals(Guid.Empty)))
            {
                return RhinoDocument.Layers[id];
            }
            else
            {
                return null;
            }
        }

        Layer EnsureLayer(RhinoDoc RhinoDocument, string name)
        {
            return EnsureLayer(RhinoDocument, name, false);
        }

        /// <summary>
        /// Make sure layer "name" exists, find it, or create a new one.
        /// </summary>
        /// <param name="RhinoDocument"></param>
        /// <param name="name"></param>
        /// <param name="isRoot">Make sure the layer is not a sublayer</param>
        /// <returns></returns>
        Layer EnsureLayer(RhinoDoc RhinoDocument, string name, bool isRoot)
        {
            Layer l = FindLayer(RhinoDocument, name, isRoot);
            if (l == null)
            {
                l = new Layer();
                l.Name = name;
                int id = RhinoDocument.Layers.Add(l);
                l = RhinoDocument.Layers[id];
                RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(id, true);
            }
            return l;
        }

        /// <summary>
        /// EnsureFullPath: Make sure the full path exists.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="RhinoDocument"></param>
        /// <returns></returns>
        Layer EnsureFullPath(string name, RhinoDoc RhinoDocument)
        {
            Layer l = FindLayerByFullPath(RhinoDocument, name);
            if (l == null)
            {
                string[] segments = System.Text.RegularExpressions.Regex.Split(name, "::");
                string layerPath = segments[0];
                Layer CurrentLayer = EnsureLayer(RhinoDocument, layerPath, true);
                for (int i = 1; i < segments.Length; i++)
                {
                    CurrentLayer = EnsureChildLayer(RhinoDocument, CurrentLayer, segments[i]);
                }
                l = CurrentLayer;
            }
            return l;
        }

        /// <summary>
        /// Find layer by full path
        /// </summary>
        /// <param name="RhinoDocument"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Layer FindLayerByFullPath(RhinoDoc RhinoDocument, string name)
        {
            int id = RhinoDocument.Layers.FindByFullPath(name, true);
            if (id >= 0)
            {
                return RhinoDocument.Layers[id];
            }
            return null;
        }

        /// <summary>
        /// Recurse all child layers
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public IEnumerable<Layer> RecurseChildren(Layer parent)
        {
            foreach (Layer child in parent.GetChildren())
            {
                yield return child;

                if (child.GetChildren() != null)
                {
                    foreach (Layer grandchild in RecurseChildren(child))
                    {
                        yield return grandchild;
                    }
                }
            }
        }

        /// <summary>
        /// Find layer of parent by names
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Layer FindChildLayer(Layer parent, string name)
        {
            if (parent.GetChildren() == null)
            {
                return null;
            }
            foreach (Layer child in parent.GetChildren())
            {
                if (child.Name.Equals(name))
                {
                    return child;
                }
            }
            return null;
        }

        /// <summary>
        /// Make sure all child layers exist
        /// </summary>
        /// <param name="RhinoDocument"></param>
        /// <param name="parent"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public IEnumerable<Layer> EnsureChildLayers(RhinoDoc RhinoDocument, Layer parent, IEnumerable<string> names)
        {
            List<Layer> layers = new List<Layer>();
            foreach (string name in names)
            {
                layers.Add(EnsureChildLayer(RhinoDocument, parent, name));
            }
            return layers;
        }

        /// <summary>
        /// Make sure one child layer exists
        /// </summary>
        /// <param name="RhinoDocument"></param>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public Layer EnsureChildLayer(RhinoDoc RhinoDocument, Layer parent, string name)
        {
            Layer child = FindChildLayer(parent, name);
            if (child == null)
            {
                child = new Layer();
                child.Name = name;
                child.ParentLayerId = parent.Id;
                int id = RhinoDocument.Layers.Add(child);
                child = RhinoDocument.Layers[id];
            }
            return child;
        }
        // </Custom additional code>
    }
}
