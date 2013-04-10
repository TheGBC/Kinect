using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Drawing;



namespace Kinect
{
    class GrahamScan
    {
        private int size;
        private List<Vector3> points;

        //contructor method
        public GrahamScan(Vector3[] pts)
        {
            size = pts.Length;
            points[0] = pts[pts.Length - 1];
            for (int i = 0; i < pts.Length; i++)
            {
                points.Add(pts[i]);
            }
        }

        //get the element with the minimum y value
        private Vector3 getMinY() {
            Vector3 min = points[0];
            points = new List<Vector3>();
            for (int i = 0; i < points.Count; i++) {
                if (min.Y > points[i].Y) {
                    min = points[i];
                }
            }
            return min;
        }

        //sort the angles by their polar values
        //  simple bubbleSort method used here...
        public void sortByPolars(double x, double y) {
            PolarAngle polar = new PolarAngle(x, y);
            Boolean swapped = true;
            int j = 0;
            Vector3 temp;
            while (swapped)
            {
                swapped = false;
                j++;
                for (int i = 0; i < points.Count - 1; i++)
                {
                    if (polar.getAngle(points[i].X, points[i].Y) > polar.getAngle(points[i + 1].X, points[i + 1].Y))
                    {
                        temp = points[i];
                        points[i] = points[i + 1];
                        points[i+1] = temp;
                        swapped = true;
                    }

                }
            }
        }

        //  if (c.x – a.x)(b.y – a.y) > (c.y – a.y)(b.x – a.x) then the movement from line a-b to line a-c is clockwise.
        //  if (c.x – a.x)(b.y – a.y) < (c.y – a.y)(b.x – a.x) then the movement from line a-b to line a-c is counterclockwise.
        //  Otherwise the three points are co-linear.
        private Boolean counterClockWise(Vector3 b, Vector3 c, Vector3 min)
        {
            if( (c.X-min.X)*(b.Y-min.Y) <  (c.Y-min.Y)*(b.X-min.X) {
                return true;
            }
            else {
            return false;
            }
        }

        public List<Vector3> findConvexHull() {
            //find the minY
            Vector3 min = getMinY();
            //sort by polar angle
            sortByPolars(min.X, min.Y);
            //iterate through ...
            //finish tomorrow...

            List<Vector3> convex = new List<Vector3>();
            List<Vector3> result = new List<Vector3>();
            //convex.Add(points[points.Count-1]);
            //convex.Add(min);
           for(int i = 0; i < points.Count; i ++)
           {
               convex.Add(points[i]);
           }
            //number of points
            //add numbers to the result then run the algthm
           result.Add(convex[0]);
           result.Add(convex[1]);
           result.Add(convex[2]);
           for (int i = 3; i < convex.Count; i++)
           {
               //acts as a stack in here..
               while (counterClockWise(result[result.Count - 2], result[result.Count - 1], convex[i]) && result.Count > 3)
               {
                   result.RemoveAt(result.Count - 1);
               }
               result.Add(convex[i]);

           }

           return result;
        }
    }
}
