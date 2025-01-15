using System.Collections.Generic;

namespace bet_slum.Utility
{
    public class PriorityQueue<T>
    {
        private List<(T, double)> elements = new List<(T, double)>();

        public int Count
        {
            get { return elements.Count; }
        }

        public void Enqueue(T item, double priorityValue)
        {
            elements.Add((item, priorityValue));
        }

        public bool TryDequeue(out T item)
        {
            if (elements.Count == 0) 
            {
                item = default(T);
                return false; 
            }

            item = Dequeue();
            return true;
        }

        public T Dequeue()
        {
            int bestPriorityIndex = 0;

            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Item2 < elements[bestPriorityIndex].Item2)
                {
                    bestPriorityIndex = i;
                }
            }

            T bestItem = elements[bestPriorityIndex].Item1;
            elements.RemoveAt(bestPriorityIndex);
            return bestItem;
        }

        public T Peek()
        {
            int bestPriorityIndex = 0;

            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Item2 < elements[bestPriorityIndex].Item2)
                {
                    bestPriorityIndex = i;
                }
            }

            T bestItem = elements[bestPriorityIndex].Item1;
            return bestItem;
        }
    }
}