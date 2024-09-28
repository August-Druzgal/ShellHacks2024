using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSpell : SpellEffect
{
    [SerializeField] GameObject prefab;

    public override void CastSpell(Transform spellSpawn)
    {
        Debug.Log("CastSpell");
        ThrowSpell tSpell = Instantiate(prefab, spellSpawn.position, spellSpawn.rotation).GetComponent<ThrowSpell>();
    }
}
