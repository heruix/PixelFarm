//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------

//MIT 2014, WinterDev
//----------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Collections;

namespace MatterHackers.Agg
{
  
    //--------------------------------------------------------------pod_vector
    // A simple class template to store Plain Old Data (POD), a vector
    // of a fixed size. The data is contiguous in memory
    //------------------------------------------------------------------------
    public class ArrayList<T>
    {
        int currentSize;
        T[] internalArray = new T[0]; 
        public ArrayList()
        {
        }

        public ArrayList(int cap)
        {
            Allocate(cap, 0);
        }


        public virtual void RemoveLast()
        {
            if (currentSize != 0)
            {
                currentSize--;
            }
        }
        public void CopyFrom(ArrayList<T> vetorToCopy)
        {
            Allocate(vetorToCopy.currentSize);
            if (vetorToCopy.currentSize != 0)
            {
                vetorToCopy.internalArray.CopyTo(internalArray, 0);
            }
        }
        public int Count
        {
            get { return currentSize; }
        }

        internal int AllocatedSize
        {
            get
            {
                return internalArray.Length;
            }
        }

        public void Clear()
        {
            currentSize = 0;
        }
        // Set new capacity. All data is lost, size is set to zero.
        public void Clear(int newCapacity)
        {
            Clear(newCapacity, 0);
        }
        public void Clear(int newCapacity, int extraTail)
        {
            currentSize = 0;
            if (newCapacity > AllocatedSize)
            {
                internalArray = null;
                int sizeToAllocate = newCapacity + extraTail;
                if (sizeToAllocate != 0)
                {
                    internalArray = new T[sizeToAllocate];
                }
            }
        }



        // Allocate n elements. All data is lost, 
        // but elements can be accessed in range 0...size-1. 
        public void Allocate(int size)
        {
            Allocate(size, 0);
        }

        public void Allocate(int size, int extraTail)
        {
            Clear(size, extraTail);
            currentSize = size;
        } 

        /// <summary>
        ///  Resize keeping the content
        /// </summary>
        /// <param name="newSize"></param>
        public void AdjustSize(int newSize)
        {
            if (newSize > currentSize)
            {
                if (newSize > AllocatedSize)
                {
                    var newArray = new T[newSize];
                    if (internalArray != null)
                    {
                        for (int i = internalArray.Length - 1; i >= 0; --i)
                        {
                            newArray[i] = internalArray[i];
                        }

                    }
                    internalArray = newArray;
                }
            }
        }


        static T zeroed_object = default(T);


        public void zero()
        {

            for (int i = internalArray.Length - 1; i >= 0; --i)
            {
                internalArray[i] = zeroed_object;
            }
        }



        public virtual void AddItem(T v)
        {
            if (internalArray.Length < (currentSize + 1))
            {
                if (currentSize < 100000)
                {
                    AdjustSize(currentSize + (currentSize / 2) + 16);
                }
                else
                {
                    AdjustSize(currentSize + currentSize / 4);
                }
            }
            internalArray[currentSize++] = v;
        }


        public T this[int i]
        {
            get
            {
                return internalArray[i];
            }
        }

        public T[] Array
        {
            get
            {
                return internalArray;
            }
        }


        public T[] GetArray() { return internalArray; }


        public int Length
        {
            get
            {
                return currentSize;
            }
        }



    }

    //----------------------------------------------------------range_adaptor
    public class ArrayListRangeAdaptor
    {
        ArrayList<int> m_array;
        int m_start;
        int m_size;

        public ArrayListRangeAdaptor(ArrayList<int> array, int start, int size)
        {
            m_array = (array);
            m_start = (start);
            m_size = (size);
        }


        public int this[int i]
        {
            get
            {
                return m_array.Array[m_start + i];
            }

            set
            {
                m_array.Array[m_start + i] = value;
            }
        }

        public int Count
        {
            get { return this.m_size; }
        }
    }

    public class Queue<T>
    {
        T[] itemArray;
        int size;
        int head;
        int shiftFactor;
        int mask;

        public int Count
        {
            get { return size; }
        }

        public Queue(int shiftFactor)
        {
            this.shiftFactor = shiftFactor;
            mask = (1 << shiftFactor) - 1;
            itemArray = new T[1 << shiftFactor];
            head = 0;
            size = 0;
        }

        public T First
        {
            get { return itemArray[head & mask]; }
        }

        public void Enqueue(T itemToQueue)
        {
            if (size == itemArray.Length)
            {
                int headIndex = head & mask;
                shiftFactor += 1;
                mask = (1 << shiftFactor) - 1;
                T[] newArray = new T[1 << shiftFactor];
                // copy the from head to the end
                Array.Copy(itemArray, headIndex, newArray, 0, size - headIndex);
                // copy form 0 to the size
                Array.Copy(itemArray, 0, newArray, size - headIndex, headIndex);
                itemArray = newArray;
                head = 0;
            }
            itemArray[(head + (size++)) & mask] = itemToQueue;
        }

        public T Dequeue()
        {
            int headIndex = head & mask;
            T firstItem = itemArray[headIndex];
            if (size > 0)
            {
                head++;
                size--;
            }
            return firstItem;
        }
    }
}
