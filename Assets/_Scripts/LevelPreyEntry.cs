using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelPreyEntry
{
    public int Level;
    public List<AnimalType> AvailablePrey;
}
public enum AnimalType
{
    Rat,
    deer,
    Elk,
    Tiger,
    Elephant
}