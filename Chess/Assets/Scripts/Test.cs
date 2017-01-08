using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Test : MonoBehaviour
{
	void Start ()
    {
        List<Mahjong.RuleNode> ruleNodes = new List<Mahjong.RuleNode>();
        Mahjong.Rule rule = new Mahjong.Rule();
        Debug.Log(rule.Check(new Mahjong.Rule.Enumerator(new LinkedList<int>(new int[]
        {
            new Mahjong.Tile(Mahjong.TileType.Dots, 3) << 2,
            new Mahjong.Tile(Mahjong.TileType.Dots, 3) << 2,
            new Mahjong.Tile(Mahjong.TileType.Dots, 3) << 2,

            new Mahjong.Tile(Mahjong.TileType.Dots, 4) << 2,
            new Mahjong.Tile(Mahjong.TileType.Dots, 5) << 2,
            new Mahjong.Tile(Mahjong.TileType.Dots, 6) << 2,

            new Mahjong.Tile(Mahjong.TileType.Bomboo, 4) << 2,
            new Mahjong.Tile(Mahjong.TileType.Bomboo, 5) << 2,
            new Mahjong.Tile(Mahjong.TileType.Bomboo, 6) << 2,

            new Mahjong.Tile(Mahjong.TileType.Easet, 0) << 2,
            new Mahjong.Tile(Mahjong.TileType.Easet, 0) << 2,
            new Mahjong.Tile(Mahjong.TileType.Easet, 0) << 2,

            new Mahjong.Tile(Mahjong.TileType.Green, 0) << 2,
            new Mahjong.Tile(Mahjong.TileType.Green, 0) << 2,
        }))).Count());


	}
	
}
