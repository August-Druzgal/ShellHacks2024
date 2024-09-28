using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Spell
{
    public List<Gesture> gestures;
    [HideInInspector]
    public List<Gesture> reversedGestures;
    public SpellEffect effect;
}
