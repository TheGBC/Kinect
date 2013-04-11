using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinect {
  class SkeletonPointComparer : IEqualityComparer<SkeletonPoint> {
    public bool Equals(SkeletonPoint x, SkeletonPoint y) {
      return x.X == y.X && x.Y == y.Y && x.Z == y.Z;
    }

    public int GetHashCode(SkeletonPoint obj) {
      return (int)(obj.X * obj.Y * obj.Z);
    }
  }
}
