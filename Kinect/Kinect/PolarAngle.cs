using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kinect
{
    /// <summary>
    /// Compares points by polar angle to the 0 point.
    /// </summary>
    class PolarAngle : IComparer<Point>
    {
        //private Point point0;
        private double ref_x;
        private double ref_y;

      
        //create polar angle class
        public PolarAngle(double x, double y)
        {
            this.ref_x = x;
            this.ref_y = y;
        }


        //returns the polar angle of the points with respect to point zero
        public double getAngle(double x, double y)
        {
            double angle = (ref_x - x) / (y - ref_y);

            return angle;
        }
    }
}
