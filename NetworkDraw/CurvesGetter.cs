using System;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace NetworkDraw
{
    class CurvesGetter : GetObject
    {
        public CurvesGetter(string prompt)
            : base()
        {
            SetCommandPrompt(prompt);
            GeometryFilter = ObjectType.Curve;
            GeometryAttributeFilter = GeometryAttributeFilter.OpenCurve;
            EnablePreSelect(true, true);
            EnablePressEnterWhenDonePrompt(false);
        }

        /// <summary>
        /// Use 0 as max if you do not want to set it.
        /// </summary>
        /// <param name="min">The minimun number of lines</param>
        /// <param name="max">The maximum, as in GetObject.GetMultiple()</param>
        /// <param name="lines"></param>
        /// <returns>True if the getting supplied the requested lines, false otherwise.</returns>
        public bool Curves(int min, int max, out Curve[] lines)
        {
            GetResult a;
            if (min == 1 && max == 1)
                a = Get();
            else
                a = GetMultiple(min, max);

            if (a == GetResult.Object)
            {
                if (ObjectCount > 0)
                {
                    int realCount = 0;

                    lines = new Curve[ObjectCount];
                    for (int i = 0; i < ObjectCount; i++)
                    {
                        Curve c = Object(i).Curve();
                        if (c != null && c.IsValid)
                        {
                            lines[realCount++] = c;
                        }
                    }
                    Array.Resize(ref lines, realCount);
                    return true;
                }
            }
            lines = null;

            return false;
        }
    }
}
