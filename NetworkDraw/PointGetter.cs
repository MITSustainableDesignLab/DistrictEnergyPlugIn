using System;
using System.Drawing;
using Rhino;
using Rhino.ApplicationSettings;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using NetworkDraw.Geometry;
using System.Collections.Generic;
using Mit.Umi.RhinoServices;

namespace NetworkDraw
{
    class PointGetter : IDisposable
    {
        readonly GetPoint _getPoint = new GetPoint();
        readonly int _fromIndex;
        readonly double[] _distances;
        readonly CurvesTopology _crvTopology;
        readonly SearchMode _sm;
        readonly PathMethod _pathSearchMethod;

        public PointGetter(CurvesTopology crvTopology)
            : this(crvTopology, -1, SearchMode.Links)
        {
        }

        public PointGetter(CurvesTopology crvTopology, int fromIndex, SearchMode mode)
        {
            _crvTopology = crvTopology;
            _fromIndex = fromIndex;

            if(!Enum.IsDefined(typeof(SearchMode), mode))
                throw new ArgumentException("Enum is undefined.", "mode");
            _sm = mode;

            if (fromIndex != -1)
            {
                switch(mode)
                {
                    case SearchMode.CurveLength:
                        _distances = crvTopology.MeasureAllEdgeLengths();
                        break;
                    case SearchMode.LinearDistance:
                        _distances = crvTopology.MeasureAllEdgeLinearDistances();
                        break;
                    case SearchMode.Links:
                        _distances = null;
                        break;
                    default:
                        throw new ApplicationException("Behaviour for this enum value is undefined.");
                }
            }

            _pathSearchMethod = PathMethod.FromMode(_sm, _crvTopology, _distances);
        }

        public double[] DistanceCache
        {
            get
            {
                return _distances;
            }
        }

        public Result GetBuildingPointOnTopology(out List<int> index)
        {
            List<Guid> g = new List<Guid>();
            foreach (var o in GlobalContext.GetObjects())
            {
                g.Add(Guid.Parse(o.Id));
            }
            index = new List<int>();
            foreach (var guid in g)
            {
                var obj = RhinoDoc.ActiveDoc.Objects.Find(guid);
                if (obj != null)
                {
                    var a = obj.Geometry.AsBuildingGeometry();
                    var mass_properties = VolumeMassProperties.Compute(a);
                    Point3d pt = new Point3d(mass_properties.Centroid.X, mass_properties.Centroid.Y, 0);
                    index.Add(_crvTopology.GetClosestNode(pt));
                }
                    
            }
                

                

            return Result.Success;
        }

        public virtual void Dispose()
        {
            _getPoint.Dispose();
        }
    }
}