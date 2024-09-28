using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpellEffect : MonoBehaviour
{
    public abstract void CastSpell(Transform spellSpawn);
}
