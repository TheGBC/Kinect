using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinect {
  class BatchHandler<T> {
    private int batch_count;
    private int unit;

    public BatchHandler(int batches, int unit) {
      this.batch_count = batches;
      this.unit = unit;
    }

    public List<T>[] handleBatches(List<T> items){
      List<T>[] arr = new List<T>[batch_count];
      for (int i = 0; i < batch_count; i++) {
        arr[i] = new List<T>();
      }

      for (int i = 0; i < items.Count; i+=3) {
        int ind = (i * batch_count) / items.Count;
        arr[ind].Add(items[i]);
        arr[ind].Add(items[i + 1]);
        arr[ind].Add(items[i + 2]);
      }

      return arr;
    }
  }
}
