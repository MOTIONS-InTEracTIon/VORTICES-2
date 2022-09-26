using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CircularList
{
    public static T GetElement<T>(List<T> list, int index)
    {
        T element;
        
        if(index >= list.Count)
        {
            int newindex = index - list.Count;
            element = list[newindex];
        }
        else if(index < 0)
        {
            int newindex = list.Count + index;
            element = list[newindex];
        } 
        else
        {
            element = list[index];
        }

        return element;
    } 
}
