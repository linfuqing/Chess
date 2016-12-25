using System.Collections.Generic;
using UnityEngine;
using ZG.Network;

public class Test : MonoBehaviour
{
	void Start ()
    {
        List<Mahjong.RuleNode> ruleNodes = new List<Mahjong.RuleNode>();
        Mahjong.Rule rule = new Mahjong.Rule();
        rule.Check(new int[]
        {

            new Mahjong.Tile(Mahjong.TileType.Dots, 6) << 2,
            new Mahjong.Tile(Mahjong.TileType.Dots, 8) << 2,
            new Mahjong.Tile(Mahjong.TileType.Bomboo, 0) << 2,
            new Mahjong.Tile(Mahjong.TileType.Bomboo, 1) << 2,
            new Mahjong.Tile(Mahjong.TileType.Bomboo, 2) << 2,
            new Mahjong.Tile(Mahjong.TileType.Bomboo, 4) << 2,
            
            new Mahjong.Tile(Mahjong.TileType.Bomboo, 5) << 2,
            new Mahjong.Tile(Mahjong.TileType.Bomboo, 6) << 2,
        }, new Mahjong.Tile(Mahjong.TileType.Bomboo, 3) << 2, ruleNodes.Add);

        foreach(Mahjong.RuleNode ruleNode in ruleNodes)
        {
            Debug.Log(ruleNode.type.ToString() + ":" + ruleNode.index + ":" + ruleNode.offset);
        }
	}
	
}
