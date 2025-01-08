using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSkins", menuName = "Game Tools/ItemSkins", order = 1)]
public class ItemSkins : ScriptableObject
{
    public List<Sprite> itemSkins = new List<Sprite>();
}
